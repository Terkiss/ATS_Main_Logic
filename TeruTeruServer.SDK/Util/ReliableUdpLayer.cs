using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TeruTeruServer.SDK.Util
{
    public class ReliableUdpLayer
    {
        private uint _nextSequenceToSend = 1;
        private uint _expectedSequenceToReceive = 1;
        private readonly ConcurrentDictionary<uint, byte[]> _receiveBuffer = new ConcurrentDictionary<uint, byte[]>();
        private readonly object _lock = new object();

        /// <summary>
        /// 송신용 패킷 생성: 데이터 앞에 4바이트 시퀀스 번호를 붙입니다.
        /// </summary>
        public byte[] Encapsulate(byte[] data)
        {
            byte[] packet = new byte[data.Length + 4];
            Buffer.BlockCopy(BitConverter.GetBytes(_nextSequenceToSend++), 0, packet, 0, 4);
            Buffer.BlockCopy(data, 0, packet, 4, data.Length);
            return packet;
        }

        /// <summary>
        /// 수신된 패킷 처리: 시퀀스 번호를 확인하고 순서대로 정렬된 페이로드를 반환합니다.
        /// </summary>
        public List<byte[]> ProcessIncoming(byte[] packet)
        {
            if (packet.Length < 4) return new List<byte[]>();

            uint seq = BitConverter.ToUInt32(packet, 0);
            byte[] payload = new byte[packet.Length - 4];
            Buffer.BlockCopy(packet, 4, payload, 0, payload.Length);

            var result = new List<byte[]>();

            lock (_lock)
            {
                if (seq < _expectedSequenceToReceive)
                {
                    // 중복 또는 이미 지난 패킷 무시
                    return result;
                }

                _receiveBuffer.TryAdd(seq, payload);

                // 순서대로 정렬된 패킷들 추출
                while (_receiveBuffer.TryRemove(_expectedSequenceToReceive, out var nextPayload))
                {
                    result.Add(nextPayload);
                    _expectedSequenceToReceive++;
                }
            }

            return result;
        }
    }
}
