using System;
using EtoTest.Encrypt;
using EtoTest.Interfaces;
using EtoTest.Model;

namespace EtoTest.SecureFiles
{
    public class SecureFileBroker : ISecureFileBroker
    {
        private readonly SafeRemoteSecureFileBroker _remoteSecureFileBroker;
        private readonly ILocalSecureFileRepository _localSecureFileRepository;

        public SecureFileBroker(ISecureFileRepository remoteSecureFileBroker, ILocalSecureFileRepository localSecureFileRepository, bool forceFailure)
        {
            _remoteSecureFileBroker = new SafeRemoteSecureFileBroker(remoteSecureFileBroker, forceFailure);
            _localSecureFileRepository = localSecureFileRepository;
        }

        public static SecureFileBroker Create(IFilePathProvider filePathProvider, string mSecretsSecretKey, string ivName, string dataFileName, string dataVersionFileName, string saltName)
        {
            var remoteSecureFileBroker = new Ec2SecureFileBroker(mSecretsSecretKey, ivName, dataFileName, saltName);
            var localSecureFileRepository = new LocalSecureFileRepository(filePathProvider, ivName, dataFileName, dataVersionFileName, saltName);
            return new SecureFileBroker(remoteSecureFileBroker, localSecureFileRepository, false);
        }

        public byte[] GetIv()
        {
            //get it local if we can; else get it remote
            var iv = _localSecureFileRepository.GetIv() ?? _remoteSecureFileBroker.GetIv();
            if (iv == null)
            {
                throw new ApplicationException("Could not load IV");
            }
            return iv;
        }

        public byte[] GetSalt()
        {
            //get it local if we can; else get it remote
            var salt = _localSecureFileRepository.GetSalt() ?? _remoteSecureFileBroker.GetSalt();
            if (salt == null)
            {
                throw new ApplicationException("Could not load Salt");
            }
            return salt;
        }

        public byte[] GetDataFile()
        {
            //just get the local data file (user needs to either sync or merge to get the latest)
            var data = _localSecureFileRepository.GetDataFile();
            if (data == null)
            {
                throw new ApplicationException("Could not load local data");
            }
            return data;
        }

        public DataFileVersion GetDataFileVersion()
        {
            var data = _localSecureFileRepository.GetDataFileVersion();
            if (data == null)
            {
                throw new ApplicationException("Could not load local data file version");
            }
            return data;
        }

        public void Init()
        {
            //_remoteSecureFileBroker.Init();
            //_localSecureFileRepository.Init(_remoteSecureFileBroker.GetIv(), _remoteSecureFileBroker.GetSalt());
        }


        /// <summary>
        /// Returned true if the local repository was in sync, return false if the local file was overwritten
        /// </summary>
        /// <returns></returns>
        public bool Sync(byte[] data)
        {
            var localDataFileVersion = _localSecureFileRepository.GetDataFileVersion();
            var remoteDataVersion = _remoteSecureFileBroker.GetDataFileVersion();
            if (remoteDataVersion == null)
            {
                throw new ApplicationException("Could not load remote data file version");
            }
            if (localDataFileVersion?.CurrentVersionId == null)
            {
                //there is no local, save remote locally
                DownloadLatest();
                return true;
            }
            if (localDataFileVersion.CurrentVersionId == remoteDataVersion.CurrentVersionId)
            {
                //if neither has changed do nothing - or update both locations;
                if (data != null && AreDifferent(data, _localSecureFileRepository.GetDataFile()))
                {

                    _remoteSecureFileBroker.SaveDataFile(data, false, (localDataFileVersion.CurrentVersionId ?? 0) + 1, localDataFileVersion.StationName);
                    _localSecureFileRepository.SaveDataFile(data, false, (localDataFileVersion.CurrentVersionId ?? 0) + 1, localDataFileVersion.StationName);
                }
                return false;
            }
            if (localDataFileVersion.FromVersionId == localDataFileVersion.CurrentVersionId)
            {
                //if remote has changed, but local has not changed then overwrite local with remote
                DownloadLatest();
                return true;
            }
            if (localDataFileVersion.FromVersionId == remoteDataVersion.CurrentVersionId)
            {
                //if local has changed, but remote has not changed then overwrite remote with local
                _localSecureFileRepository.WrapSaveDataVersion(
                    () => _remoteSecureFileBroker.SaveDataFile(data ?? _localSecureFileRepository.GetDataFile(), false, localDataFileVersion.CurrentVersionId.Value, localDataFileVersion.StationName),
                    false, localDataFileVersion.CurrentVersionId.Value, localDataFileVersion.StationName);
                return true;
            }
            //if remote and local has changed; save local up to remote then overwrite local with remote
            _remoteSecureFileBroker.SaveDataFile(data ?? _localSecureFileRepository.GetDataFile(), true, localDataFileVersion.CurrentVersionId.Value, localDataFileVersion.StationName);
            DownloadLatest();
            return true;
        }

        static bool AreDifferent(byte[] a1, byte[] a2)
        {
            if (a1 == a2)
                return false;
            if (a1.Length != a2.Length)
                return true;

            for (int i = 0; i < a1.Length; i++)
                if (a1[i] != a2[i])
                    return true;

            return false;
        }

        private void DownloadLatest()
        {
            //var localData = _localSecureFileRepository.GetDataFileVersion();
            //if (localData == null || String.IsNullOrWhiteSpace(localData.StationName))
            {
                //local version will be created ... throw new ApplicationException("Could not load local data file version");
            }
            var remoteData = _remoteSecureFileBroker.GetDataFile();
            var remoteDataVersion = _remoteSecureFileBroker.GetDataFileVersion();
            if (remoteDataVersion == null || !remoteDataVersion.CurrentVersionId.HasValue)
            {
                throw new ApplicationException("Could not load remote data file version");
            }
            _localSecureFileRepository.SaveDataFile(remoteData, false, remoteDataVersion.CurrentVersionId.Value, "Test");
        }

        public String Diff()
        {
            //write a list of all the different keys (added, removed and updated) to a text file and display
            //between the latest remote and the last merge conflict of this workstation on remote.
            //user can swith to the last merge conflict and copy the results to a text file if they wish.
            return "";
        }

        public void SaveDataFile(byte[] data, bool branch, int toFileVersion, String stationName)
        {
            //look at the remote location and the local, decide which is the last and use that
            //save to both locations if possible.
            var remoteDataVersion = _remoteSecureFileBroker.GetDataFileVersion();
            if (remoteDataVersion == null && _remoteSecureFileBroker.HadError)
            {
                var localDataVersion = _localSecureFileRepository.GetDataFileVersion();
                _localSecureFileRepository.SaveDataFile(data, true, (localDataVersion.CurrentVersionId ?? 0) + 1, localDataVersion.StationName);
            }
            else
            {
                Sync(data);
            }
        }

        public void InitRemote()
        {
            _remoteSecureFileBroker.Init();

        }
    }
}