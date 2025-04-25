using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TeruTeruServer.ManageLogic.Util
{
    public class TeruTeruLogger
    {
        public TeruTeruLogger Instance { get; } = new TeruTeruLogger();
        public static Logger logger = new Logger();



        public static void LogInfo(string logMessage, [CallerMemberName] string className = "", [CallerLineNumber] int lineNumber = 0)
        {
            string log = $"[{DateTime.Now.ToString("yyyy/MM/dd/HH/mm/ss")}][CLASS = {className}][INFO]: {logMessage} \n";


            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(log);
            Console.ResetColor();

            logger.SetLogMessage(LogLevel.INFO, log);
        }

        public static void LogAttention(string logMessage, [CallerMemberName] string className = "", [CallerLineNumber] int lineNumber = 0)
        {
            string log = $"[{DateTime.Now.ToString("yyyy/MM/dd/HH/mm/ss")}][CLASS = {className}][ATTENTION]: {logMessage} \n";


            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine(log);
            Console.ResetColor();

            logger.SetLogMessage(LogLevel.ATTENTION, log);
        }

        public static void LogError(string errorMessage, [CallerMemberName] string className = "", [CallerLineNumber] int lineNumber = 0)
        {
            string log = $"[{DateTime.Now.ToString("yyyy/MM/dd/HH/mm/ss")}][CLASS = {className}][ERROR]: {errorMessage}\n";
            log += $"line number: {lineNumber}\n";

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(log);
            Console.ResetColor();

            logger.SetLogMessage(LogLevel.ERROR, log);

        }

        public static void LogWarning(string warningMessage, [CallerMemberName] string className = "", [CallerLineNumber] int lineNumber = 0)
        {
            string log = $"[{DateTime.Now.ToString("yyyy/MM/dd/HH/mm/ss")}][CLASS = {className}][WARNING]: {warningMessage}\n";
            log += $"line number: {lineNumber}\n";


            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(log);
            Console.ResetColor();

            logger.SetLogMessage(LogLevel.WARNING, log);
        }


        public static void LogInvisible(string logMessage, [CallerMemberName] string className = "", [CallerLineNumber] int lineNumber = 0)
        {
            string log = $"[{DateTime.Now.ToString("yyyy/MM/dd/HH/mm/ss")}][CLASS = {className}][INVISIBLE]: {logMessage}\n";
            log += $"line number: {lineNumber}\n";

            logger.SetLogMessage(LogLevel.HARDWARE, log);
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
        private string logDirectory;
        private string logFileName;
        private string hardwareLogFileName;
        private Queue<(int, string)> LogQuque = new Queue<(int, string)>();



        public Logger()
        {
            // 로그 파일이 저장될 디렉토리 설정
            logDirectory = "Logs";

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // 로그 파일 이름을 날짜 기반으로 생성
            logFileName = $"{DateTime.Now:yyyyMMdd}.log";
            hardwareLogFileName = $"{DateTime.Now:yyyyMMdd}_hardware.log";

            Thread logMainLoop = new Thread(new ThreadStart(LogMainLoop));
            logMainLoop.Start();
        }


        public void Log(string logMessage)
        {
            // 날짜와 메시지를 로그 파일에 추가
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {logMessage}";
            File.AppendAllText(Path.Combine(logDirectory, logFileName), logEntry + Environment.NewLine);
        }
        public void LogHardware(string logMessage)
        {
            // 날짜와 메시지를 로그 파일에 추가
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {logMessage}";
            File.AppendAllText(Path.Combine(logDirectory, hardwareLogFileName), logEntry + Environment.NewLine);
        }
        public void SetLogMessage(LogLevel logLevel, string logMessage)
        {
            // 로그 메세지를 큐에 담는다
            LogQuque.Enqueue(((int)logLevel, logMessage));

        }

        // 로그 메인 루프는 1분에 한번 주기로 파일에 저장 한다.
        private void LogMainLoop()
        {
            while (true)
            {

                Thread.Sleep(60000);

                // 큐에 담긴 로그 메세지를 파일에 저장한다.
                while (LogQuque.Count > 0)
                {
                    var log = LogQuque.Dequeue();

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
