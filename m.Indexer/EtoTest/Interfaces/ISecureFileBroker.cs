using EtoTest.Encrypt;

namespace EtoTest.Interfaces
{
    public interface ISecureFileBroker : ISecureFileRepository
    {
        bool Sync(byte[] data);
    }
}