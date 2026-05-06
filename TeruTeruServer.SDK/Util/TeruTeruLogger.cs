using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TeruTeruServer.SDK.Util
{
    public class TeruTeruLogger
    {
        public TeruTeruLogger Instance { get; } = new TeruTeruLogger();

        // 로깅 기능을 수행하는 내부 로거 인스턴스
        public static Logger LoggerInstance = new Logger();



        public static void LogInfo(string logMessage, [CallerMemberName] string className = "", [CallerLineNumber] int lineNumber = 0)
        {
            string log = $"[{DateTime.Now.ToString("yyyy/MM/dd/HH/mm/ss")}][CLASS = {className}][INFO]: {logMessage} \n";


            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(log);
            Console.ResetColor();

            LoggerInstance.SetLogMessage(LogLevel.INFO, log);
        }

        public static void LogAttention(string logMessage, [CallerMemberName] string className = "", [CallerLineNumber] int lineNumber = 0)
        {
            string log = $"[{DateTime.Now.ToString("yyyy/MM/dd/HH/mm/ss")}][CLASS = {className}][ATTENTION]: {logMessage} \n";


            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine(log);
            Console.ResetColor();

            LoggerInstance.SetLogMessage(LogLevel.ATTENTION, log);
        }

        public static void LogError(string errorMessage, [CallerMemberName] string className = "", [CallerLineNumber] int lineNumber = 0)
        {
            string log = $"[{DateTime.Now.ToString("yyyy/MM/dd/HH/mm/ss")}][CLASS = {className}][ERROR]: {errorMessage}\n";
            log += $"라인 번호: {lineNumber}\n";

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(log);
            Console.ResetColor();

            LoggerInstance.SetLogMessage(LogLevel.ERROR, log);

        }

        public static void LogWarning(string warningMessage, [CallerMemberName] string className = "", [CallerLineNumber] int lineNumber = 0)
        {
            string log = $"[{DateTime.Now.ToString("yyyy/MM/dd/HH/mm/ss")}][CLASS = {className}][WARNING]: {warningMessage}\n";
            log += $"라인 번호: {lineNumber}\n";


            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(log);
            Console.ResetColor();

            LoggerInstance.SetLogMessage(LogLevel.WARNING, log);
        }


        public static void LogInvisible(string logMessage, [CallerMemberName] string className = "", [CallerLineNumber] int lineNumber = 0)
        {
            string log = $"[{DateTime.Now.ToString("yyyy/MM/dd/HH/mm/ss")}][CLASS = {className}][INVISIBLE]: {logMessage}\n";
            log += $"라인 번호: {lineNumber}\n";

            // 하드웨어 관련 로그는 별도로 저장
            LoggerInstance.SetLogMessage(LogLevel.HARDWARE, log);
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

    public class Logger
    {
        private string _logDirectory;
        private string _logFileName;
        private string _hardwareLogFileName;
        private Queue<(int, string)> _logQueue = new Queue<(int, string)>();



        public Logger()
        {
            _logDirectory = "Logs";

            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            _logFileName = $"{DateTime.Now:yyyyMMdd}.log";
            _hardwareLogFileName = $"{DateTime.Now:yyyyMMdd}_hardware.log";

            Thread logMainLoop = new Thread(new ThreadStart(LogMainLoop));
            logMainLoop.IsBackground = true;
            logMainLoop.Start();
        }


        public void Log(string logMessage)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {logMessage}";
            File.AppendAllText(Path.Combine(_logDirectory, _logFileName), logEntry + Environment.NewLine);
        }
        public void LogHardware(string logMessage)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {logMessage}";
            File.AppendAllText(Path.Combine(_logDirectory, _hardwareLogFileName), logEntry + Environment.NewLine);
        }
        public void SetLogMessage(LogLevel logLevel, string logMessage)
        {
            _logQueue.Enqueue(((int)logLevel, logMessage));
        }

        // 로그 메인 루프는 1분에 한번 주기로 파일에 저장 한다.
        private void LogMainLoop()
        {
            while (true)
            {
                Thread.Sleep(60000);

                while (_logQueue.Count > 0)
                {
                    var log = _logQueue.Dequeue();

                    if ((LogLevel)log.Item1 == LogLevel.HARDWARE)
                    {
                        LogHardware(log.Item2);
                    }
                    else
                    {
                        Log(log.Item2);
                    }
                }
            }
        }

    }
}
