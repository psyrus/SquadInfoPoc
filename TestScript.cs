using System;
using System.Diagnostics;
using System.Threading;
using System.Timers;

namespace SquadInfoPoc
{
    class TestScript
    {
        private const string SQUAD_PROCESS_NAME = "Squad";
        private static bool loggerRunning = false;
        static void Main(string[] args)
        {

            // Create a 30 min timer 
            var timer = new System.Timers.Timer(60000);

            // Hook up the Elapsed event for the timer.
            timer.Elapsed += IsSquadRunning;

            timer.Enabled = true;

            while (true)
            {
                Thread.Sleep(10000);
                Console.WriteLine("Still waiting on squad");
            }
        }

        private static void IsSquadRunning(object source, ElapsedEventArgs e)
        {
            bool squadRunning = Array.Exists(Process.GetProcesses(), m => m.ProcessName == SQUAD_PROCESS_NAME);
            if (squadRunning && loggerRunning)
            {
                return;
            }
            else if (squadRunning && !loggerRunning)
            {
                Console.WriteLine("Starting Squad Logger Program");
                loggerRunning = true;
            }
            else if (!squadRunning && loggerRunning)
            {
                Console.WriteLine("Stopped logger");
                loggerRunning = false;
            }
        }
    }
}
