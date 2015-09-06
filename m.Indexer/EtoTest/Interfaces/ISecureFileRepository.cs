using EtoTest.Model;

namespace EtoTest.Interfaces
{
    public interface ISecureFileRepository
    {
        byte[] GetIv();
        byte[] GetSalt();
        byte[] GetDataFile();
        void SaveDataFile(byte[] data, bool branch, int toFileVersion, string stationName);
        DataFileVersion GetDataFileVersion();
        void Init();

    }
}