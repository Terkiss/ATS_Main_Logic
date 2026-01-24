using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TeruTeruServer.ManageLogic.Protocol
{

    /// <summary>
    /// 서버 연결을 위한 데이터 구조체입니다.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ConnectionData
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string Guid; // 서버 연결 확인을 위한 고유 GUID
    }

    /// <summary>
    /// 일반적인 데이터 전송을 위한 구조체입니다.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SendData
    {
        public int Index; // 플레이어 또는 객체의 인덱스
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] Data; // 실제 데이터 바이트 배열
    }


    /// <summary>
    /// 이미지 데이터 전송을 위한 구조체입니다.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SendImageData
    {
        public int HostID; // 호스트 식별 ID

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] UserID; // 사용자 ID (byte 배열 형태)

        public int ImgSize; // 이미지 데이터 크기

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2097152)]
        public byte[] Data; // 이미지 이진 데이터
    }

    /// <summary>
    /// YOLO 객체 탐지 결과를 담는 구조체입니다.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack =1)]
    public struct YoloDetectResult
    {
        public int HostID; // 호스트 식별 ID

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
        public string UserID; // 사용자 ID (문자열 형태)

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2097152)]
        public byte[] Data; // 관련 이미지 데이터 (필요 시)

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1048576)]
        public string DetectionResult; // JSON 형식의 탐지 결과 문자열
    }


    /// <summary>
    /// 채팅 참여 데이터 구조체입니다.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ChatData
    {
        public int Index; // 보낸 플레이어 인덱스

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string Sender; // 발신자 이름

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 2048)]
        public string Message; // 채팅 메시지 내용
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
