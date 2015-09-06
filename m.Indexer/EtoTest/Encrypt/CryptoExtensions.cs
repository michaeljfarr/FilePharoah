using System.Security.Cryptography;

namespace EtoTest.Encrypt
{
    public static class CryptoExtensions
    {
        public static byte[] GenerateRandomData(this RNGCryptoServiceProvider randomNumberGenerator, int length)
        {
            byte[] array = new byte[length];
            randomNumberGenerator.GetBytes(array);
            return array;
        }
    }
}