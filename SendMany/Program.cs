using li.qubic.lib;
using li.qubic.lib.Helper;
using li.qubic.lib.Network;
using SendMany.Model;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace SendMany
{
    
    // TODO: clean poc code

    /// <summary>
    /// 
    /// </summary>
    internal class Program
    {
        private static QubicHelper _helper = new QubicHelper();
        private static long fixedFee = 10;
        private static int QUTIL_CONTRACT_ID = 4;
        private static string _signSeed = "";
        private static string _targetIp = "0.0.0.0";

        /// <summary>
        /// poc implementation to use sendmany with c#
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static int Main(string[] args)
        {

            if(args.Length < 4) {
                Console.WriteLine($"Need at least three arguments");
                Console.WriteLine($"Usage: sendMany <nodeIP> <senderSeed> <targetId1> <targetId1Amount> [targetIdX] [targetIdXAmount]");
                return 1;
            }

            _targetIp = args[0];

            var senderSeed = args[1];
            if(senderSeed == null || senderSeed.Length != 55 || !Regex.IsMatch(senderSeed, "[a-z]") ) {
                Console.WriteLine($"Invalid sender seed");
                return 2;
            }
            var senderId = _helper.GetIdentityFromSeed(senderSeed);
            var senderPublicKey = _helper.GetPublicKeyFromIdentity(senderId);


            if(senderPublicKey == null)
            {
                Console.WriteLine("Error in Converting sender seed to public key");
                return 3;
            }

            _signSeed = senderSeed;


            bool canProceed = true;
            List<DestinationAdress> destinations = new List<DestinationAdress>();
            for (int i = 2;i < args.Length;i++)
            {
                var id = args[i];
                long amount = 0;
                canProceed = canProceed && CheckId(id) && long.TryParse(args[++i], out amount);
                
                destinations.Add(new DestinationAdress(id, amount));
            }

            if (!canProceed)
            {
                Console.WriteLine("Error in Destination Addresses");
                return 4;
            }

            // get fee
            fixedFee = GetSendManyFeeAsync().GetAwaiter().GetResult();

            var destinationAddresses = new byte[25][];
            var destinationAmounts = new long[25];
            var addressIndex = 0;
            foreach (var address in destinations)
            {
                destinationAmounts[addressIndex] = address.Amount;
                destinationAddresses[addressIndex++] = _helper.GetPublicKeyFromIdentity(address.Id).ToArray();
            }

            // fill rest of addresses with 0
            for (; addressIndex < 25; addressIndex++)
            {
                destinationAmounts[addressIndex] = 0;
                destinationAddresses[addressIndex] = new byte[32];
            }
           

            // build the sc call
            var request = new SendManyRequest()
            {
                header = new RequestResponseHeader()
                {
                    size = 0, // should be set later
                    type = (short)QubicPackageTypes.BROADCAST_TRANSACTION
                },
                tx = new BaseTransaction()
                {
                    amount = destinations.Sum(s => s.Amount) + fixedFee,
                    inputSize = (ushort)Marshal.SizeOf<SendToManyV1_input>(),
                    inputType = 1,
                    tick = 0,
                    destinationPublicKey = GetDestinationPublicKey(),
                    sourcePublicKey = senderPublicKey
                },
                input = new SendToManyV1_input()
                {
                    amounts = destinationAmounts,
                    addresses = destinationAddresses.ToQubicArray(25,32),
                }
            };

            // here you may want to check if the source address has enough balance!

            // calculated package size
            request.header.size = (int)(QubicStructSizes.RequestResponseHeader + QubicStructSizes.Transaction + Marshal.SizeOf<SendToManyV1_input>()) + QubicLibConst.SIGNATURE_SIZE;

            // open requestor
            var requestor = new QubicRequestor(_targetIp, 1);

            // connect to node
            if (!requestor.Connect())
                throw new Exception($"Conection to {_targetIp} failed");

            // get current tick
            // thi is used to be able to send tx with offset
            var tickinfo = requestor.GetTickInfo().GetAwaiter().GetResult();
            request.tx.tick = tickinfo.tick + 5; // add 5 to current tick

            // get signature
            var signature = SignAndGetSignature(request);

            // concat package to send to the network
            var toSendPackage = Marshalling.Serialize(request).Concat(signature).ToArray();

            // verify signature (only for poc use)
            var verififed = _helper.VerifyQubicStruct(toSendPackage.Skip(8).ToArray(), toSendPackage.Length - 8, senderPublicKey);

            // sent tx to network
            requestor.Send(toSendPackage);

            Console.WriteLine($"TX SENT in tick {request.tx.tick}");

            // here you may want to calculate digest and give the user the tx id

            // close connection properly
            requestor.Disconnect();

            return 0; // all good
        }

        /// <summary>
        /// sign the tx package
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        private static byte[] SignAndGetSignature(SendManyRequest package)
        {
            var binaryPackage = Marshalling.Serialize(package.tx).Concat(Marshalling.Serialize(package.input)).ToArray();
            var signatur = _helper.SignStruct(_signSeed, binaryPackage, binaryPackage.Length);
            return signatur;
        }

        /// <summary>
        /// receive the fee to be paid to use the SC
        /// </summary>
        /// <returns></returns>
        private async static Task<long> GetSendManyFeeAsync()
        {
            var header = new RequestResponseHeader(true)
            {
                type = (short)QubicPackageTypes.REQUEST_CONTRACT_FUNCTION,
                size = (int)QubicStructSizes.RequestResponseHeader + Marshal.SizeOf<RequestContractFunction>()
            };
            var rcf = new RequestContractFunction()
            {
                contractIndex = (uint)QUTIL_CONTRACT_ID,
                inputSize = 0,
                inputType = 1
            };

            var reqPackage = Marshalling.Serialize(header).Concat(Marshalling.Serialize(rcf)).ToArray();

            var req = new QubicRequestor(_targetIp, 1);
            var result = await req.GetDataPackageFromPeerAsyc<GetSendToManyV1Fee_output>(reqPackage, (short)QubicPackageTypes.RESPOND_CONTRACT_FUNCTION);
            
            return result.fee;
        }

        /// <summary>
        /// Checks if the dest id is valid
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private static bool CheckId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }
            if(id.Length != 60)
            {
                Console.WriteLine($"{id} must be 60 characters");
                return false;
            }
            if (!Regex.IsMatch(id, "[A-Z]"))
            {
                Console.WriteLine($"{id} must consist of [A-Z]");
                return false;
            }
            var bytes = _helper.GetPublicKeyFromIdentity(id);
            var compareId = _helper.GetIdentity(bytes);
            if (compareId != id)
            {
                Console.WriteLine($"{id} must be a valid qubic address");
                return false;
            }
            return true;
        }


        /// <summary>
        /// genrates the sc id
        /// </summary>
        /// <returns></returns>
        private static byte[] GetDestinationPublicKey()
        {
            byte[] destPublicKey = new byte[32];

            Array.Copy(BitConverter.GetBytes((ulong)QUTIL_CONTRACT_ID), 0, destPublicKey, 0, 8);
            Array.Copy(BitConverter.GetBytes((ulong)0), 0, destPublicKey, 8, 8);
            Array.Copy(BitConverter.GetBytes((ulong)0), 0, destPublicKey, 16, 8);
            Array.Copy(BitConverter.GetBytes((ulong)0), 0, destPublicKey, 24, 8);
            return destPublicKey;
        }

    }


}
