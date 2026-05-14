using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;
using System;
using System.Diagnostics;

namespace TeruTeruServer.Commands
{
    public class HealthCommand : ICommand
    {
        private readonly ISessionManager _sessionManager;

        public HealthCommand(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public bool Execute(string[] args)
        {
            long tps = ServerMetrics.Tps;
            long totalProcessed = ServerMetrics.GetProcessedPacketCount();
            int currentSessions = _sessionManager.Players.Count;
            long memoryUsed = Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024);
            int threadCount = Process.GetCurrentProcess().Threads.Count;

            Console.WriteLine("=== Server Health Status ===");
            Console.WriteLine($"Current Sessions : {currentSessions}");
            Console.WriteLine($"TPS              : {tps} packets/sec");
            Console.WriteLine($"Total Packets    : {totalProcessed}");
            Console.WriteLine($"Memory Used      : {memoryUsed} MB");
            Console.WriteLine($"Thread Count     : {threadCount}");
            Console.WriteLine($"Uptime           : {DateTime.Now - Process.GetCurrentProcess().StartTime}");
            Console.WriteLine("============================");

            return true;
        }
    }
}
