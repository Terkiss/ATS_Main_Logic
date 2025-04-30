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
        /// <summary>
        /// 호스트 ID 생성기 락 오브젝트
        /// </summary>
        public static object HostIDGeneratorLock = new object();

        private static Dictionary<int, ClientSession> hosts = new Dictionary<int, ClientSession>();
        private static Dictionary<string, int> gameID2HostID = new Dictionary<string, int>();


        private static ConcurrentQueue<SendImageData> imageWork_PreOrder_Queue = new ConcurrentQueue<SendImageData>();
        private static ConcurrentQueue<SendImageData> imageWork_Complete_Queue = new ConcurrentQueue<SendImageData>();


        /// <summary>
        /// 호스트 ID 생성기
        /// </summary>
        public static int GetHostID
        {
            get
            {
                lock (HostIDGeneratorLock)
                {
                    return currentHostID++;
                }
            }
        } private static int currentHostID = 0;



        public static Object HostsLock = new Object();
        public static object GameID2HostIDLock = new object();

        public static List<ClientSession> GetClientSessions()
        {
            lock (HostsLock)
            {
                return new List<ClientSession>(hosts.Values);
            }
        }


        public static Dictionary<int, ClientSession> AddHostToDictionary(int hostID, ClientSession host)
        {
            lock (HostsLock)
            {
                hosts.Add(hostID, host);
            }
            return hosts;
        }
        public static void RemoveHostFromDictionary(int hostID)
        {
            lock (HostsLock)
            {
                hosts.Remove(hostID);
            }
        }

        public static void AddGameIDToDictionary(string gameID, int hostID)
        {
            lock (GameID2HostIDLock)
            {
                if (!gameID2HostID.ContainsKey(gameID))
                {
                    gameID2HostID.Add(gameID, hostID);
                }
            }
        }
        public static void RemoveGameIDFromDictionary(string gameID)
        {
            lock (GameID2HostIDLock)
            {
                if (gameID2HostID.ContainsKey(gameID))
                {
                    gameID2HostID.Remove(gameID);
                }
            }
        }
        public static void RemoveGameIDFromDictionary(int hostID)
        {
            lock (GameID2HostIDLock)
            {
                string key = string.Empty;
                foreach (var item in gameID2HostID)
                {
                    if (item.Value == hostID)
                    {
                        key = item.Key;
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(key))
                {
                    gameID2HostID.Remove(key);
                }
                hosts.Remove(hostID);
            }
        }


        public static ClientSession FindClientSession(int hostID)
        {
            lock (HostsLock)
            {
                if (hosts.ContainsKey(hostID)) {
                    return hosts[hostID];
                }
                return null;
            }
        }
        public static ClientSession FindClientSession(string gameID)
        {
            lock (GameID2HostIDLock)
            {
                if (gameID2HostID.ContainsKey(gameID))
                {
                    return FindClientSession(gameID2HostID[gameID]);
                }
                return null;
            }
        }


        public static void AddImageWork_PreOrder_Queue(SendImageData imageData)
        {
            imageWork_PreOrder_Queue.Enqueue(imageData);
        }
        public static void AddImageWork_Complete_Queue(SendImageData imageData)
        {
            imageWork_Complete_Queue.Enqueue(imageData);
        }
        public static SendImageData GetImageWork_PreOrder_Queue()
        {
            if (imageWork_PreOrder_Queue.TryDequeue(out SendImageData imageData))
            {
                return imageData;
            }
            return default;
        }
        public static bool GetImageWork_PreOrder_Queue(out SendImageData data)
        {
            bool check = imageWork_PreOrder_Queue.TryDequeue(out SendImageData imageData);
            data = imageData;
            return check;
        }
   
        public static SendImageData GetImageWork_Complete_Queue()
        {
            if (imageWork_Complete_Queue.TryDequeue(out SendImageData imageData))
            {
                return imageData;
            }
            return default;
        }
        public static bool GetImageWork_Complete_Queue(out SendImageData data)
        {
            bool check = imageWork_Complete_Queue.TryDequeue(out SendImageData imageData);
            data = imageData;
            return check;
        }
        public static int GetImageWork_PreOrder_QueueCount()
        {
            return imageWork_PreOrder_Queue.Count;
        }
        public static int GetImageWork_Complete_QueueCount()
        {
            return imageWork_Complete_Queue.Count;
        }

    }
}

