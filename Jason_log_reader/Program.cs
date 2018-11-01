using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace LogReader
{
	class Program
	{
		static void Main(string[] args)
		{
			//Starts execution timer
			Stopwatch execTimer = Stopwatch.StartNew();

			//Regex matching declarations
			Regex countReg = new Regex(@"LogDiscordRichPresence: Number of players changed to\s(\d+)");
			Regex statusReg = new Regex(@"LogSquadTrace: \[Client\] ASQPlayerController::ChangeState\(\):\sPC=[A-z0-9.]+\sOldState=(\w+)\sNewState=(\w+)");
			Regex serverReg = new Regex(@"LogDiscordRichPresence: Change server name command received.  Server name is\s([A-z0-9_\-#|.]+?\s[A-z0-9_\-#|.]+?\s[A-z0-9_\-#|.]+?\s[A-z0-9_\-#|.]+?\s[A-z0-9_\-#|.]+)");
			Regex mapReg = new Regex(@"LogDiscordRichPresence: Change Map command received.  Map name is\s([A-z0-9_\-#|.]+)");

			
			//Imports text file data line by line
			String fileName = @"squad_data\log_big.log";
			var logLines = File.ReadLines(fileName);

			//Log file data declarations
			int playerCount = 0;
			String playerStatus = String.Empty;
			String prevStatus = String.Empty;
			String serverName = String.Empty;
			String mapName = String.Empty;
			int lineCounter = 0;

			Console.WriteLine("Start of File...");
			foreach (var line in logLines)
			{
				Match userCount = countReg.Match(line);
				Match userStatus = statusReg.Match(line);
				Match userServer = serverReg.Match(line);
				Match userMap = mapReg.Match(line);

				//Sets the value if found in the log for user count, player's status, server name, and map name
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
				if (userServer.Success)
				{
					serverName = userServer.Groups[1].Value;
					lineCounter += 1;
				}
				if (userMap.Success)
				{
					mapName = userMap.Groups[1].Value;
					lineCounter += 1;
				}
			}
			
			//Stops and converts script timer
			execTimer.Stop();
			double totalTime = execTimer.ElapsedMilliseconds;
			totalTime /= 1000;

			//Test Output
			Console.WriteLine("");
			Console.WriteLine("Server Name: {0}", serverName);
			Console.WriteLine("Map Name: {0}", mapName);
			Console.WriteLine("Current players on server: {0}", playerCount);
			Console.WriteLine("Player's previous status: {0}", prevStatus);
			Console.WriteLine("Player's current status: {0}", playerStatus);
			Console.WriteLine("");
			Console.WriteLine("Processed {0} lines in the log...", lineCounter);
			Console.WriteLine("Total Time is: {0} seconds", totalTime);
			Console.WriteLine("Press any key to continue...");
			Console.ReadKey();
		}
	}
}
