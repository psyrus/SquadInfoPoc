using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

using System.Threading; // needed for AutoResetEvent FileSystemWatcher stuff

namespace TrevorTest
{
    internal class Program
    {
        private static Regex playersCount = new Regex(@"\[(.*?)\]\[.*?\].*?LogDiscordRichPresence: Number of players changed to (\d+)");
        private static Regex playerStatus = new Regex(@"\[(.*?)\]\[.*?\].*?ASQPlayerController::ChangeState\(\): PC=(.*?) OldState=(.*?) NewState=(.+)");
        private static Regex serverName = new Regex(@"\[(.*?)\]\[.*?\].*?Change server name command received.  Server name is (.+)");
        private static int latestPlayerCount = 0;
        private static PlayerStatus latestPlayerStatus = PlayerStatus.Inactive;
        private static string latestServerName = "Menu";
        private static int numPlayerLoggingThreshold = 70; // Number of players at which to start logging the FPS
        private static string eventTime = "0:0";
        private static bool playerThreshold = false;
        private static bool playerTimerStarted = false;
        private static bool serverTimerStarted = false;
        private static bool serverChanged = false;
        private static DateTime playersThresholdTimer = new DateTime(2000, 01, 01, 01, 01, 01);
        private static DateTime serverChangedTimer = new DateTime(2000, 01, 01, 01, 01, 01);
        private static DateTime timeNow = new DateTime(2001, 02, 02, 02, 02, 02);
        private static TimeSpan playerTimeDiff;
        private static TimeSpan serverTimeDiff;

