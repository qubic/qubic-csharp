using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace li.qubic.lib
{
    public static class QubicLibConst
    {
        public static int BUFFER_SIZE = 1048576 * 4;
        public static int SIGNATURE_SIZE = 64;
        public static int PORT = 21841;
        public static int NUMBER_OF_EXCHANGED_PEERS = 4;
        public static int IDENTITY_SIZE = 32;
        public static int DIGEST_SIZE = 32;

        /* TX stuff */
        public static int MAX_TRANSACTION_SIZE = 1024;

        /* SC stuff */

        #region SC's
        public static int SC_QUTIL_CONTRACT_ID = 4;
        #endregion

        public static long SC_QUTIL_SENDMANY_FEE = 10;
    }
}
