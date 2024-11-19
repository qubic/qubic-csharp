using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace li.qubic.crypt
{
    public class QubicCrypt : IQubicCrypt
    {

        public static short SIGNATURE_LENGTH = 64;

        private IQubicCrypt _qubicCrypt { get; set; }

        public QubicCrypt()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _qubicCrypt = new QubicLinuxCrypt();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _qubicCrypt = new QubicWindowsCrypt();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public byte[] KangarooTwelve(byte[] inputData, int outputByteLen, int? fixedInputDataLength = null)
        {
            return _qubicCrypt.KangarooTwelve(inputData, outputByteLen, fixedInputDataLength);
        }

        public byte[] KangarooTwelve(byte[] inputData, int? fixedInputDataLength = null)
        {
            return _qubicCrypt.KangarooTwelve(inputData, fixedInputDataLength);
        }

        public byte[] KangarooTwelve64To32(byte[] inputData)
        {
            return _qubicCrypt.KangarooTwelve64To32(inputData);
        }

        public byte[] GetSharedKey(string seed, byte[] publicKey)
        {
            return _qubicCrypt.GetSharedKey(seed, publicKey);   
        }

        public byte[] GetSharedKey(byte[] privateKey, byte[] publicKey)
        {
            return _qubicCrypt.GetSharedKey(privateKey, publicKey);
        }

        public byte[] GetPrivateKey(string seed)
        {
            return _qubicCrypt.GetPrivateKey(seed);
        }

        public byte[] GetPublicKey(string seed)
        {
            return _qubicCrypt.GetPublicKey(seed);
        }

        public string GetIdentityFromPublicKey(byte[] publicKey)
        {
            return _qubicCrypt.GetIdentityFromPublicKey(publicKey);
        }

        public string GetHumanReadableBytes(byte[] data)
        {
            return _qubicCrypt.GetHumanReadableBytes(data);
        }

        public byte[] Sign(string seed, byte[] message)
        {
            return _qubicCrypt.Sign(seed, message);
        }

        public bool Verify(byte[] publicKey, byte[] message)
        {
            return _qubicCrypt.Verify(publicKey, message);
        }

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
    }
}
