using TeruTeruServer.SDK.GameEngine;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Runtime.GameEngine
{
    /// <summary>
    /// 게임 입력(GameInput) 패킷의 수신 빈도를 검증하는 클래스입니다.
    /// </summary>
    public class InputFrequencyValidator
    {
        private readonly int _maxInputsPerTick;
        
        public InputFrequencyValidator(int maxInputsPerTick = 3)
        {
            _maxInputsPerTick = maxInputsPerTick;
        }
        
        /// <summary>
        /// 세션의 현재 틱 입력 횟수를 검증합니다.
        /// </summary>
        public SecurityEvent? Validate(ClientSession session, long currentTick)
        {
            if (session.LastInputTick == currentTick)
            {
                session.InputCountThisTick++;
                if (session.InputCountThisTick > _maxInputsPerTick)
                {
                    return new SecurityEvent
                    {
                        HostId = session.HostID,
                        EventType = "InputFlood",
                        Description = $"Input count {session.InputCountThisTick} exceeds limit {_maxInputsPerTick} at tick {currentTick}",
                        Severity = "Warning"
                    };
                }
            }
            else
            {
                session.LastInputTick = currentTick;
                session.InputCountThisTick = 1;
            }
            return null;
        }
    }
}
