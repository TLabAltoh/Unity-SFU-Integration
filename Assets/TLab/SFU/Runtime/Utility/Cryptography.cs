using System.Security.Cryptography;
using System.Text;
using static System.BitConverter;

namespace TLab.SFU
{
    public static class Cryptography
    {
        public static int MD5From(string @string)
        {
            // https://stackoverflow.com/a/26870764/22575350
            var hasher = MD5.Create();
            var hassed = hasher.ComputeHash(Encoding.UTF8.GetBytes(@string));
            return ToInt32(hassed);
        }

        public static int MD5From(params byte[] bytes)
        {
            var hasher = MD5.Create();
            var hassed = hasher.ComputeHash(bytes);
            return ToInt32(hassed);
        }
    }
}
