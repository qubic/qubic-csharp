using li.qubic.lib.Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace li.qubic.lib.Logging
{
    public class QubicEventLogEntry
    {
        public uint Epoch { get; set; }
        public uint Tick { get; set; }
        public QubicLogMessageType MessageType { get; set; }
        public uint MessageSize { get; set; }

        public ulong Id { get; set; }
        public ulong Digest { get; set; }


        public QuTransfer? QuTransfer { get; set; }
        public AssetIssuance? AssetIssuance { get; set; }
        public AssetOwnershipChange? AssetOwnershipChange { get; set; }
        public AssetPossessionChange? AssetPossessionChange { get; set; }
        public DummyContractErrorMessage? DummyContractErrorMessage { get; set; }
        public DummyContractWarningMessage? DummyContractWarningMessage { get; set; }
        public DummyContractInfoMessage? DummyContractInfoMessage { get; set; }
        public DummyContractDebugMessage? DummyContractDebugMessage { get; set; }
        public DummyCustomMessage? DummyCustomMessag { get; set; }
        public Burning? Burning { get; set; }
        public DustBurning? DustBurning { get; set; }
        public SpectrumStats? SpectrumStats { get; set; }


        /// <summary>
        /// contructs a qubic event log entry from the buffer returned by a node
        /// </summary>
        /// <param name="logBuffer"></param>
        public static List<QubicEventLogEntry> FromBuffer(byte[] logBuffer)
        {
            int bufferSize = logBuffer.Length;
            if (bufferSize == 0)
            {
                throw new ArgumentException("Empty log");
            }
            if (bufferSize < 26)
            {
                throw new ArgumentException($"Buffer size is too small (not enough to contain the header), expected 26 | received {bufferSize}");
            }


            var output = new List<QubicEventLogEntry>();
            int offset = 0;
            while (offset < bufferSize)
            {
                var entry = new QubicEventLogEntry();
                // basic info
                entry.Epoch = BitConverter.ToUInt16(logBuffer, offset);
                entry.Tick = BitConverter.ToUInt32(logBuffer, offset + 2);
                uint tmp = BitConverter.ToUInt32(logBuffer, offset + 6);
                entry.MessageType = (QubicLogMessageType)(byte)(tmp >> 24);
                entry.MessageSize = tmp & 0xFFFFFF;
                entry.Id = BitConverter.ToUInt64(logBuffer, offset + 10);
                entry.Digest = BitConverter.ToUInt64(logBuffer, offset + 18);


                offset += 26;

                if (bufferSize >= offset + entry.MessageSize)
                {

                    //Console.WriteLine("MessageType: " + entry.MessageType);

                    switch (entry.MessageType)
                    {
                        case QubicLogMessageType.QuTransfer:
                            if (entry.MessageSize == QubicLogger.QU_TRANSFER_LOG_SIZE || entry.MessageSize == (QubicLogger.QU_TRANSFER_LOG_SIZE + 8)) // with or without transfer ID
                            {
                                entry.QuTransfer = Marshalling.Deserialize<QuTransfer>(logBuffer, offset);
                            }
                            else
                            {
                                throw new Exception("Malfunction buffer size for QU_TRANSFER log");
                            }
                            break;

                        case QubicLogMessageType.AssetIssuance:
                            if (entry.MessageSize == QubicLogger.ASSET_ISSUANCE_LOG_SIZE)
                            {
                                entry.AssetIssuance = Marshalling.Deserialize<AssetIssuance>(logBuffer, offset);
                            }
                            else
                            {
                                Console.WriteLine("Malfunction buffer size for ASSET_ISSUANCE log");
                            }
                            break;

                        case QubicLogMessageType.AssetOwnershipChange:
                        case QubicLogMessageType.AssetPossessionChange:
                            if (entry.MessageSize == QubicLogger.ASSET_OWNERSHIP_CHANGE_LOG_SIZE || entry.MessageSize == QubicLogger.ASSET_POSSESSION_CHANGE_LOG_SIZE)
                            {
                                entry.AssetOwnershipChange = Marshalling.Deserialize<AssetOwnershipChange>(logBuffer, offset);
                                entry.AssetPossessionChange = Marshalling.Deserialize<AssetPossessionChange>(logBuffer, offset);
                            }
                            else
                            {
                                Console.WriteLine("Malfunction buffer size for ASSET_OWNERSHIP_CHANGE or ASSET_POSSESSION_CHANGE log");
                            }
                            break;

                        case QubicLogMessageType.Burning:
                            if (entry.MessageSize == QubicLogger.BURNING_LOG_SIZE)
                            {
                                entry.Burning = Marshalling.Deserialize<Burning>(logBuffer, offset);
                            }
                            else
                            {
                                Console.WriteLine("Malfunction buffer size for BURNING log");
                            }
                            break;

                        case QubicLogMessageType.ContractInfo:
                            if (BitConverter.ToUInt32(logBuffer, offset) == 4)
                            {
                                entry.DummyContractInfoMessage = Marshalling.Deserialize<DummyContractInfoMessage>(logBuffer, offset);
                            }
                            break;

                        case QubicLogMessageType.ContractError:
                        case QubicLogMessageType.ContractWarning:
                        case QubicLogMessageType.ContractDebug:
                        case QubicLogMessageType.CustumMessage:
                            break;

                        default:
                            throw new Exception($"Unknown message type: {entry.MessageType.ToString()}");
                    }

                    offset += (int)entry.MessageSize;

                    output.Add(entry);
                }
            }

            return output;
        }
    }
}
