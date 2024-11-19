using System.Runtime.InteropServices;
using System.Text;


namespace li.qubic.crypt
{
    /// <summary>
    /// linux only 
    /// </summary>
    internal class QubicLinuxCrypt : IQubicCrypt
    {
        [DllImport("libfourq-qubic", EntryPoint = "sign")]
        private static extern bool sign(byte[] subSeed, byte[] publicKey, byte[] messageDigest, byte[] signature);

        [DllImport("libfourq-qubic", EntryPoint = "verify")]
        private static extern bool verify(byte[] publicKey, byte[] messageDigest, byte[] signature);

        [DllImport("libfourq-qubic", EntryPoint = "getSubseedFromSeed")]
        private static extern bool getSubseedFromSeed(string seed, byte[] subseed);


        [DllImport("libfourq-qubic", EntryPoint = "getPrivateKeyFromSubSeed")]
        private static extern void getPrivateKeyFromSubSeed(byte[] seed, byte[] privateKey);

        [DllImport("libfourq-qubic", EntryPoint = "getPublicKeyFromPrivateKey")]
        private static extern void getPublicKeyFromPrivateKey(byte[] privateKey, byte[] publicKey);

        [DllImport("libfourq-qubic", EntryPoint = "getIdentityFromPublicKey")]
        private static extern void getIdentityFromPublicKey(byte[] publicKey, byte[] identity);

        [DllImport("libfourq-qubic", EntryPoint = "getSharedKey")]
        private static extern bool getSharedKey(byte[] privateKey, byte[] publicKey, byte[] sharedKey);

        [DllImport("libfourq-qubic", EntryPoint = "KangarooTwelveExported")]
        private static extern void _KangarooTwelve(byte[] input, int inputByteLen, byte[] output, int outputByteLen);

        [DllImport("libfourq-qubic", EntryPoint = "KangarooTwelve64To32Exported")]
        private static extern void _KangarooTwelve64To32(byte[] input, byte[] output);


        public byte[] KangarooTwelve(byte[] inputData, int outputByteLen, int? fixedInputDataLength = null)
        {
            var ouput = new byte[outputByteLen];
            var inputLength = fixedInputDataLength ?? inputData.Length;
            _KangarooTwelve(inputData, inputLength, ouput, outputByteLen);
            return ouput;
        }


        public byte[] KangarooTwelve(byte[] inputData, int? fixedInputDataLength = null)
        {
            var ouput = new byte[32];
            var inputLength = fixedInputDataLength ?? inputData.Length;
            _KangarooTwelve(inputData, inputLength, ouput, 32);
            return ouput;
        }

        public byte[] KangarooTwelve64To32(byte[] inputData)
        {
            if (inputData.Length != 64)
                throw new Exception("Length of InputData must be 64");
            var output = new byte[32];
            _KangarooTwelve64To32(inputData, output);
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
            getSharedKey(privateKey, publicKey, sharedKey);
            return sharedKey;
        }

        public byte[] GetPrivateKey(string seed)
        {
            var subseed = new byte[32];
            getSubseedFromSeed(seed, subseed);
            var privateKey = new byte[32];
            getPrivateKeyFromSubSeed(subseed, privateKey);
            return privateKey;
        }

        public byte[] GetPublicKey(string seed)
        {
            var publicKey = new byte[32];
            getPublicKeyFromPrivateKey(GetPrivateKey(seed), publicKey);
            return publicKey;
        }

        public string GetIdentityFromPublicKey(byte[] publicKey)
        {
            byte[] identity = new byte[60];
            getIdentityFromPublicKey(publicKey, identity);
            return Encoding.ASCII.GetString(identity).ToUpper();
        }

        public string GetHumanReadableBytes(byte[] data)
        {
            byte[] identity = new byte[60];
            getIdentityFromPublicKey(data, identity);
            return Encoding.ASCII.GetString(identity);
        }

        public byte[] Sign(string seed, byte[] message)
        {
            var subseed = new byte[32];
            getSubseedFromSeed(seed, subseed);
            var privateKey = new byte[32];
            getPrivateKeyFromSubSeed(subseed, privateKey);
            var publicKey = new byte[32];
            getPublicKeyFromPrivateKey(privateKey, publicKey);
            var signature = new byte[64];
            var digest = new byte[32];
            _KangarooTwelve(message, message.Length, digest, digest.Length);
            sign(subseed, publicKey, digest, signature);
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
            _KangarooTwelve(message, message.Length - 64, digest, digest.Length);
            return verify(publicKey, digest, message.Skip(message.Length - 64).ToArray());
        }

        public string GenerateRandomSeed()
        {
            throw new NotImplementedException();
        }
    }
}
