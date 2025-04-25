using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TeruTeruServer.ManageLogic.Protocol
{

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ConnectionData
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string guid;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SendData
    {
        public int index;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SendImageData
    {
        public int hostID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] userID;

        public int imgSize;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2097152)]
        public byte[] data;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ChatData
    {
        public int index;



        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string sender;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
        public string message;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PlayerData
    {
        public int Index;              // 플레이어 고유 인덱스
        public float PositionX;        // 위치 X
        public float PositionY;        // 위치 Y
        public float PositionZ;        // 위치 Z

        public float RotationX;        // 회전 X
        public float RotationY;        // 회전 Y
        public float RotationZ;        // 회전 Z
        public float RotationW;        // 회전 W

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public int[] AnimationState;   // 애니메이션 상태
        public bool Gender;            // 성별

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public int[] SkinData;         // 스킨 데이터


        // PlayerData를 바이트 배열로 변환
        public byte[] ToBytes()
        {
            // 구조체 데이터를 직렬화하는 방법 (간단 예시)
            byte[] data = new byte[1024];
            Buffer.BlockCopy(BitConverter.GetBytes(Index), 0, data, 0, 4);
            // 나머지 데이터를 추가로 직렬화
            return data;
        }
    }

}
