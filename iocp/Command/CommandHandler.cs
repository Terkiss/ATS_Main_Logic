using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeruTeruServer.Command
{
    /// <summary>
    /// 콘솔로부터 입력된 명령어를 해석하고 실행하는 핸들러 클래스입니다.
    /// </summary>
    public class CommandHandler
    {
        // 명령어 이름을 키로, 명령어 실행 객체를 값으로 가지는 딕셔너리
        private readonly Dictionary<string, ICommand> _commands = new();

        public CommandHandler(MainServer server)
        {
            _commands["exit"] = new ExitCommand();
            _commands["Queue_Count"] = new QueueCountCommand();
            _commands["2"] = new ImageDumpCommand();
            _commands["Worker_Start"] = new WorkerStartCommand();
            // ... 기타 명령어 등록
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
