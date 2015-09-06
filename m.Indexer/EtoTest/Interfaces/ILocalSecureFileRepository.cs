using System;
using EtoTest.Encrypt;

namespace EtoTest.Interfaces
{
    public interface ILocalSecureFileRepository : ISecureFileRepository
    {
        void WrapSaveDataVersion(Action action, bool branch, int toFileVersion, string stationName);
        void Init(byte[] iv, byte[] salt);
    }
}