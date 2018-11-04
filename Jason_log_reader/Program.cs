using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Timers;
using System.Reflection;

namespace LogReader
{
	class Program
	{	
		//Application global variables 
		private static Timer countdownTimer;
		private static int seconds;
		private const int offsetSeconds = 5; //***Will be 120 seconds after testing
		private const int captureTimerSeconds = 10; //***Will be 300 seconds after testing
		private static bool offsetComplete = false;
		private static Object serverDetails;

		//***Debugging output variables***
		private static int lineCounter = 0;
		private static int totalLines = 0;
		private static double totalTime;

		static void Main(string[] args)
		{
			//Get log details
			serverDetails = GetLogInfo();

			Console.ReadKey();
		}

		//Prototype log reader
		private static Object GetLogInfo()
		{
			//Variable declarations
			String serverName = String.Empty;
			String mapName = String.Empty;
			int playerCount = 0;
			String playerStatus = String.Empty;
			String prevStatus = String.Empty;
			LogData serverDetails = new LogData(serverName, mapName, playerCount, prevStatus, playerStatus);

			//Regex matching declarations
			Regex countReg = new Regex(@"LogDiscordRichPresence: Number of players changed to\s(\d+)");
			Regex statusReg = new Regex(@"LogSquadTrace: \[Client\] ASQPlayerController::ChangeState\(\):\sPC=[A-z0-9.]+\sOldState=(\w+)\sNewState=(\w+)");
			Regex serverReg = new Regex(@"LogDiscordRichPresence: Change server name command received.  Server name is\s([A-z0-9_\-#|.]+?\s[A-z0-9_\-#|.]+?\s[A-z0-9_\-#|.]+?\s[A-z0-9_\-#|.]+?\s[A-z0-9_\-#|.]+)");
			Regex mapReg = new Regex(@"LogDiscordRichPresence: Change Map command received.  Map name is\s([A-z0-9_\-#|.]+)");

			//Imports text file data line by line
			string fileName = @"squad_data\log_big.log";
			var logLines = File.ReadLines(fileName);

			//Starts log read execution timer
			Stopwatch execTimer = Stopwatch.StartNew();

			foreach (var line in logLines)
			{
				Match userCount = countReg.Match(line);
				Match userStatus = statusReg.Match(line);
				Match userServer = serverReg.Match(line);
				Match userMap = mapReg.Match(line);

				//Sets the value if found in the log for user count, player's status, server name, and map name
				if (userServer.Success)
				{
					serverName = userServer.Groups[1].Value;
					lineCounter += 1;
				}
				if (userMap.Success)
				{
					//Check if map has changed before the offset and capture time completed
					if (serverDetails.MapName != null && serverDetails.MapName != userMap.Groups[1].Value)
					{
						seconds = offsetSeconds;
						offsetComplete = false;
						//TODO: Call function to stop recording and clear caputured data
					}
					mapName = userMap.Groups[1].Value;
					lineCounter += 1;
				}
				if (userCount.Success)
				{
					playerCount = Int32.Parse(userCount.Groups[1].Value);
					lineCounter += 1;
				}
				if (userStatus.Success)
				{
					prevStatus = userStatus.Groups[1].Value;
					playerStatus = userStatus.Groups[2].Value;
					lineCounter += 1;
				}
				totalLines += 1;
			}
			serverDetails.ServerName = serverName;
			serverDetails.MapName = mapName;
			serverDetails.PlayerCount = playerCount;
			serverDetails.PrevStatus = prevStatus;
			serverDetails.PlayerStatus = playerStatus;

			//Stops and converts the log read script timer
			execTimer.Stop();
			totalTime = execTimer.ElapsedMilliseconds;
			totalTime /= 1000;

			String server = (serverDetails.GetType().GetProperty("ServerName").GetValue(serverDetails, null)).ToString();
			String map = (serverDetails.GetType().GetProperty("MapName").GetValue(serverDetails, null)).ToString();
			int players = Convert.ToInt32(serverDetails.GetType().GetProperty("PlayerCount").GetValue(serverDetails, null));

			//Check that a server and game map has been selected
			if (server != null && (map != null || map != "EntryMap") && players >= 70)
			{
				SetCountdownTimer();
			}

			return serverDetails;
		}

		//Prototype countdown timer
		private static void SetCountdownTimer()
		{
			countdownTimer = new Timer();
			countdownTimer.Interval = 1000;
			countdownTimer.Elapsed += TickManager;
			countdownTimer.AutoReset = true;
			countdownTimer.Enabled = true;
		}

