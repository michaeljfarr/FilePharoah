using System;
using EtoTest.Interfaces;
using EtoTest.Model;

namespace EtoTest.SecureFiles
{
    public class SafeRemoteSecureFileBroker : ISecureFileRepository
    {
        private readonly ISecureFileRepository _remoteSecureFileBroker;
        private readonly bool _forceFailure;

        public SafeRemoteSecureFileBroker(ISecureFileRepository remoteSecureFileBroker, bool forceFailure)
        {
            _remoteSecureFileBroker = remoteSecureFileBroker;
            _forceFailure = forceFailure;
        }

        public bool HadError { get; private set; }

        private void DoSafe(Action func)
        {
            if (_forceFailure)
            {
                HadError = true;
                return;
            }
            try
            {
                func();
                HadError = false;
            }
            catch (Exception e)
            {
                HadError = true;
            }
        }

        private T DoSafe<T>(Func<T> func)
        {
            if (_forceFailure)
            {
                HadError = true;
                return default(T);
            }
            try
            {
                var val = func();
                HadError = false;
                return val;
            }
            catch (Exception e)
            {
                HadError = true;
                return default(T);
            }
        }

        public byte[] GetIv()
        {
            return DoSafe(() => _remoteSecureFileBroker.GetIv());
        }

        public byte[] GetSalt()
        {
            return DoSafe(() => _remoteSecureFileBroker.GetSalt());
        }

        public byte[] GetDataFile()
        {
            return DoSafe(() => _remoteSecureFileBroker.GetDataFile());
        }

        public void SaveDataFile(byte[] data, bool branch, int toFileVersion, string stationName)
        {
            DoSafe(() => _remoteSecureFileBroker.SaveDataFile(data, branch, toFileVersion, stationName));
        }

        public DataFileVersion GetDataFileVersion()
        {
            return DoSafe(() => _remoteSecureFileBroker.GetDataFileVersion());
        }

        public void Init()
        {
            _remoteSecureFileBroker.Init();
        }
    }
}