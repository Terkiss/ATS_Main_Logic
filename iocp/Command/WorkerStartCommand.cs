using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeruTeruServer.ManageLogic.Protocol;
using TeruTeruServer.ManageLogic.Util;

namespace TeruTeruServer.Command
{
    public class WorkerStartCommand : ICommand
    {

        private RpcProxy rpcProxy;
        private bool isRunning = false;
        public WorkerStartCommand()
        {
            rpcProxy = new RpcProxy();
        }

        public bool Execute(string[] args)
        {
            if (isRunning)
            {
                return true; // 이미 실행중인 경우
            }
            Thread workerThread = new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(30);
                    if (ServerMemory.GetImageWork_PreOrder_Queue(out SendImageData preOrderItem))
                        rpcProxy.RequestObjectDetect(preOrderItem);

                    if (ServerMemory.GetImageWork_Complete_Queue(out YoloDetectResult completeItem))
                    {
                        //이 구간 은 완료 콜백
                        TeruTeruLogger.LogInfo($"CompleteItem : {completeItem.UserID}, {completeItem.hostID}");

                        TeruTeruLogger.LogInfo("JSON Result: " + completeItem.DetectionResult);

                    }
                    

                }
            });
            workerThread.Start();
            isRunning = true;
            return true; // 프로그램 계속
        }
    }
}
