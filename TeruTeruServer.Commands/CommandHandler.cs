using TeruTeruServer.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeruTeruServer.Commands
{
    /// <summary>
    /// 콘솔로부터 입력된 명령어를 해석하고 실행하는 핸들러 클래스입니다.
    /// </summary>
    public class CommandHandler
    {
        private readonly Dictionary<string, ICommand> _commands = new();

        public CommandHandler(IMessageSender messageSender, ISessionManager sessionManager)
        {
            _commands["exit"] = new ExitCommand();
            _commands["Queue_Count"] = new QueueCountCommand();
            // 필요한 경우 커맨드 생성 시 인터페이스 전달
            _commands["2"] = new ImageDumpCommand();
            _commands["Worker_Start"] = new WorkerStartCommand();
        }

        public bool Handle(string input)
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return true;

            var cmd = parts[0];
            var args = parts.Skip(1).ToArray();

            if (_commands.TryGetValue(cmd, out var command))
                return command.Execute(args);

            Console.WriteLine($"알 수 없는 명령어: {cmd}");
            return true;
        }
    }

}
