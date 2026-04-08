using TeruTeruServer.SDK.Interfaces;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeruTeruServer.SDK.Protocol;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Commands
{
    /// <summary>
    /// 백그라운드 워커 쓰레드를 시작하여 이미지 분석 요청을 처리하는 명령어 클래스입니다.
    /// </summary>
    public class WorkerStartCommand : ICommand
    {
        private RpcProxy _rpcProxy;
        private bool _isRunning = false; // 워커 쓰레드 실행 여부 플래그

        public WorkerStartCommand()
        {
            _rpcProxy = new RpcProxy();
        }

        public bool Execute(string[] args)
        {
            if (_isRunning)
            {
                return true; 
            }
            Thread workerThread = new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(30);
                    // 이미지 분석 대기 큐에서 데이터를 하나씩 꺼내 탐지 요청
                    if (ServerMemory.GetImageWork_PreOrder_Queue(out SendImageData preOrderItem))
                        _rpcProxy.RequestObjectDetect(preOrderItem);

                    // 분석 완료 큐에 결과가 있으면 로그 출력
                    if (ServerMemory.GetImageWork_Complete_Queue(out YoloDetectResult completeItem))
                    {
                        TeruTeruLogger.LogInfo($"분석 완료 유저: {completeItem.UserID}, 호스트: {completeItem.HostID}");
                        TeruTeruLogger.LogInfo("탐지 결과 JSON: " + completeItem.DetectionResult);
                    }
                }
            });
            workerThread.IsBackground = true;
            workerThread.Start();
            _isRunning = true;
            return true;
        }
    }
}
