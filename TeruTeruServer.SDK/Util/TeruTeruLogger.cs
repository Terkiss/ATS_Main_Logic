using System;
using System.IO;
using System.Runtime.CompilerServices;
using Serilog;
using Serilog.Formatting.Compact;

namespace TeruTeruServer.SDK.Util
{
    public class TeruTeruLogger
    {
        public TeruTeruLogger Instance { get; } = new TeruTeruLogger();

        private static readonly Serilog.ILogger _mainLogger;
        private static readonly Serilog.ILogger _hardwareLogger;

        static TeruTeruLogger()
        {
            string logDirectory = "Logs";
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            _mainLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy/MM/dd/HH/mm/ss}][CLASS = {ClassName}][{Level:u3}]: {Message:lj}\n{Exception}")
                .WriteTo.File(new CompactJsonFormatter(), Path.Combine(logDirectory, "log-.json"), rollingInterval: RollingInterval.Day)
                .CreateLogger();

            _hardwareLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(new CompactJsonFormatter(), Path.Combine(logDirectory, "hardware-.json"), rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        public static void LogInfo(string logMessage, [CallerMemberName] string className = "", [CallerLineNumber] int lineNumber = 0)
        {
            _mainLogger.ForContext("ClassName", className).ForContext("LineNumber", lineNumber).Information(logMessage);
        }

        public static void LogAttention(string logMessage, [CallerMemberName] string className = "", [CallerLineNumber] int lineNumber = 0)
        {
            _mainLogger.ForContext("ClassName", className).ForContext("LineNumber", lineNumber).Warning("[ATTENTION] " + logMessage);
        }

        public static void LogError(string errorMessage, [CallerMemberName] string className = "", [CallerLineNumber] int lineNumber = 0)
        {
            _mainLogger.ForContext("ClassName", className).ForContext("LineNumber", lineNumber).Error(errorMessage + " (라인 번호: " + lineNumber + ")");
        }

        public static void LogWarning(string warningMessage, [CallerMemberName] string className = "", [CallerLineNumber] int lineNumber = 0)
        {
            _mainLogger.ForContext("ClassName", className).ForContext("LineNumber", lineNumber).Warning(warningMessage + " (라인 번호: " + lineNumber + ")");
        }

        public static void LogInvisible(string logMessage, [CallerMemberName] string className = "", [CallerLineNumber] int lineNumber = 0)
        {
            _hardwareLogger.ForContext("ClassName", className).ForContext("LineNumber", lineNumber).Information(logMessage + " (라인 번호: " + lineNumber + ")");
        }
    }

    public enum LogLevel
    {
        INFO,
        ATTENTION,
        ERROR,
        WARNING,
        HARDWARE
    }
}
