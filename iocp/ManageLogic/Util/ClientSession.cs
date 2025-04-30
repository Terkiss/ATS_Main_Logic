using TeruTeruServer.ManageLogic.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TeruTeruServer.ManageLogic.Util
{
    public class ClientSession
    {
        public int HostID { get; set; }
        public string GameID { get; set; }
        public string HostIP { get; set; }
        public int HostPort { get; set; }
        public Socket clientSocket { get; set; }


        public String Role { get; set; }
        public String ClientName { get; set; }

    




        public ClientSession(int hostID, Socket clientSocket, string gameID)
        {
            HostID = hostID;
            GameID = gameID;

            this.clientSocket = clientSocket;
            Clear();

            ServerMemory.AddHostToDictionary(hostID, this);
            ServerMemory.AddGameIDToDictionary(gameID, hostID);
        }

        public void Clear()
        {


        }

   

        //public PlayerData GetPlayerData()
        //{
        //    PlayerData playerData = new PlayerData();
        //    playerData.Index = HostID;
        //    playerData.AnimationState = animState;
        //    playerData.PositionX = x_pos;
        //    playerData.PositionY = y_pos;
        //    playerData.PositionZ = z_pos;
        //    playerData.RotationX = x_rot;
        //    playerData.RotationY = y_rot;
        //    playerData.RotationZ = z_rot;
        //    playerData.RotationW = w_rot;
        //    playerData.SkinData = skinData;
        //    playerData.Gender = gender;
        //    return playerData;
        //}
    }
}