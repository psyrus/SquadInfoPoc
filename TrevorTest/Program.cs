using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace TrevorTest
{
    class Program
    {

        static void Main(string[] args)
        {
            Regex playersCount = new Regex(@"\[(.*?)\]\[.*?\].*?LogDiscordRichPresence: Number of players changed to (\d+)");
            Regex playerStatus = new Regex(@"\[(.*?)\]\[.*?\].*?ASQPlayerController::ChangeState\(\): PC=(.*?) OldState=(.*?) NewState=(.+)");
            Regex serverName = new Regex(@"\[(.*?)\]\[.*?\].*?Change server name command received.  Server name is (.+)");

            int latestPlayerCount = 0;
            PlayerStatus latestPlayerStatus = PlayerStatus.Inactive;
            string latestServerName = "Menu";

            // Open File
            // Loop through each line of the file
            String line;
            Match m;
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                
                //Pass the file path and file name to the StreamReader constructor
                StreamReader sr = new StreamReader("log_big.log");

                //Read the first line of text
                line = sr.ReadLine();
                if (line.Contains("Log file open"))
                {
                    Console.WriteLine("This is the start of the file!");
                }
                //Continue to read until you reach end of file
                while (line != null)
                {

                    //Check latest player count
                    m = playersCount.Match(line);
                    latestPlayerCount = m.Success ? int.Parse(m.Groups[2].Value) : latestPlayerCount;
                    //Check player status
                    m = playerStatus.Match(line);
                    latestPlayerStatus = m.Success ? GetStatus(m.Groups[4].Value) : latestPlayerStatus;
                    //Check joined server
                    m = serverName.Match(line);
                    latestServerName = m.Success ? m.Groups[2].Value : latestServerName;

                    
                    //Output values:
                    //Console.WriteLine($"Time: ");
                    //Console.WriteLine($"Latest Player Count: {latestPlayerCount}");
                    //Console.WriteLine($"Latest Player Status: {latestPlayerStatus.ToString()}");
                    //Console.WriteLine($"Latest Server: {latestServerName}");

                    //Determine if the player count has been >70 for more than 2 minutes and the player has been in the server for at least 5 minutes

                    //Read the next line
                    line = sr.ReadLine();
                }

                //close the file
                sr.Close();
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
