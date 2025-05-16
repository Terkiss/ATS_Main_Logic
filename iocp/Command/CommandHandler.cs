using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeruTeruServer.Command
{
    public class CommandHandler
    {
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
