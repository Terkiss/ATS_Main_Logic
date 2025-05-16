using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeruTeruServer.ManageLogic.Util;

namespace TeruTeruServer.Command
{
    public class QueueCountCommand : ICommand
    {
        public bool Execute(string[] args)
        {
            var preOrderCount = ServerMemory.GetImageWork_PreOrder_QueueCount();
            var completeCount = ServerMemory.GetImageWork_Complete_QueueCount();
            TeruTeruLogger.LogInfo($"preOrderCount : {preOrderCount}, CompleteCount : {completeCount}");

            return true; // 프로그램 계속
        }
    }
}
