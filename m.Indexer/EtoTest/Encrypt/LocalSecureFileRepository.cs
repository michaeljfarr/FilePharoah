using System;
using System.IO;
using EtoTest.Interfaces;
using EtoTest.Model;
using EtoTest.Serialisation;

namespace EtoTest.Encrypt
{
    public class LocalSecureFileRepository : ILocalSecureFileRepository
    {
        private readonly IFilePathProvider _filePathProvider;
        private readonly string _ivName;
        private readonly string _dataFileName;
        private readonly string _dataVersionFileName;
        private readonly string _saltName;

        public LocalSecureFileRepository(IFilePathProvider filePathProvider, string ivName, string dataFileName, string dataVersionFileName, string saltName)
        {
            _filePathProvider = filePathProvider;
            _ivName = ivName;
            _dataFileName = dataFileName;
            _dataVersionFileName = dataVersionFileName;
            _saltName = saltName;
        }

        public byte[] GetIv()
        {
            var ivPath = _filePathProvider.GetFilePath(_ivName);
            return File.ReadAllBytes(ivPath);
        }

        public byte[] GetSalt()
        {
            var saltPath = _filePathProvider.GetFilePath(_saltName);
            return File.ReadAllBytes(saltPath);
        }

        public byte[] GetDataFile()
        {
            var dataFilePath = _filePathProvider.GetFilePath(_dataFileName);
            return File.ReadAllBytes(dataFilePath);
        }

        public DataFileVersion GetDataFileVersion()
        {
            var dataVersionFilePath = _filePathProvider.GetFilePath(_dataVersionFileName);
            //File.ReadAllBytes(dataFilePath);
            if (!File.Exists(dataVersionFilePath))
            {
                return null;
            }
            using (var file = File.OpenRead(dataVersionFilePath))
            {
                var versionData = JsonStringHelper.ReadJsonObject<DataFileVersion>(file, JsonStringHelper.MaxJsonSize);
                return versionData;
            }
        }

        public void Init()
        {
            throw new NotImplementedException();
        }

        public void SaveDataFile(byte[] data, bool branch, int toFileVersion, String stationName)
        {
            WrapSaveDataVersion(() => SaveLocalDataFile(data), branch, toFileVersion, stationName);
        }

        public void WrapSaveDataVersion(Action action, bool branch, int toFileVersion, string stationName)
        {
            var dataVersionFilePath = _filePathProvider.GetFilePath(_dataVersionFileName);
            using (var file = File.Open(dataVersionFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {

                var versionData = JsonStringHelper.ReadJsonObject<DataFileVersion>(file, JsonStringHelper.MaxJsonSize) ?? new DataFileVersion { FromVersionId = 0 };
                var newDataFileVersion = new DataFileVersion
                {
                    CurrentVersionId = toFileVersion,
                    StationName = stationName,
                    BeforeOperation = "Update",
                    FromVersionId = versionData.FromVersionId
                };
                if (!branch)
                {
                    newDataFileVersion.FromVersionId = toFileVersion;
                }
                file.Seek(0, SeekOrigin.Begin);
                JsonStringHelper.WriteJsonObject(file, newDataFileVersion, compress: false);
                file.SetLength(file.Position);
                try
                {

                    action();

                    newDataFileVersion.BeforeOperation = "";
                    file.Seek(0, SeekOrigin.Begin);
                    JsonStringHelper.WriteJsonObject(file, newDataFileVersion, compress: false);
                    file.SetLength(file.Position);
                }
                catch (Exception e)
                {
                    file.Seek(0, SeekOrigin.Begin);
                    JsonStringHelper.WriteJsonObject(file, versionData, compress: false);
                    file.SetLength(file.Position);
                }
            }
        }

        public void Init(byte[] iv, byte[] salt)
        {
            var ivPath = _filePathProvider.GetFilePath(_ivName);
            File.WriteAllBytes(ivPath, iv);
            var saltPath = _filePathProvider.GetFilePath(_saltName);
            File.WriteAllBytes(saltPath, salt);
        }

        private void SaveLocalDataFile(byte[] data)
        {
            var dataFilePath = _filePathProvider.GetFilePath(_dataFileName);
            String tempDataFilePath = AesBasedFileEncryption.SwapExtension(dataFilePath, ".tmp");
            File.WriteAllBytes(tempDataFilePath, data);
            AesBasedFileEncryption.SafeFileMove(dataFilePath, tempDataFilePath);
        }
    }
}