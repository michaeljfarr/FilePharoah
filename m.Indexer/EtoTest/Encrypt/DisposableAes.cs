using System;
using System.Security.Cryptography;

namespace EtoTest.Encrypt
{
    public class DisposableAes : IDisposable
    {
        readonly RijndaelManaged _algorithm;
        ICryptoTransform _decTransform;
        ICryptoTransform _encTransform;

        public DisposableAes(byte[] key, byte[] iv)
        {
            _algorithm = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Key = key,
                IV = iv
            };
        }

        public ICryptoTransform DecryptorTransform
        {
            get
            {
                if (_decTransform == null)
                {
                    _decTransform = _algorithm.CreateDecryptor();
                }
                return _decTransform;
            }
        }
        public ICryptoTransform EncryptorTransform
        {
            get
            {
                if (_encTransform == null)
                {
                    _encTransform = _algorithm.CreateEncryptor();
                }
                return _encTransform;
            }
        }

        public void Dispose()
        {
            _decTransform?.Dispose();
            _encTransform?.Dispose();
            _algorithm.Clear();
        }
    }
}
