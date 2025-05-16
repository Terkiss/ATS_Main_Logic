using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeruTeruServer.Command
{
    public interface ICommand
    {
        /// <summary>
        /// 명령어 실행
        /// </summary>
        /// <param name="args">명령어 인자</param>
        /// <returns>프로그램 계속 여부</returns>
        bool Execute(string[] args);
    }

}
