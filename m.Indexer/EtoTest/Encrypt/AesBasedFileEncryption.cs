using System;
using System.Configuration;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using EtoTest.IO;
using EtoTest.Model;
using EtoTest.Serialisation;

namespace EtoTest.Encrypt
{
    public static class AesBasedFileEncryption
    {
        private static void SetupDataName(string dataFileName)
        {
            if (!DoesSaltExist(dataFileName) && !DoesIVExist(dataFileName))
            {
                GenerateAndStoreSalt(dataFileName);
                GenerateAndStoreIv(dataFileName);
            }
        }

        private static DisposableAes CreateCrypto(SecureStringOrArray password, String dataFileName)
        {
            String ivPath = GetIVPath(dataFileName);
            return new DisposableAes(GenerateKey(password, dataFileName), File.ReadAllBytes(ivPath));
        }

        public static void BinarySerializeObjectEncrypted(String dataFileName, SecureStringOrArray password, object objectValue)
        {
            SetupDataName(dataFileName);
            String dataFilePath = GetDataFilePath(dataFileName);
            String tempDataFilePath = SwapExtension(dataFilePath, ".tmp");
            using (var stream = File.Open(tempDataFilePath, FileMode.Create))
            using (DisposableAes aes = CreateCrypto(password, dataFileName))
            using (CryptoStream cryptoStream = new CryptoStream(
                stream,
                aes.EncryptorTransform,
                CryptoStreamMode.Write))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(cryptoStream, objectValue);
            }
            SafeFileMove(dataFilePath, tempDataFilePath);
        }

        public static void WcfBinaryCompressedSerializeEncrypted<T1>(String dataFileName, SecureStringOrArray password, T1 objectValue)
        {
            SetupDataName(dataFileName);
            String dataFilePath = GetDataFilePath(dataFileName);
            String tempDataFilePath = SwapExtension(dataFilePath, ".tmp");
            using (var stream = File.Open(tempDataFilePath, FileMode.Create))
            using (DisposableAes aes = CreateCrypto(password, dataFileName))
            using (CryptoStream cryptoStream = new CryptoStream(
                stream,
                aes.EncryptorTransform,
                CryptoStreamMode.Write))
            {
                ServiceIo.WcfBinaryCompressedSerialize(cryptoStream, objectValue);
            }
            SafeFileMove(dataFilePath, tempDataFilePath);
        }



        public static void SafeFileMove(String dataFilePath, String tempDataFilePath)
        {
            if (File.Exists(dataFilePath))
            {
                string oldDataFilePath = SwapExtension(dataFilePath, ".old");
                DeleteIfExists(oldDataFilePath);
                File.Move(dataFilePath, oldDataFilePath);
                File.Move(tempDataFilePath, dataFilePath);
                File.Delete(oldDataFilePath);
            }
            else
            {
                File.Move(tempDataFilePath, dataFilePath);
            }
        }

