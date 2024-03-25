using li.qubic.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SendMany.Model
{

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct SendToManyV1_input
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (25*32))]
        public byte[] addresses;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (25))]
        public long[] amounts;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct SendManyRequest
    {
        public RequestResponseHeader header;
        public BaseTransaction tx;
        public SendToManyV1_input input;
    }
}
