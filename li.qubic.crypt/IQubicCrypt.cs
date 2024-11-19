using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace li.qubic.crypt
{
    public interface IQubicCrypt
    {
        public byte[] KangarooTwelve(byte[] inputData, int outputByteLen, int? fixedInputDataLength = null);

        public byte[] KangarooTwelve(byte[] inputData, int? fixedInputDataLength = null);

        public byte[] KangarooTwelve64To32(byte[] inputData);

        public byte[] GetSharedKey(string seed, byte[] publicKey);

        public byte[] GetSharedKey(byte[] privateKey, byte[] publicKey);

        public byte[] GetPrivateKey(string seed);
        public byte[] GetPublicKey(string seed);

        public string GetIdentityFromPublicKey(byte[] publicKey);

        public string GetHumanReadableBytes(byte[] data);
        public string GenerateRandomSeed();

        public byte[] Sign(string seed, byte[] message);

        /// <summary>
        /// verifies a struct
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="message">includes the signature</param>
        /// <param name="signature"></param>
        /// <returns></returns>
        public bool Verify(byte[] publicKey, byte[] message);

    }
}
