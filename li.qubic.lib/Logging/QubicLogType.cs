using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace li.qubic.lib.Logging
{
    public enum QubicLogMessageType
    {
        QuTransfer=0,
        AssetIssuance=1,
        AssetOwnershipChange=2,
        AssetPossessionChange=3,
        ContractError=4,
        ContractWarning=5,
        ContractInfo=6,
        ContractDebug=7,
        Burning=8,


        CustumMessage=255
    }
}
