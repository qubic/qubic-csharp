using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;


namespace li.qubic.crypt
{
    /// <summary>
    /// windows only 
    /// legacy library!!!!!
    /// </summary>
    internal class QubicWindowsCrypt : IQubicCrypt
    {

        [DllImport("libQubicHelper", EntryPoint = "kangarooTwelveExported")]
        private static extern bool kangarooTwelveDll(byte[] input, int inputByteLenth, byte[] output, long outputByteLength);

        [DllImport("libQubicHelper", EntryPoint = "kangarooTwelve64To32Exported")]
        private static extern bool kangarooTwelve64To32Dll(byte[] input, byte[] output);

        [DllImport(@"libQubicHelper", EntryPoint = "signStructExported")]
        private static extern bool signStructDll(string seed, byte[] data, int structSize, byte[] signature);

        [DllImport("libQubicHelper", EntryPoint = "getIdentityFromSeedExported")]
        private static extern bool getIdentityFromSeedDll(string seed, byte[] identity);

        [DllImport("libQubicHelper", EntryPoint = "getPublicKeyFromIdentityExported")]
        private static extern bool getPublicKeyFromIdentity(string computor, byte[] publicKey);

        [DllImport("libQubicHelper", EntryPoint = "getSharedKeyExported")]
        private static extern bool getSharedKeyFromDll(byte[] privateKey, byte[] publicKey, byte[] sharedKey);

        [DllImport("libQubicHelper", EntryPoint = "generatePrivateKeyExported")]
        private static extern bool generatePrivateKeyFromDll(string seed, byte[] privateKey);

        [DllImport("libQubicHelper", EntryPoint = "getIdentityExported")]
        private static extern void getIdentity(byte[] publicKey, byte[] identity, bool isLowerCase);

        [DllImport("libQubicHelper", EntryPoint = "getBinaryFromStringExported")]
        static extern bool getBinaryFromStringFromDll(string input, byte[] output);

        [DllImport("libQubicHelper", EntryPoint = "VerifyExported")]
        static extern bool VerifyFromDll(byte[] publicKey, byte[] messageDigest, byte[] signature);

        #region Interface Implementations

        public byte[] KangarooTwelve(byte[] inputData, int outputByteLen, int? fixedInputDataLength = null)
        {
            var ouput = new byte[outputByteLen];
            var inputLength = fixedInputDataLength ?? inputData.Length;
            kangarooTwelveDll(inputData, inputLength, ouput, outputByteLen);
            return ouput;
        }


        public byte[] KangarooTwelve(byte[] inputData, int? fixedInputDataLength = null)
        {
            var ouput = new byte[32];
            var inputLength = fixedInputDataLength ?? inputData.Length;
            kangarooTwelveDll(inputData, inputLength, ouput, 32);
            return ouput;
        }

        public byte[] KangarooTwelve64To32(byte[] inputData)
        {
            if (inputData.Length != 64)
                throw new Exception("Length of InputData must be 64");
            var output = new byte[32];
            kangarooTwelve64To32Dll(inputData, output);
            return output;
        }

        public byte[] GetSharedKey(string seed, byte[] publicKey)
        {
            var privateKey = GetPrivateKey(seed);
            return GetSharedKey(privateKey, publicKey);
        }

        public byte[] GetSharedKey(byte[] privateKey, byte[] publicKey)
        {
            byte[] sharedKey = new byte[32];
            getSharedKeyFromDll(privateKey, publicKey, sharedKey);
            return sharedKey;
        }

        public byte[] GetPrivateKey(string seed)
        {
            var privateKey = new byte[32];
            generatePrivateKeyFromDll(seed, privateKey);
            return privateKey;
        }

        public byte[] GetPublicKey(string seed)
        {
            byte[] identity = new byte[60];
            getIdentityFromSeedDll(seed, identity);
            var id = Encoding.ASCII.GetString(identity);
            var publicKey = new byte[32];
            getPublicKeyFromIdentity(id, publicKey);
            return publicKey;
        }

        public string GetIdentityFromPublicKey(byte[] publicKey)
        {
            byte[] identity = new byte[60];
            getIdentity(publicKey, identity, false);
            return Encoding.ASCII.GetString(identity).ToUpper();
        }

        /// <summary>
        /// returns any 32 byte as human readable string (lower case)
        /// used e.g. for tx id's
        /// </summary>
        /// <param name="data">any 32 byte input (e.g. tx digest)</param>
        /// <returns></returns>
        public string GetHumanReadableBytes(byte[] data)
        {
            byte[] identity = new byte[60];
            getIdentity(data, identity, true);
            return Encoding.ASCII.GetString(identity);
        }

        public byte[] Sign(string seed, byte[] message)
        {
            var signature = new byte[64];
            signStructDll(seed, message, message.Length, signature);
            return signature;
        }

        /// <summary>
        /// verifies a struct
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="message">includes the signature</param>
        /// <param name="signature"></param>
        /// <returns></returns>
        public bool Verify(byte[] publicKey, byte[] message)
        {
            var digest = new byte[32];
            kangarooTwelveDll(message, message.Length - 64, digest, digest.Length);
            return VerifyFromDll(publicKey, digest, message.Skip(message.Length - 64).ToArray());
        }



        public string GenerateRandomSeed()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
