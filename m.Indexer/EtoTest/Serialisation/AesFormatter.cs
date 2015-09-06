using System.IO;
using System.Security.Cryptography;
using EtoTest.Encrypt;
using EtoTest.Interfaces;
using EtoTest.Model;

namespace EtoTest.Serialisation
{
    public class AesFormatter
    {
        private readonly ISecureFileRepository _secureFileRepository;

        public AesFormatter(ISecureFileRepository secureFileRepository)
        {
            _secureFileRepository = secureFileRepository;
        }

        private DisposableAes CreateCrypto(SecureStringOrArray password)
        {
            return new DisposableAes(AesBasedFileEncryption.GenerateKey(password, _secureFileRepository.GetSalt()), _secureFileRepository.GetIv());
        }

        public T DeserializeData<T>(SecureStringOrArray password)
        {
            //return AESBasedFileEncryption.WcfBinaryCompressedDeserializeEncrypted<T>(password);
            using (var stream = new MemoryStream(_secureFileRepository.GetDataFile()))
            using (var aes = CreateCrypto(password))
            using (var cryptoStream = new CryptoStream(
                stream,
                aes.DecryptorTransform,
                CryptoStreamMode.Read))
            {
                var result = ServiceIo.WcfBinaryCompressedDeserialize<T>(cryptoStream);
                return result;
            }
        }

        //public void Init()
        //{
        //    _secureFileRepository.Init();
        //    _secureFileRepository.Sync(null);
        //    GetDataFileVersion();
        //}

        public DataFileVersion GetDataFileVersion()
        {
            return _secureFileRepository.GetDataFileVersion();
        }

        public void SerializeData<T>(SecureStringOrArray password, T objectValue)
        {
            using (var stream = new MemoryStream())
            using (var aes = CreateCrypto(password))
            using (var cryptoStream = new CryptoStream(stream, aes.EncryptorTransform, CryptoStreamMode.Write))
            {
                ServiceIo.WcfBinaryCompressedSerialize(cryptoStream, objectValue);
                cryptoStream.FlushFinalBlock();
                _secureFileRepository.SaveDataFile(stream.ToArray(), false, -1, null);
            }
        }
    }
}