using li.qubic.lib.Helper;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace li.qubic.lib.transfer
{
    public class TransferFactory
    {
        
        /// <summary>
        /// create an instance to build a qubic transaction
        /// </summary>
        /// <param name="type">choose which type of transaction you would like to create</param>
        public TransferFactory(TransactionType type)
        {
            _transfer.TransactionType = type;
            _transfer.InputType = (ushort)TransactionType.SendMany;
        }

        /// <summary>
        /// empty constructor to be able to create custom transactions
        /// </summary>
        public TransferFactory()
        {

        }

        /// <summary>
        /// internal class which holds all information to build the transaction
        /// </summary>
        private class InternalTransfer
        {
            /// <summary>
            /// set the tye of transaction. if NULL factory tries to auto detect transaction type
            /// </summary>
            public TransactionType? TransactionType { get; set; }

            public long Amount { get; set; }
            private byte[] _sender;
            public byte[] SenderPublicKey
            {
                get
                {
                    return _sender;
                }
                set
                {
                    if (value.Length != QubicLibConst.IDENTITY_SIZE)
                        throw new ArgumentException($"Public Key must have {QubicLibConst.IDENTITY_SIZE} length");
                    _sender = value;
                }
            }
            private byte[] _receiver { get; set; }
            public byte[] ReceiverPublicKey
            {
                get
                {
                    return _receiver;
                }
                set
                {
                    if (value.Length != QubicLibConst.IDENTITY_SIZE)
                        throw new ArgumentException($"Public Key must have {QubicLibConst.IDENTITY_SIZE} length");
                    _receiver = value;
                }
            }
            public uint TargetTick { get; set; }
            public ushort InputType { get; set; }
            public ushort InputSize { get; set; }

            public byte[] InputPayload { get; set; }

            public List<SendManyRecipient> SendManyRecipients { get; set; } = new List<SendManyRecipient>();

            public long GetAmount()
            {
                if(TransactionType == null || TransactionType == transfer.TransactionType.Default)
                    return Amount;
                if (TransactionType == transfer.TransactionType.SendMany)
                    return this.SendManyRecipients.Sum(s => s.Amount) + QubicLibConst.SC_QUTIL_SENDMANY_FEE;

                throw new Exception("Amount or Type unknown");
            }

            public ushort GetInputSize()
            {
                if (TransactionType == null || TransactionType == transfer.TransactionType.Default)
                    return 0;
                if (TransactionType == transfer.TransactionType.SendMany)
                    return 1000;

                throw new Exception("InputSize or Type unknown");
            }

            public BaseTransaction GetBaseTransaction()
            {
                return new BaseTransaction
                {
                    amount = GetAmount(),
                    destinationPublicKey = ReceiverPublicKey,
                    inputSize = GetInputSize(),
                    inputType = InputType,
                    sourcePublicKey = SenderPublicKey,
                    tick = TargetTick
                };
            }

            internal List<string> CheckForErrors()
            {
                // todo: error checking

                return new List<string>();
            }

            internal int GetTransactionSize()
            {
                return Marshal.SizeOf<BaseTransaction>() + this.GetInputSize() + QubicLibConst.SIGNATURE_SIZE;
            }

            internal SendToManyV1_input GetSendManyStruct()
            {
                var destinationAddresses = new byte[25][];
                var destinationAmounts = new long[25];
                var addressIndex = 0;
                foreach (var address in SendManyRecipients)
                {
                    destinationAmounts[addressIndex] = address.Amount;
                    destinationAddresses[addressIndex++] = address.RecipientPublicKey;
                }

                // fill rest of addresses with 0
                for (; addressIndex < 25; addressIndex++)
                {
                    destinationAmounts[addressIndex] = 0;
                    destinationAddresses[addressIndex] = new byte[32];
                }

                return new SendToManyV1_input
                {
                    addresses = destinationAddresses.ToQubicArray(25, 32),
                    amounts = destinationAmounts,
                };
            }
        }

        private InternalTransfer _transfer { get; set; } = new InternalTransfer();
        private QubicHelper _helper = new QubicHelper();


        /// <summary>
        /// set the amount to be transfered
        /// if using send many, do not specifiy custom amount
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public TransferFactory SetAmount(long amount)
        {
            _transfer.Amount = amount;
            return this;
        }

        /// <summary>
        /// set the sender of the transaction. can be left empty if you want to set it automatically when signing the tx
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        public TransferFactory SetSenderIdentity(string identity)
        {
            _transfer.SenderPublicKey = _helper.GetPublicKeyFromIdentity(identity);
            return this;
        }

        /// <summary>
        /// set the receiver for the transaction
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        public TransferFactory SetReceiverIdentity(string identity)
        {
            _transfer.ReceiverPublicKey = _helper.GetPublicKeyFromIdentity(identity);
            return this;
        }

        /// <summary>
        /// set the receiver for the transaction
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        public TransferFactory SetReceiverIdentity(byte[] publicKey)
        {
            _transfer.ReceiverPublicKey = publicKey;
            return this;
        }

        /// <summary>
        /// set the tick when the transaction should be executed
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        public TransferFactory SetTargetTick(uint tick)
        {
            _transfer.TargetTick = tick;
            return this;
        }
        /// <summary>
        /// set the type of the transaction
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public TransferFactory SetType(TransactionInputType type)
        {
            _transfer.InputType = (ushort)type;
            return this;
        }
        /// <summary>
        /// set the type of transaction with plain short
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public TransferFactory SetType(ushort type)
        {
            _transfer.InputType = (ushort)type;
            return this;
        }

        /// <summary>
        /// add a custom input payload to the transaction
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        public TransferFactory SetInputPayload(byte[] payload)
        {
            if (payload != null && payload.Length > QubicLibConst.MAX_TRANSACTION_SIZE)
                throw new ArgumentException($"PAYLOAD must be NULL or <= {QubicLibConst.MAX_TRANSACTION_SIZE}");

            _transfer.InputPayload = payload;
            _transfer.InputSize = (ushort)payload.Length;
            return this;
        }

        public TransferFactory AddSendManyRecipients(List<SendManyRecipient> recipients)
        {
            if(_transfer.SendManyRecipients.Count + recipients.Count > 25)
                throw new ArgumentException("Send Many only allowes 25 recipients");

            foreach (var recipient in recipients)
            {
                _transfer.SendManyRecipients.Add(recipient);
            }
            return this;
        }

        /// <summary>
        /// add a send many recipient
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public TransferFactory AddSendManyRecipient(string identity, long amount)
        {
            if (_transfer.SendManyRecipients.Count >= 25)
                throw new ArgumentException("Send Many only allowes 25 recipients");

            _transfer.SendManyRecipients.Add(new SendManyRecipient(identity, amount));
            return this;
        }

        public byte[] BuildAndSign(string senderSeed)
        {
            if (string.IsNullOrEmpty(senderSeed))
                throw new ArgumentException("senderSeed must not be null or empty");
            if (senderSeed.Length != 55 || !Regex.IsMatch(senderSeed, "[a-z]"))
                throw new ArgumentException("senderSeed must 55 lower case characters [a-z]");


            // do the sanity checks

            var desiredSenderPublicKey = _helper.GetPublicKeyFromIdentity(_helper.GetIdentityFromSeed(senderSeed));

            if (_transfer.SenderPublicKey == null)
                _transfer.SenderPublicKey = desiredSenderPublicKey;

            List<string> transferErrors = _transfer.CheckForErrors();
            if (transferErrors.Any())
             throw new ArgumentException("Validation Errors: " + string.Join("| ", transferErrors));

            if (!desiredSenderPublicKey.SequenceEqual(_transfer.SenderPublicKey))
                throw new ArgumentException("Sender Identity must be inherited from Sender Seed");

            // all checks passed
            if(this._transfer.TransactionType == null)
            {
                // try to guess type
                throw new NotImplementedException("Transaction Type must be specified");
            }

            // create header
            var header = new RequestResponseHeader()
            {
                size = _transfer.GetTransactionSize() + Marshal.SizeOf<RequestResponseHeader>(),
                type = (short)QubicPackageTypes.BROADCAST_TRANSACTION
            };

            BaseTransaction? baseTransaction = null; // must be filled later
            byte[] txInput = new byte[0]; // optional

            switch(this._transfer.TransactionType)
            {
                case transfer.TransactionType.Default:
                    {
                        baseTransaction = _transfer.GetBaseTransaction();
                    }
                    break;
                case transfer.TransactionType.SendMany:
                    {
                        baseTransaction = _transfer.GetBaseTransaction();
                        txInput = Marshalling.Serialize(_transfer.GetSendManyStruct());
                    }

                    break;
            }

            if (baseTransaction == null)
                throw new Exception("Error Creating Base Transaction");

            // sign the tx
            var completeTx = Marshalling.Serialize(baseTransaction.Value).Concat(txInput).ToArray();
            var signature = _helper.SignStruct(senderSeed, completeTx);


            return Marshalling.Serialize(header).Concat(completeTx).Concat(signature).ToArray();
        }

    }
}
