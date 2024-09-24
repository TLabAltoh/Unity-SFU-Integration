using System.Text;
using System.Security.Cryptography;

namespace TLab.NetworkedVR.Network.Security
{
    public static class Authentication
    {
        public static byte[] GetHash(string inputString)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public static string GetHashString(string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        public static bool ConfirmPassword(string password, string hash)
        {
            return GetHashString(password) == hash;
        }
    }
}
