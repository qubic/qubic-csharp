using li.qubic.lib.Helper;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace li.qubic.lib
{
    public class QubicHelper
    {

        public QubicHelper()
        {

        }


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

        [DllImport("libQubicHelper", EntryPoint = "verifyQubicStructExported")]
        private static extern bool verifyQubicStructDll(byte[] data, int packageSize, byte[] publicKey);

        [DllImport("libQubicHelper", EntryPoint = "getBinaryFromStringExported")]
        static extern bool getBinaryFromStringFromDll(string input, byte[] output);

        /// <summary>
        /// converts 32 byte array binary to human readable lower case string
        /// </summary>
        /// <param name="binary"></param>
        /// <returns></returns>
        public string GetHumanReadableBinary(byte[] binary)
        {
            byte[] identity = new byte[60];
            getIdentity(binary, identity, true);
            return Encoding.ASCII.GetString(identity);
        }

        /// <summary>
        /// Convers a publicKey to a human readable string (identity)
        /// </summary>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public string GetIdentity(byte[] publicKey)
        {
            if (publicKey == null)
                throw new ArgumentException("Must not be null", "publicKey");
            byte[] identity = new byte[60];
            getIdentity(publicKey, identity, false);
            return Encoding.ASCII.GetString(identity);
        }

        /// <summary>
        /// converts a seed to an identity
        /// </summary>
        /// <param name="seed"></param>
        /// <returns></returns>
        public string GetIdentityFromSeed(string seed)
        {
            byte[] identity = new byte[60];
            getIdentityFromSeedDll(seed, identity);
            return Encoding.ASCII.GetString(identity);
        }


        /// <summary>
        /// converts a seed to a privatekey
        /// </summary>
        /// <param name="seed"></param>
        /// <returns></returns>
        public byte[] GetPrivateKey(string seed)
        {
            byte[] privateKey = new byte[32];
            generatePrivateKeyFromDll(seed, privateKey);
            return privateKey;
        }

        /// <summary>
        /// converts the private and publickey into a shared key
        /// </summary>
        /// <param name="privateKey"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public byte[] GetSharedKey(byte[] privateKey, byte[] publicKey)
        {
            byte[] sharedKey = new byte[32];
            getSharedKeyFromDll(privateKey, publicKey, sharedKey);
            return sharedKey;
        }


        /// <summary>
        /// can be used to sign a struct and receie the signature
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="data"></param>
        /// <param name="fixedSize"></param>
        /// <returns></returns>
        public byte[] SignStruct(string seed, byte[] data, int? fixedSize = null)
        {
            byte[] signature = new byte[QubicLibConst.SIGNATURE_SIZE];
            var size = fixedSize ?? data.Length;
            signStructDll(seed, data, size, signature);
            return signature;
        }

        /// <summary>
        /// can be used to verify a qubic struct (signed data)
        /// data should include signature
        /// </summary>
        /// <param name="data">the struct/object to verify</param>
        /// <param name="size">size of what should be verified</param>
        /// <param name="publicKey">the publickey of the signing private key</param>
        /// <returns></returns>
        public bool VerifyQubicStruct(byte[] data, int size, byte[] publicKey)
        {
            return verifyQubicStructDll(data, size, publicKey);
        }


        /// <summary>
        /// converts a qubic identity string representation back to binary
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public byte[]? GetBinaryFromString(string input)
        {
            byte[] output = new byte[32];
            if (getBinaryFromStringFromDll(input, output))
            {
                return output;
            }
            return null;
        }

        /// <summary>
        /// Custom implementation of Getting Public Key from Identity
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        public byte[]? GetPublicKeyFromIdentity(string identity)
        {
            byte[] publicKey = new byte[32];
            if (getPublicKeyFromIdentity(identity, publicKey))
            {
                return publicKey;
            }
            return null;
        }


        #region Seed Gen




        private Random _random = new Random();
        private char GetLetter()
        {
            // This method returns a random lowercase letter.
            // ... Between 'a' and 'z' inclusive.
            int num = _random.Next(0, 26); // Zero to 25
            char let = (char)('a' + num);
            return let;
        }

        /// <summary>
        /// generates a random seed
        /// </summary>
        /// <returns></returns>
        public string GenerateRandomSeed()
        {
            var sb = new StringBuilder();
            for(var i = 0; i < 60; i++)
            {
                sb.Append(GetLetter());
            }
            return sb.ToString();
        }

        #endregion

        #region Static Helper Methods

        



        /// <summary>
        /// converts a Qubic ID String back to a byte array
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static byte[]? QubicStringToBytes(string input)
        {
            return new QubicHelper().GetBinaryFromString(input);
        }
        /// <summary>
        /// converts a qubic base64 public key to a string identity
        /// </summary>
        /// <param name="base64Input"></param>
        /// <returns></returns>
        public static string QubbicBase64BytesToString(string base64Input)
        {
            return new QubicHelper().GetHumanReadableBinary(Convert.FromBase64String(base64Input));
        }
        public static string QubbicBytesToString(byte[] input)
        {
            return new QubicHelper().GetHumanReadableBinary(input);
        }

        /// <summary>
        /// Generates a Long Randum number
        /// </summary>
        /// <param name="rand"></param>
        /// <returns></returns>
        public static ulong LongRandom(Random rand)
        {
            byte[] buf = new byte[8];
            rand.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);

            return (ulong)Math.Abs(longRand);
        }

        /// <summary>
        /// Converts the Computor Index to the Short Code
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static string ComputorShortCode(int index)
        {
            char[] alphabet = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
            return alphabet[index / 26].ToString() + alphabet[index % 26].ToString();
        }


        /// <summary>
        /// Converts Computor Short Code to Computor Index
        /// </summary>
        /// <param name="shortCode"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static ushort ReverseComputorShortCode(string shortCode)
        {
            char[] alphabet = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
            if (shortCode.Length != 2)
            {
                throw new ArgumentException("Invalid short code format. The short code must be exactly two characters long.");
            }

            char firstChar = shortCode[0];
            char secondChar = shortCode[1];

            int firstIndex = Array.IndexOf(alphabet, char.ToUpper(firstChar));
            int secondIndex = Array.IndexOf(alphabet, char.ToUpper(secondChar));

            if (firstIndex < 0 || secondIndex < 0)
            {
                throw new ArgumentException("Invalid characters in the short code.");
            }

            return (ushort)(firstIndex * 26 + secondIndex);
        }


        #endregion

    }
}
