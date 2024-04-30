using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace li.qubic.lib.transfer
{
    public class SendManyRecipient
    {
        public SendManyRecipient() { }
        public SendManyRecipient(byte[] recipientPublicKey, long amount)
        {
            RecipientPublicKey = recipientPublicKey;
            Amount = amount;
        }
        public SendManyRecipient(string recipientIdentity, long amount)
        {
            RecipientPublicKey = new QubicHelper().GetPublicKeyFromIdentity(recipientIdentity);
            Amount = amount;
        }

        // todo: create publickey type

        private byte[] recipient;
        public byte[] RecipientPublicKey {
            get
            {
                return recipient;
            }
            set
            {
                if (value.Length != QubicLibConst.IDENTITY_SIZE)
                    throw new ArgumentException($"Public Key must have {QubicLibConst.IDENTITY_SIZE} length");
                recipient = value;
            }
        }
        public long Amount { get; set; }
    }
}
