using System.Text.RegularExpressions;

namespace Jason_log_reader.Models
{
	public class LogData
	{
		public string ServerName { get; set; }
		public string MapName { get; set; }
		public int PlayerCount { get; set; }
		public string PrevStatus { get; set; }
		public string PlayerStatus { get; set; }

		//Regex matching declarations
		public Regex countReg = new Regex(@"LogDiscordRichPresence: Number of players changed to\s(\d+)");
		public Regex statusReg = new Regex(@"LogSquadTrace: \[Client\] ASQPlayerController::ChangeState\(\):\sPC=[A-z0-9.]+\sOldState=(\w+)\sNewState=(\w+)");
		public Regex serverReg = new Regex(@"LogDiscordRichPresence: Change server name command received.  Server name is (.+)");
		public Regex mapReg = new Regex(@"LogDiscordRichPresence: Change Map command received.  Map name is ([^_\s]+)");

		public LogData()
		{
			ServerName = string.Empty;
			MapName = string.Empty;
			PlayerCount = 0;
			PrevStatus = string.Empty;
			PlayerStatus = string.Empty;
		}
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
