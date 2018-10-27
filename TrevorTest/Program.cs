using System;
using System.IO;
using System.Text.RegularExpressions;

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
            fsw.Changed += (s,e) => wh.Set();

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

            TimeSpan playerTimeDiff;
            TimeSpan serverTimeDiff;

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
                    while (true)
                    {
                        LStart:
                        line = sr.ReadLine();
                        if (line != null)
                        {
                            serverChanged = false;

                            //Check latest player count
                            m = playersCount.Match(line);
                            // latestPlayerCount = m.Success ? int.Parse(m.Groups[2].Value) : latestPlayerCount;
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

                            //Check player status
                            m = playerStatus.Match(line);
                            // latestPlayerStatus = m.Success ? GetStatus(m.Groups[4].Value) : latestPlayerStatus;
                            if (m.Success)
                            {
                                latestPlayerStatus = GetStatus(m.Groups[4].Value);
                                eventTime = m.Groups[1].Value;
                                Console.Write($"Time: {eventTime} ");
                                Console.WriteLine($"Player Status Changed: {latestPlayerStatus}");
                                goto TimerStuff;
                                // continue;
                            }

                            //Check joined server
                            m = serverName.Match(line);
                            // latestServerName = m.Success ? m.Groups[2].Value : latestServerName;
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
                        }
                    }
                }

                Console.WriteLine("This is the end of the file!");

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
