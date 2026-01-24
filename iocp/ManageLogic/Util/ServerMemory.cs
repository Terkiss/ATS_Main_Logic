using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using TeruTeruServer.ManageLogic.Protocol;

namespace TeruTeruServer.ManageLogic.Util
{
    /// <summary>
    /// 서버 메모리 클래스
    /// </summary>
    public class ServerMemory
    {
        public static MainServer MainServer { get; set; }
        public static object HostIDGeneratorLock = new object();

        private static Dictionary<int, ClientSession> _hosts = new Dictionary<int, ClientSession>();
        private static Dictionary<string, int> _gameID2HostID = new Dictionary<string, int>();

        private static ConcurrentQueue<SendImageData> _imageWorkPreOrderQueue = new ConcurrentQueue<SendImageData>();
        private static ConcurrentQueue<YoloDetectResult> _imageWorkCompleteQueue = new ConcurrentQueue<YoloDetectResult>();
        private static int _currentHostID = 0;



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

        public static Object HostsLock = new Object();
        public static object GameID2HostIDLock = new object();

        public static List<ClientSession> GetClientSessions()
        {
            lock (HostsLock)
            {
                return new List<ClientSession>(_hosts.Values);
            }
        }


        public static Dictionary<int, ClientSession> AddHostToDictionary(int hostID, ClientSession host)
        {
            lock (HostsLock)
            {
                _hosts.Add(hostID, host);
            }
            return _hosts;
        }

        public static void RemoveHostFromDictionary(int hostID)
        {
            lock (HostsLock)
            {
                _hosts.Remove(hostID);
            }
        }

        public static void AddGameIDToDictionary(string gameID, int hostID)
        {
            lock (GameID2HostIDLock)
            {
                if (!_gameID2HostID.ContainsKey(gameID))
                {
                    _gameID2HostID.Add(gameID, hostID);
                }
            }
        }

        public static void RemoveGameIDFromDictionary(string gameID)
        {
            lock (GameID2HostIDLock)
            {
                if (_gameID2HostID.ContainsKey(gameID))
                {
                    _gameID2HostID.Remove(gameID);
                }
            }
        }

        public static void RemoveGameIDFromDictionary(int hostID)
        {
            lock (GameID2HostIDLock)
            {
                string key = string.Empty;
                foreach (var item in _gameID2HostID)
                {
                    if (item.Value == hostID)
                    {
                        key = item.Key;
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(key))
                {
                    _gameID2HostID.Remove(key);
                }
                _hosts.Remove(hostID);
            }
        }


        public static ClientSession FindClientSession(int hostID)
        {
            lock (HostsLock)
            {
                if (_hosts.ContainsKey(hostID)) {
                    return _hosts[hostID];
                }
                return null;
            }
        }

        public static ClientSession FindClientSession(string gameID)
        {
            lock (GameID2HostIDLock)
            {
                if (_gameID2HostID.ContainsKey(gameID))
                {
                    return FindClientSession(_gameID2HostID[gameID]);
                }
                return null;
            }
        }


        public static void AddImageWork_PreOrder_Queue(SendImageData imageData)
        {
            _imageWorkPreOrderQueue.Enqueue(imageData);
        }

        public static void AddImageWork_Complete_Queue(YoloDetectResult imageData)
        {
            _imageWorkCompleteQueue.Enqueue(imageData);
        }

        public static SendImageData GetImageWork_PreOrder_Queue()
        {
            if (_imageWorkPreOrderQueue.TryDequeue(out SendImageData imageData))
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
   
        public static YoloDetectResult GetImageWork_Complete_Queue()
        {
            if (_imageWorkCompleteQueue.TryDequeue(out YoloDetectResult imageData))
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

