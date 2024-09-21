using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace li.qubic.lib.Logging
{
    public class QubicLogger
    {
        private static QubicHelper helper = new QubicHelper();

        // Constants from the original C++ code
        public static int QU_TRANSFER = 0;
        public static int QU_TRANSFER_LOG_SIZE = 72;
        public static int ASSET_ISSUANCE = 1;
        public static int ASSET_ISSUANCE_LOG_SIZE = 55;
        public static int ASSET_OWNERSHIP_CHANGE = 2;
        public static int ASSET_OWNERSHIP_CHANGE_LOG_SIZE = 119;
        public static int ASSET_POSSESSION_CHANGE = 3;
        public static int ASSET_POSSESSION_CHANGE_LOG_SIZE = 119;
        public static int CONTRACT_ERROR_MESSAGE = 4;
        public static int CONTRACT_ERROR_MESSAGE_LOG_SIZE = 4;
        public static int CONTRACT_WARNING_MESSAGE = 5;
        public static int CONTRACT_INFORMATION_MESSAGE = 6;
        public static int CONTRACT_DEBUG_MESSAGE = 7;
        public static int BURNING = 8;
        public static int BURNING_LOG_SIZE = 40;
        public static int CUSTOM_MESSAGE = 255;

        public static void PrintQubicLog(byte[] logBufferInput)
        {
            var logBuffer = logBufferInput; //.Skip(18).ToArray();
            int bufferSize = logBuffer.Length;
            if (bufferSize == 0)
            {
                Console.WriteLine("Empty log");
                return;
            }
            if (bufferSize < 26)
            {
                Console.WriteLine($"Buffer size is too small (not enough to contain the header), expected 26 | received {bufferSize}");
                return;
            }

            int offset = 0;
            while (offset < bufferSize)
            {
                // basic info
                ushort epoch = BitConverter.ToUInt16(logBuffer, offset);
                uint tick = BitConverter.ToUInt32(logBuffer, offset + 2);
                uint tmp = BitConverter.ToUInt32(logBuffer, offset + 6);
                byte messageType = (byte)(tmp >> 24);
                string mt = LogTypeToString(messageType);
                uint messageSize = tmp & 0xFFFFFF;
                ulong id = BitConverter.ToUInt64(logBuffer, offset + 10);
                Debug.WriteLine("LogId: " + id);
                ulong digest = BitConverter.ToUInt64(logBuffer, offset + 18);


                offset += 26;
                string humanLog = "null";

                switch ((QubicLogMessageType)messageType)
                {
                    case QubicLogMessageType.QuTransfer:
                        if (messageSize == QU_TRANSFER_LOG_SIZE || messageSize == (QU_TRANSFER_LOG_SIZE + 8)) // with or without transfer ID
                        {
                            humanLog = ParseLogToStringType0(logBuffer, offset);
                        }
                        else
                        {
                            Console.WriteLine("Malfunction buffer size for QU_TRANSFER log");
                        }
                        break;

                    case QubicLogMessageType.AssetIssuance:
                        if (messageSize == ASSET_ISSUANCE_LOG_SIZE)
                        {
                            humanLog = ParseLogToStringType1(logBuffer, offset);
                        }
                        else
                        {
                            Console.WriteLine("Malfunction buffer size for ASSET_ISSUANCE log");
                        }
                        break;

                    case QubicLogMessageType.AssetOwnershipChange:
                    case QubicLogMessageType.AssetPossessionChange:
                        if (messageSize == ASSET_OWNERSHIP_CHANGE_LOG_SIZE || messageSize == ASSET_POSSESSION_CHANGE_LOG_SIZE)
                        {
                            humanLog = ParseLogToStringType2Type3(logBuffer, offset);
                        }
                        else
                        {
                            Console.WriteLine("Malfunction buffer size for ASSET_OWNERSHIP_CHANGE or ASSET_POSSESSION_CHANGE log");
                        }
                        break;

                    case QubicLogMessageType.Burning:
                        if (messageSize == BURNING_LOG_SIZE)
                        {
                            humanLog = ParseLogToStringBurning(logBuffer, offset);
                        }
                        else
                        {
                            Console.WriteLine("Malfunction buffer size for BURNING log");
                        }
                        break;

                    case QubicLogMessageType.ContractInfo:
                        if (BitConverter.ToUInt32(logBuffer, offset) == 4)
                        {
                            humanLog = ParseLogToStringQUtil(logBuffer, offset + 8); // padding issue, +8 instead of +4
                        }
                        break;

                    case QubicLogMessageType.ContractError:
                    case QubicLogMessageType.ContractWarning:
                    case QubicLogMessageType.ContractDebug:
                    case QubicLogMessageType.CustumMessage:
                        break;

                    default:
                        Console.WriteLine($"Unknown message type: {messageType}");
                        break;
                }

                Console.WriteLine($"{tick}.{epoch:D3} {mt}: {humanLog}");

                if (humanLog == "null")
                {
                    StringBuilder buff = new StringBuilder();
                    for (int i = 0; i < messageSize; i++)
                    {
                        buff.AppendFormat("{0:X2}", logBuffer[offset + i]);
                    }
                    Console.WriteLine($"Can't parse, original message: {buff}");
                }

                offset += (int)messageSize;
            }
        }

        public static string Log(QubicEventLogEntry entry)
        {
            if(entry.QuTransfer != null) {
                return $"QU Transfer from {helper.GetIdentity(entry.QuTransfer.Value.sourcePublicKey)} to {helper.GetIdentity(entry.QuTransfer.Value.destinationPublicKey)} {entry.QuTransfer.Value.amount} QU.";
            }

            return "empty";
        }

        

        public static string LogTypeToString(byte type)
        {
            switch (type)
            {
                case 0: return "QU transfer";
                case 1: return "Asset issuance";
                case 2: return "Asset ownership change";
                case 3: return "Asset possession change";
                case 4: return "Contract error";
                case 5: return "Contract warning";
                case 6: return "Contract info";
                case 7: return "Contract debug";
                case 8: return "Burning";
                case 255: return "Custom msg";
                default: return "Unknown msg";
            }
        }

        public static string ParseLogToStringType0(byte[] buffer, int offset)
        {
            string sourceIdentity = GetIdentityFromPublicKey(buffer, offset, 32, false);
            string destIdentity = GetIdentityFromPublicKey(buffer, offset + 32, 32, false);
            long amount = BitConverter.ToInt64(buffer, offset + 64);

            return $"from {sourceIdentity} to {destIdentity} {amount} QU.";
        }

        public static string ParseLogToStringType1(byte[] buffer, int offset)
        {
            string sourceIdentity = GetIdentityFromPublicKey(buffer, offset, 32, false);
            long numberOfShares = BitConverter.ToInt64(buffer, offset + 32);
            string name = Encoding.ASCII.GetString(buffer, offset + 32 + 8, 7).TrimEnd('\0');
            char numberOfDecimalPlaces = (char)buffer[offset + 32 + 8 + 7];
            string unit = Encoding.ASCII.GetString(buffer, offset + 32 + 8 + 7 + 1, 7).TrimEnd('\0');

            return $"{sourceIdentity} issued {numberOfShares} {name}. Number of decimal: {numberOfDecimalPlaces}. " +
                   $"Unit of measurement: {string.Join("-", unit.ToCharArray())}.";
        }

        public static string ParseLogToStringType2Type3(byte[] buffer, int offset)
        {
            string sourceIdentity = GetIdentityFromPublicKey(buffer, offset, 32, false);
            string dstIdentity = GetIdentityFromPublicKey(buffer, offset + 32, 32, false);
            string issuerIdentity = GetIdentityFromPublicKey(buffer, offset + 64, 32, false);
            long numberOfShares = BitConverter.ToInt64(buffer, offset + 96);
            string name = Encoding.ASCII.GetString(buffer, offset + 96 + 8, 7).TrimEnd('\0');
            char numberOfDecimalPlaces = (char)buffer[offset + 96 + 8 + 7];
            string unit = Encoding.ASCII.GetString(buffer, offset + 96 + 8 + 7 + 1, 7).TrimEnd('\0');

            return $"from {sourceIdentity} to {dstIdentity} {numberOfShares} {name} (Issuer: {issuerIdentity}). " +
                   $"Number of decimal: {numberOfDecimalPlaces}. Unit of measurement: {string.Join("-", unit.ToCharArray())}.";
        }

        public static string ParseLogToStringBurning(byte[] buffer, int offset)
        {
            string sourceIdentity = GetIdentityFromPublicKey(buffer, offset, 32, false);
            long amount = BitConverter.ToInt64(buffer, offset + 32);

            return $"{amount} QU from {sourceIdentity}.";
        }

        public static string ParseLogToStringQUtil(byte[] buffer, int offset)
        {
            string res = $"from {GetIdentityFromPublicKey(buffer, offset, 32, false)} to ";
            res += $"{GetIdentityFromPublicKey(buffer, offset + 32, 32, false)} Amount ";
            long amount = BitConverter.ToInt64(buffer, offset + 64);
            res += $"{amount}: ";
            uint logtype = BitConverter.ToUInt32(buffer, offset + 72);

            switch (logtype)
            {
                case 0: res += "Success"; break;
                case 1: res += "Invalid amount number"; break;
                case 2: res += "insufficient fund"; break;
                case 3: res += "Triggered SendToManyV1"; break;
                case 4: res += "send fund via SendToManyV1"; break;
            }

            return res;
        }

        private static string GetIdentityFromPublicKey(byte[] buffer, int offset, int length, bool isLowerCase)
        {
            var key = helper.GetIdentity(buffer.Skip(offset).Take(length).ToArray());

            return isLowerCase ? key.ToLower() : key ;
        }
    }

}
