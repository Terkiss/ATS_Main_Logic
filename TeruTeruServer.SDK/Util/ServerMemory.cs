using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TeruTeruServer.SDK.Protocol;
using TeruTeruServer.SDK.Enums;

namespace TeruTeruServer.SDK.Util
{
    /// <summary>
    /// 서버 메모리 클래스
    /// </summary>
    public class ServerMemory
    {
        public static object HostIDGeneratorLock = new object();

        private static ConcurrentDictionary<int, ClientSession> _hosts = new ConcurrentDictionary<int, ClientSession>();
        private static ConcurrentDictionary<string, int> _gameID2HostID = new ConcurrentDictionary<string, int>();

        private static ConcurrentQueue<SendImageData> _imageWorkPreOrderQueue = new ConcurrentQueue<SendImageData>();
        private static ConcurrentQueue<YoloDetectResult> _imageWorkCompleteQueue = new ConcurrentQueue<YoloDetectResult>();

        /// <summary>
        /// 서버 자신을 지칭하는 예약된 고정 Host ID
        /// </summary>
        public const int SERVER_HOST_ID = 0;

        private static int _currentHostID = 1;

        /// <summary>
        /// 새로운 고유 호스트 ID를 생성합니다.
        /// </summary>
        public static int GetHostID
        {
            get
            {
                lock (HostIDGeneratorLock)
                {
                    return _currentHostID++;
                }
            }
        }

        public static List<ClientSession> GetClientSessions()
        {
            return _hosts.Values.ToList();
        }

        public static void AddHostToDictionary(int hostID, ClientSession host)
        {
            _hosts.TryAdd(hostID, host);
        }

        public static void RemoveHostFromDictionary(int hostID)
        {
            _hosts.TryRemove(hostID, out _);
        }

        public static void AddGameIDToDictionary(string gameID, int hostID)
        {
            _gameID2HostID.TryAdd(gameID, hostID);
        }

        public static void RemoveGameIDFromDictionary(string gameID)
        {
            _gameID2HostID.TryRemove(gameID, out _);
        }

        public static void RemoveGameIDFromDictionary(int hostID)
        {
            var keyToRemove = _gameID2HostID.FirstOrDefault(x => x.Value == hostID).Key;
            
            if (!string.IsNullOrEmpty(keyToRemove))
            {
                _gameID2HostID.TryRemove(keyToRemove, out _);
            }

            _hosts.TryRemove(hostID, out _);
        }

        public static ClientSession? FindClientSession(int hostID)
        {
            if (_hosts.TryGetValue(hostID, out var session))
            {
                return session;
            }
            return null;
        }

        public static ClientSession? FindClientSession(string gameID)
        {
            if (_gameID2HostID.TryGetValue(gameID, out int hostID))
            {
                return FindClientSession(hostID);
            }
            return null;
        }

        public static void AddImageWork_PreOrder_Queue(SendImageData imageData)
        {
            _imageWorkPreOrderQueue.Enqueue(imageData);
        }

        public static void AddImageWork_Complete_Queue(YoloDetectResult imageData)
        {
            _imageWorkCompleteQueue.Enqueue(imageData);
        }

        public static SendImageData? GetImageWork_PreOrder_Queue()
        {
            if (_imageWorkPreOrderQueue.TryDequeue(out SendImageData? imageData))
            {
                return imageData;
            }
            return default;
        }

        public static bool GetImageWork_PreOrder_Queue(out SendImageData data)
        {
            bool check = _imageWorkPreOrderQueue.TryDequeue(out SendImageData imageData);
            data = imageData;
            return check;
        }

        public static YoloDetectResult? GetImageWork_Complete_Queue()
        {
            if (_imageWorkCompleteQueue.TryDequeue(out YoloDetectResult? imageData))
            {
                return imageData;
            }
            return default;
        }

        public static bool GetImageWork_Complete_Queue(out YoloDetectResult data)
        {
            bool check = _imageWorkCompleteQueue.TryDequeue(out YoloDetectResult imageData);
            data = imageData;
            return check;
        }

        public static int GetImageWork_PreOrder_QueueCount()
        {
            return _imageWorkPreOrderQueue.Count;
        }

        public static int GetImageWork_Complete_QueueCount()
        {
            return _imageWorkCompleteQueue.Count;
        }
    }
}

