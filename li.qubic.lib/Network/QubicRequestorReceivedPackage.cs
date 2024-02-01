using li.qubic.lib;
using li.qubic.lib.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace li.qubic.lib.Network
{
    public class QubicRequestorReceivedPackage
    {
        public QubicRequestorReceivedPackage(IPAddress peer, RequestResponseHeader header, byte[] payload)
        {
            Peer = peer;
            Header = header;
            Payload = payload;
        }

        public Toutput GetPayload<Toutput>()
            where Toutput : struct
        {
            return Marshalling.Deserialize<Toutput>(this.Payload);
        }

        public RequestResponseHeader Header { get; set; }
        public byte[] Payload { get; set; }

        public IPAddress Peer { get; set; }
    }
}
