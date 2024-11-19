using li.qubic.crypt;
using li.qubic.lib.Helper;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace li.qubic.lib
{
    public class QubicHelper
    {

        private QubicCrypt _qubicCrypt = new QubicCrypt();

        public QubicHelper()
        {

        }

        /// <summary>
        /// converts 32 byte array binary to human readable lower case string
        /// </summary>
        /// <param name="binary"></param>
        /// <returns></returns>
        public string GetHumanReadableBinary(byte[] binary)
        {
            return _qubicCrypt.GetHumanReadableBytes(binary);
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

            return _qubicCrypt.GetIdentityFromPublicKey(publicKey);
        }

        /// <summary>
        /// converts a seed to an identity
        /// </summary>
        /// <param name="seed"></param>
        /// <returns></returns>
        public string GetIdentityFromSeed(string seed)
        {
            return _qubicCrypt.GetIdentityFromPublicKey(_qubicCrypt.GetPublicKey(seed));
        }


        /// <summary>
        /// converts a seed to a privatekey
        /// </summary>
        /// <param name="seed"></param>
        /// <returns></returns>
        public byte[] GetPrivateKey(string seed)
        {
            return _qubicCrypt.GetPrivateKey(seed);
        }

        /// <summary>
        /// converts the private and publickey into a shared key
        /// </summary>
        /// <param name="privateKey"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public byte[] GetSharedKey(byte[] privateKey, byte[] publicKey)
        {
            return _qubicCrypt.GetSharedKey(privateKey, publicKey);
        }


        /// <summary>
        /// can be used to sign a struct and receive the signature
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="data"></param>
        /// <param name="fixedSize"></param>
        /// <returns></returns>
        public byte[] SignStruct(string seed, byte[] data, int? fixedSize = null)
        {
            return _qubicCrypt.Sign(seed, fixedSize == null ? data : data.Take(fixedSize.Value).ToArray());
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
            return _qubicCrypt.Verify(publicKey, data);
        }


        /// <summary>
        /// Custom implementation of Getting Public Key from Identity
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        public byte[]? GetPublicKeyFromIdentity(string identity)
        {
            return GetBinaryFromString(identity);
        }


        private byte[] GetBinaryFromString(string identity)
        {
            byte[] publicKey = new byte[32];

            if (identity.Length != 60)
            {
                // Ensure that identity length matches the expected length
                throw new ArgumentException("identity must be 60 characters");
            }

            identity = identity.ToLower();

            byte[] publicKeyBuffer = new byte[32];
            for (int i = 0; i < 4; i++)
            {
                ulong value = 0;
                for (int j = 13; j >= 0; j--)
                {
                    char c = identity[i * 14 + j];
                    if (c < 'a' || c > 'z')
                    {
                        throw new Exception("wrong character detected");
                    }
                    value = value * 26 + (ulong)(c - 'a');
                }

                // Copy value to publicKeyBuffer (64 bits)
                byte[] valueBytes = BitConverter.GetBytes(value);
                Buffer.BlockCopy(valueBytes, 0, publicKeyBuffer, i * 8, 8);
            }

            // Copy publicKeyBuffer to publicKey (32 bytes)
            Buffer.BlockCopy(publicKeyBuffer, 0, publicKey, 0, 32);

            return publicKey;
    }

        #region Seed Gen

        /// <summary>
        /// generates a random seed
        /// </summary>
        /// <returns></returns>
        public string GenerateRandomSeed()
        {
            var length = 55;
            var allowableChars = @"abcdefghijklmnopqrstuvwxyz";

            // Generate random data
            var rnd = RandomNumberGenerator.GetBytes(length);

            // Generate the output string
            var allowable = allowableChars.ToCharArray();
            var l = allowable.Length;
            var chars = new char[length];
            for (var i = 0; i < length; i++)
                chars[i] = allowable[rnd[i] % l];

            return new string(chars);
        }

        #endregion

        #region Static Helper Methods

        



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