        private static void Main(string[] args)
        {
            // info on 'tail'ing a file
            // https://stackoverflow.com/questions/3791103/c-sharp-continuously-read-file
            var wh = new AutoResetEvent(false); // Notifies a waiting thread that an event has occured
            var fsw = new FileSystemWatcher(".");
            fsw.Filter = "log_big.log";
            fsw.EnableRaisingEvents = true;
            fsw.Changed += (s, e) =>
            {
                if (e.ChangeType == WatcherChangeTypes.Changed)
                {
                    wh.Set();
                }
            };

            Stopwatch sw = Stopwatch.StartNew();
            // Open File
            // Loop through each line of the file
            try
            {   
                // Open log file and loop over it to evalute lines
                // TODO: Not sure that the FileStream -> StreamReader is absolutely necessary
                using (var sr = new StreamReader(new FileStream("log_big.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    var line = sr.ReadLine();
                    if (line.Contains("Log file open"))
                    {
                        Console.WriteLine("This is the start of the file!");
                    }

                    while (true)
                    {
                        line = sr.ReadLine();
                        if (line != null)
                        {
                            serverChanged = false;
                            
                            bool shouldUpdateTimer = false;

                            // This is kind of a hacky way to do this, but I didn't want to lose *any* of the goto optimizations if possible, so this was the cleanest way that I could find.
                            // Basically you reference functions kind of like the labels, and that way there don't have to be long if-else blocks anywhere
                            // https://stackoverflow.com/a/27215922
                            var TryFunctions = new Func<string, bool>[] {
                                n => TryGetPlayersCount(line),
                                n => TryGetPlayerStatus(line),
                                n => TryGetServerName(line)
                            };

                            // Loop through the functions, if one of the functions returns true, update the timer and then skip the rest of the functions for this line.
                            // Functions are looped over in their most likely to be hit order to improve efficiency.
                            for (int i = 0; i < TryFunctions.Length; i++)
                            {
                                shouldUpdateTimer = TryFunctions[i](line);
                                if (shouldUpdateTimer)
                                {
                                    UpdateTimer(line);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // Sleep for one second (?)
                            wh.WaitOne(1000);
                            Console.WriteLine("End");
                            // Just for testing, when you reach the EOF, break out of the loop (don't tail)
                            break;
                        }
                    }
                }

                Console.WriteLine("This is the end of the file!");

                Console.WriteLine($"Execution took: {sw.Elapsed}");

                //close the file
                // sr.Close();
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
                Console.ReadLine();
            }
            finally
            {
                Console.WriteLine("Executing finally block.");
                sw.Stop();
                Console.WriteLine($"Latest Player Count: {latestPlayerCount}");
                Console.WriteLine($"Latest Player Status: {latestPlayerStatus.ToString()}");
                Console.WriteLine($"Latest Server: {latestServerName}");
                Console.WriteLine($"Execution took: {sw.Elapsed}");
                Console.ReadLine();
            }
        }

        private static void UpdateTimer(string line)
        {
            // If we've just hit the player threshold
            if (playerThreshold && !playerTimerStarted)
            {
                // Parse the log's timestamp and format it to a DateTime object
                DateTime.TryParseExact(eventTime, "yyyy.MM.dd-HH.mm.ss:fff", null, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out playersThresholdTimer);
                playerTimerStarted = true;
                Console.WriteLine("Player count timer started");
            }

            // Players dropped below threshold, stop the timer
            else if (!playerThreshold && playerTimerStarted)
            {
                playerTimerStarted = false;
                Console.WriteLine("Player count timer reset");
            }

            // If we haven't changed server
            if (!serverChanged && !serverTimerStarted)
            {
                DateTime.TryParseExact(eventTime, "yyyy.MM.dd-HH.mm.ss:fff", null, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out serverChangedTimer);
                serverTimerStarted = true;
                Console.WriteLine("Server timer started");
            }
            // If we've changed server recently
            else if (serverChanged && serverTimerStarted)
            {
                serverTimerStarted = false;
                Console.WriteLine("Server timer reset");
            }

            // Get the time, in minutes, that both timers have been running
            if (playerTimerStarted && serverTimerStarted)
            {
                DateTime.TryParseExact(eventTime, "yyyy.MM.dd-HH.mm.ss:fff", null, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out timeNow);
                playerTimeDiff = timeNow - playersThresholdTimer;
                serverTimeDiff = timeNow - serverChangedTimer;
                if (playerTimeDiff.TotalMinutes > 2)
                {
                    Console.WriteLine("Player timer passed threshold: {0:F2} minutes", playerTimeDiff.TotalMinutes);
                }
                if (serverTimeDiff.TotalMinutes > 5)
                {
                    Console.WriteLine("Server timer passed threshold: {0:F2} minutes", serverTimeDiff.TotalMinutes);
                }
            }
        }

        private static PlayerStatus GetStatus(string value)
        {
            return value == "Playing" ? PlayerStatus.Active : PlayerStatus.Inactive;
        }

        private static bool TryGetPlayersCount(string line)
        {

            // Do a rudimentary "string in string" search, skipping the timestamp
            if (line.Length <= 29 || line.IndexOf("LogDiscord", 29) == -1)
            {
                return false;
            }
            // Do a more thorough regexp search
            Match m = playersCount.Match(line);
            if (m.Success)
            {
                latestPlayerCount = int.Parse(m.Groups[2].Value);
                eventTime = m.Groups[1].Value;
                Console.Write($"Time: {eventTime} ");
                Console.WriteLine($"Player Count Changed: {latestPlayerCount}");
                playerThreshold = latestPlayerCount >= numPlayerLoggingThreshold ? true : false; // do something here with the timer starting I guess
            }
            return true;

        }
        private static bool TryGetPlayerStatus(string line)
        {
            // Do a rudimentary "string in string" search, skipping the timestamp
            if (line.Length <= 29 || line.IndexOf("ASQPlayerController", 29) == -1)
            {
                return false;
            }

            // Do a more thorough regexp search
            Match m = playerStatus.Match(line);
            if (m.Success)
            {
                latestPlayerStatus = GetStatus(m.Groups[4].Value);
                eventTime = m.Groups[1].Value;
                Console.Write($"Time: {eventTime} ");
                Console.WriteLine($"Player Status Changed: {latestPlayerStatus}");

            }
            return true;
        }
        private static bool TryGetServerName(string line)
        {
            // Do a rudimentary "string in string" search, skipping the timestamp
            if (line.Length <= 29 || line.IndexOf("Change server name", 29) == -1)
            {
                return false;
            }

            // Do a more thorough regexp search
            Match m = serverName.Match(line);
            if (m.Success)
            {
                latestServerName = m.Groups[2].Value;
                eventTime = m.Groups[1].Value;
                Console.Write($"Time: {eventTime} ");
                Console.WriteLine($"Server Changed: {latestServerName}");
                serverChanged = true;
            }
            return true;
        }

    }

    internal enum PlayerStatus
    {
        Active,
        Inactive
    }
}
