using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace li.qubic.lib.Helper
{
    public static class IdentityHelper
    {
        /// <summary>
        /// Checks if the dest id is valid
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool CheckIdentity(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }
            if (id.Length != 60)
            {
                Console.WriteLine($"{id} must be 60 characters");
                return false;
            }
            if (!Regex.IsMatch(id, "[A-Z]"))
            {
                Console.WriteLine($"{id} must consist of [A-Z]");
                return false;
            }
            var helper = new QubicHelper();
            var bytes = helper.GetPublicKeyFromIdentity(id);
            var compareId = helper.GetIdentity(bytes);
            if (compareId != id)
            {
                Console.WriteLine($"{id} must be a valid qubic address");
                return false;
            }
            return true;
        }

        /// <summary>
        /// genrates a smarc contract address
        /// provide the contract id
        /// </summary>
        /// <param name="contractId">the contract id (e.g. QX = 1, QUTIL = 4)</param>
        /// <returns></returns>
        public static byte[] GenerateContractAddress(int contractId)
        {
            byte[] destPublicKey = new byte[32];

            Array.Copy(BitConverter.GetBytes((ulong)contractId), 0, destPublicKey, 0, 8);
            Array.Copy(BitConverter.GetBytes((ulong)0), 0, destPublicKey, 8, 8);
            Array.Copy(BitConverter.GetBytes((ulong)0), 0, destPublicKey, 16, 8);
            Array.Copy(BitConverter.GetBytes((ulong)0), 0, destPublicKey, 24, 8);
            return destPublicKey;
        }
    }
}
