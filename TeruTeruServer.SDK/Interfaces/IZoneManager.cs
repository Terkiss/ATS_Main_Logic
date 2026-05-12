using System.Collections.Generic;
using TeruTeruServer.SDK.GameEngine;

namespace TeruTeruServer.SDK.Interfaces
{
    /// <summary>
    /// 게임 존(Zone)의 생명주기와 플레이어 입퇴장을 관리하는 인터페이스입니다.
    /// </summary>
    public interface IZoneManager
    {
        /// <summary>
        /// 새로운 존을 생성합니다.
        /// </summary>
        Zone CreateZone(string name, bool isInstance = false);

        /// <summary>
        /// 존을 삭제합니다.
        /// </summary>
        bool DestroyZone(int zoneId);

        /// <summary>
        /// ID로 존 정보를 조회합니다.
        /// </summary>
        Zone? GetZone(int zoneId);

        /// <summary>
        /// 모든 존 목록을 조회합니다.
        /// </summary>
        IReadOnlyList<Zone> GetAllZones();

        /// <summary>
        /// 플레이어를 특정 존에 입장시킵니다.
        /// </summary>
        bool JoinZone(int zoneId, int hostId);

        /// <summary>
        /// 플레이어를 현재 존에서 퇴장시킵니다.
        /// </summary>
        bool LeaveZone(int zoneId, int hostId);

        /// <summary>
        /// 플레이어가 현재 속한 존을 조회합니다.
        /// </summary>
        Zone? GetPlayerZone(int hostId);

        /// <summary>
        /// 플레이어를 다른 존으로 이동시킵니다.
        /// </summary>
        bool TransferPlayer(ZoneTransferRequest request);
    }
}
