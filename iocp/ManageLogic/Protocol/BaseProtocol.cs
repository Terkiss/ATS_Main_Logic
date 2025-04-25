using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeruTeruServer.ManageLogic.Protocol
{
    public abstract class BaseProtocol
    {
        public abstract ProtocolSelect ProtocolSelector { get; set; }
        public abstract int Command { get; set; }
        public abstract int HostId { get; set; }
    }
}