        public static string SwapExtension(string dataFilePath, string newExtension)
        {
            if (Path.HasExtension(dataFilePath))
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(dataFilePath);
                var directoryName = Path.GetDirectoryName(dataFilePath);
                var newFileName = Path.Combine(directoryName, $"{fileNameWithoutExtension}{newExtension}");
                return newFileName;
            }
            else
            {
                string newFileName = $"{dataFilePath}{newExtension}";
                return newFileName;
            }
        }

        private static void DeleteIfExists(String oldDataFilePath)
        {
            if (File.Exists(oldDataFilePath))
            {
                File.Delete(oldDataFilePath);
            }
        }

        public static T1 BinaryDeserializeEncryptedObject<T1>(String dataFileName, SecureStringOrArray password)
        {
            ConvertToDCStyle(dataFileName, password);
            String dataFilePath = GetDataFilePath(dataFileName);
            using (var stream = File.Open(dataFilePath, FileMode.Open))
            using (DisposableAes aes = CreateCrypto(password, dataFileName))
            using (CryptoStream cryptoStream = new CryptoStream(
                stream,
                aes.DecryptorTransform,
                CryptoStreamMode.Read))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                var result = (T1)formatter.Deserialize(cryptoStream);

                return result;
            }
        }

        public static T1 WcfBinaryCompressedDeserializeEncrypted<T1>(String dataFileName, SecureStringOrArray password)
        {
            var dataFilePath = GetDataFilePath(dataFileName);
            using (var stream = File.Open(dataFilePath, FileMode.Open))
            using (var aes = CreateCrypto(password, dataFileName))
            using (var cryptoStream = new CryptoStream(
                stream,
                aes.DecryptorTransform,
                CryptoStreamMode.Read))
            {
                var result = ServiceIo.WcfBinaryCompressedDeserialize<T1>(cryptoStream);
                return result;
            }
        }
        
        private static void ConvertToDCStyle(String dataFileName, SecureStringOrArray password)
        {
            var oldStyleSet = BinaryDeserializeEncryptedObject<CredentialSet>(dataFileName, password);
            WcfBinaryCompressedSerializeEncrypted(dataFileName + "new", password, oldStyleSet);
            //MtomSerializeEncryptedObject<M.UI.Model.NoString.CredentialSet>(newDataFileName, password, newCredentials);
            //var newSet = MtomDeserializeEncryptedObject<M.UI.Model.NoString.CredentialSet>(newDataFileName, password);

        }

        private static byte[] GenerateKey(SecureStringOrArray password, String saltName)
        {
            var filePath = GetSaltFilePath(saltName);
            return GenerateKey(password, File.ReadAllBytes(filePath));
        }

        public static byte[] GenerateKey(SecureStringOrArray password, byte[] salt)
        {
            var passwordBytes2 = password.ByteArray;
            try
            {
                //the recommended number of iterations is 1000, lets just chose something random that is near to that.
                var foo = new Rfc2898DeriveBytes(passwordBytes2, salt, 1076);
                var finalBytes = foo.GetBytes(32);
                return finalBytes;
            }
            finally
            {
                password.ZeroBytesIfRecreatable(passwordBytes2);
            }
        }


        private static byte[] GenerateAndStoreIv(String ivName)
        {
            return StoreRandomData(ivName, GetIVPath(ivName), 16);
        }

        private static byte[] GenerateAndStoreSalt(String saltName)
        {
            return StoreRandomData(saltName, GetSaltFilePath(saltName), 8);
        }

        private static byte[] StoreRandomData(String dataName, String dataPath, int length)
        {
            var array = GenerateRandomData(length);

            if (File.Exists(dataPath))
            {
                throw new ApplicationException("RandomData already exists: " + dataName);
            }
            using (var stream = File.OpenWrite(dataPath))
            {
                stream.Write(array, 0, array.Length);
            }
            return array;
        }

        private static byte[] GenerateRandomData(int length)
        {
            var randomNumberGenerator = new RNGCryptoServiceProvider();
            return randomNumberGenerator.GenerateRandomData(length);
        }

        /// <summary>
        /// Generate IV - store this within profile and backup somewhere it wont get lost
        /// </summary>
        private static string GetIVPath(String ivName)
        {
            return GetSpecialPath(Environment.SpecialFolder.LocalApplicationData, "{0}.iv", ivName);
        }

        public static string GetVersionFilePath(String ivName)
        {
            return GetSpecialPath(Environment.SpecialFolder.LocalApplicationData, "{0}.ver", ivName);
        }

        private static string GetDataFilePath(String dataFileName)
        {
            return GetSpecialPath(Environment.SpecialFolder.MyDocuments, "{0}.enc", dataFileName);
        }

        private static string GetSaltFilePath(String saltName)
        {
            return GetSpecialPath(Environment.SpecialFolder.LocalApplicationData, "{0}.salt", saltName);
        }

        private static bool DoesSaltExist(string saltName)
        {
            return File.Exists(GetSaltFilePath(saltName));
        }

        private static bool DoesIVExist(string saltName)
        {
            return File.Exists(GetIVPath(saltName));
        }

        private static bool DoesDataFileExist(string saltName)
        {
            return File.Exists(GetDataFilePath(saltName));
        }

        private static string GetSpecialPath(Environment.SpecialFolder specialFolder, string fileNameFormat, string ivName)
        {
            var userSpecificHiddenDataFolderPath = Environment.GetFolderPath(specialFolder);
            var dataFilePath = Path.Combine(userSpecificHiddenDataFolderPath, string.Format(fileNameFormat, ivName));
            var alternatePathConfig = ConfigurationManager.AppSettings["AlternatePath"];
            if (alternatePathConfig != null)
            {
                var alternatePath = ExtensionMethods.EnsureSlashSuffix(alternatePathConfig);
                var alternateDataFilePath = Path.Combine(alternatePath, String.Format(fileNameFormat, ivName));
                if (File.Exists(Path.Combine(alternatePath, "63643784855")) && File.Exists(dataFilePath))
                {
                    File.Copy(dataFilePath, alternateDataFilePath, true);
                }
                if (Directory.Exists(alternatePath) && !File.Exists(dataFilePath))
                {
                    dataFilePath = alternateDataFilePath;
                }
            }

            return dataFilePath;
        }
    }
}