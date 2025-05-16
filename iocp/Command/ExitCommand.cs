using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeruTeruServer.Command
{
    public class ExitCommand : ICommand
    {
        public bool Execute(string[] args)
        {
            return false; // 프로그램 종료
        }
    }
}
