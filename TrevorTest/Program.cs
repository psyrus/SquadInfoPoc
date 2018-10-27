using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

using System.Threading; // needed for AutoResetEvent FileSystemWatcher stuff

namespace TrevorTest
{
    class Program
    {
        static void Main(string[] args)
        {
            // info on 'tail'ing a file
            // https://stackoverflow.com/questions/3791103/c-sharp-continuously-read-file
            var wh = new AutoResetEvent(false); // Notifies a waiting thread that an event has occured
            var fsw = new FileSystemWatcher(".");
            fsw.Filter = "log_big.log";
            fsw.EnableRaisingEvents = true;
            fsw.Changed += (s, e) => {
                if(e.ChangeType == WatcherChangeTypes.Changed)
                {
                    wh.Set();
                }
            };

            Regex playersCount = new Regex(@"\[(.*?)\]\[.*?\].*?LogDiscordRichPresence: Number of players changed to (\d+)");
            Regex playerStatus = new Regex(@"\[(.*?)\]\[.*?\].*?ASQPlayerController::ChangeState\(\): PC=(.*?) OldState=(.*?) NewState=(.+)");
            Regex serverName = new Regex(@"\[(.*?)\]\[.*?\].*?Change server name command received.  Server name is (.+)");

            int latestPlayerCount = 0;
            PlayerStatus latestPlayerStatus = PlayerStatus.Inactive;
            string latestServerName = "Menu";
            int numPlayerLoggingThreshold = 70; // Number of players at which to start logging the FPS
            string eventTime = "0:0";

            DateTime playersThresholdTimer = new DateTime(2000, 01, 01, 01, 01, 01);
            DateTime serverChangedTimer = new DateTime(2000, 01, 01, 01, 01, 01);
            DateTime timeNow = new DateTime(2001, 02, 02, 02, 02, 02);
            bool playerThreshold = false;
            bool playerTimerStarted = false;
            bool serverTimerStarted = false;
            bool serverChanged = false;

            int loc = 0;

            TimeSpan playerTimeDiff;
            TimeSpan serverTimeDiff;

            Stopwatch sw = Stopwatch.StartNew();
            // Open File
            // Loop through each line of the file
            Match m;
            try
            {
                
                //Pass the file path and file name to the StreamReader constructor
                // StreamReader sr = new StreamReader("log_big.log");
                var fs = new FileStream("log_big.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                //Continue to read until you reach end of file
                using (var sr = new StreamReader(fs))
                {
                    var line = sr.ReadLine();
                    if (line.Contains("Log file open"))
                    {
                        Console.WriteLine("This is the start of the file!");
                    }
                    // for(int i = 0; i < 40; i++)
                    // {
                    //     line = sr.ReadLine();
                    // }
                    while (true)
                    {
                        LStart:
                        line = sr.ReadLine();
                        if (line != null)
                        {
                            serverChanged = false;

                            //Check latest player count
                            try
                            {
                                // Do a rudimentary "string in string" search, skipping the timestamp
                                loc = line.IndexOf("LogDiscord", 29);
                                // if the string is found
                                if (loc != -1)
                                {
                                    // Do a more thorough regexp search
                                    m = playersCount.Match(line);
                                    if (m.Success)
                                    {
                                        latestPlayerCount = int.Parse(m.Groups[2].Value);
                                        eventTime = m.Groups[1].Value;
                                        Console.Write($"Time: {eventTime} ");
                                        Console.WriteLine($"Player Count Changed: {latestPlayerCount}");
                                        playerThreshold = latestPlayerCount >= numPlayerLoggingThreshold ? true : false; // do something here with the timer starting I guess
                                        goto TimerStuff;
                                        // continue;
                                    }
                                }
                            }
                            // Without this exception handling, the script crashes on a short line like "Script call stack:" (example on line 605)
                            catch (ArgumentOutOfRangeException e)
                            {
                                // Console.WriteLine(e.Message);
                                // Console.ReadLine();
                            }
                            
                            //Check player status
                            try
                            {
                                loc = line.IndexOf("ASQPlayerController", 29);
                                // if the string is found
                                if (loc != -1)
                                {
                                    m = playerStatus.Match(line);
                                    if (m.Success)
                                    {
                                        latestPlayerStatus = GetStatus(m.Groups[4].Value);
                                        eventTime = m.Groups[1].Value;
                                        Console.Write($"Time: {eventTime} ");
                                        Console.WriteLine($"Player Status Changed: {latestPlayerStatus}");
                                        goto TimerStuff;
                                        // continue;
                                    }
                                }
                            }
                            catch (ArgumentOutOfRangeException e)
                            {
                                // Console.WriteLine(e.Message);
                                // Console.ReadLine();
                            }
                            
                            //Check joined server
                            try
                            {
                                loc = line.IndexOf("Change server name", 29);
                                // if the string is found
                                if (loc != -1)
                                {
                                    m = serverName.Match(line);
                                    if (m.Success)
                                    {
                                        latestServerName = m.Groups[2].Value;
                                        eventTime = m.Groups[1].Value;
                                        Console.Write($"Time: {eventTime} ");
                                        Console.WriteLine($"Server Changed: {latestServerName}");
                                        serverChanged = true;
                                        goto TimerStuff;
                                        // continue;
                                    }
                                }
                            }
                            catch (ArgumentOutOfRangeException e)
                            {
                                // Console.WriteLine(e.Message);
                                // Console.ReadLine();
                            }
                            
                            goto LStart;

                            TimerStuff:
                            //Determine if the player count has been >70 for more than 2 minutes and the player has been in the server for at least 5 minutes
                            // https://stackoverflow.com/questions/2821040/how-do-i-get-the-time-difference-between-two-datetime-objects-using-c
                            // 2018.05.11-08.22.01:887         <-------- date format from logs
                            // DateTime.TryParseExact("2018.05.11-08.22.01:887", "yyyy.MM.dd-HH.mm.ss:fff", null, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out a);

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

        private static PlayerStatus GetStatus(string value)
        {
            return value == "Playing" ? PlayerStatus.Active : PlayerStatus.Inactive;
        }
        
    }

    internal enum PlayerStatus
    {
        Active,
        Inactive
    }
}