		//Prototype timer tick manager
		private static void TickManager(object source, ElapsedEventArgs e)
		{
			int players = Convert.ToInt32(serverDetails.GetType().GetProperty("PlayerCount").GetValue(serverDetails, null));

			//Reduces seconds only if 70 or more players are present
			if(players >= 70)
			{
				seconds--;
			}

			//***Output timer for debugging***
			if (offsetComplete == false)
			{
				Console.WriteLine("The offset timer has {0} seconds remaining", seconds);
			}
			else
			{
				Console.WriteLine("The capture timer has {0} seconds remaining", seconds);
			}

			ManageCoundownConditions();
		}

		//Prototype to manage counter conditions
		private static void ManageCoundownConditions()
		{
			int players = Convert.ToInt32(serverDetails.GetType().GetProperty("PlayerCount").GetValue(serverDetails, null));
			String currStatus = (serverDetails.GetType().GetProperty("PlayerStatus").GetValue(serverDetails, null)).ToString();

			//Capture FPS if enough players are present and user is activly playing in the match
			if (players >= 70)
			{
				if (offsetComplete == true && currStatus == "Playing")
				{
					Console.WriteLine("FPS is being captured");
				}
			}
			else
			{
				if (offsetComplete == false)
				{
					seconds = offsetSeconds;
				}
				else
				{
					Console.WriteLine("FPS capture has stopped");
					seconds = captureTimerSeconds;
				}
			}

			//Determine next action when timer hits zero
			if (seconds == 0)
			{
				//Start the capture timer upon completion of the offset timer
				if (offsetComplete == false)
				{
					offsetComplete = true;
					seconds = captureTimerSeconds;
				}
				//End the capture timer and end the program
				else if (offsetComplete == true)
				{
					countdownTimer.Enabled = false;
					ConsoleOutputer();
				}
			}
		}

		//Prototype sum function
		public static int Sum(params int[] fps)
		{
			int result = 0;

			for (int i = 0; i < fps.Length; i++)
			{
				result += fps[i];
			}
			return result;
		}

		//Prototype to generate sample fps data
		public static int[] generateFPSData()
		{
			int stableMin = 65;
			int stableMax = 70;
			int outlierMin = 30;
			int outlierMax = 110;

			Random randNum = new Random();
			int[] fpsArray = new int[900];

			for(int i = 0; i < 850; i++)
			{
				fpsArray[i] = randNum.Next(stableMin, stableMax);
			}
			for(int i = 850; i < 900; i++)
			{
				fpsArray[i] = randNum.Next(outlierMin, outlierMax);
			}
			return fpsArray;
		}

		//Output to the console
		private static void ConsoleOutputer()
		{
			String server = (serverDetails.GetType().GetProperty("ServerName").GetValue(serverDetails, null)).ToString();
			String map = (serverDetails.GetType().GetProperty("MapName").GetValue(serverDetails, null)).ToString();
			int players = Convert.ToInt32(serverDetails.GetType().GetProperty("PlayerCount").GetValue(serverDetails, null));
			String prevStatus = (serverDetails.GetType().GetProperty("PrevStatus").GetValue(serverDetails, null)).ToString();
			String currStatus = (serverDetails.GetType().GetProperty("PlayerStatus").GetValue(serverDetails, null)).ToString();

			//Generate FPS data average
			int[] sampleFPS = generateFPSData();
			double average = Sum(sampleFPS) / sampleFPS.Length;

			//Test Output
			Console.WriteLine("");
			Console.WriteLine("Start of File...");
			Console.WriteLine("");
			Console.WriteLine("Server Name: {0}", server);
			Console.WriteLine("Map Name: {0}", map);
			Console.WriteLine("Current players on server: {0}", players);
			Console.WriteLine("Player's previous status: {0}", prevStatus);
			Console.WriteLine("Player's current status: {0}", currStatus);
			Console.WriteLine("Player's current FPS: {0}", average);
			Console.WriteLine("");
			Console.WriteLine("Matched {0} lines in the log...", lineCounter);
			Console.WriteLine("Processed {0} total lines in the log...", totalLines);
			Console.WriteLine("Total Time is: {0} seconds", totalTime);
			Console.WriteLine("");
			Console.WriteLine("Press any key to continue...");
		}
	}

	public class LogData
	{
		public string ServerName { get; set; }
		public string MapName { get; set; }
		public int PlayerCount { get; set; }
		public string PrevStatus { get; set; }
		public string PlayerStatus { get; set; }
		public LogData(string svrName, string map, int usrCnt, string prvStat, string currStat)
		{
			ServerName = svrName;
			MapName = map;
			PlayerCount = usrCnt;
			PrevStatus = prvStat;
			PlayerStatus = currStat;
		}
	}
}
