using System;
using System.IO;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.IO;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using EtoTest.Interfaces;
using EtoTest.Model;

namespace EtoTest.SecureFiles
{

    public class Ec2SecureFileBroker : ISecureFileRepository
    {
        private const string VersionIdPropertyName = "MVersionId";
        private readonly string _ivName;
        private readonly string _dataFileName;
        private readonly string _saltName;
        private string _bucketName = "m.secrets";
        private string _accessKey = "AKIAJT75OLS6A4IBKRQA";
        private readonly string _secretKey;
        private string _secretsDirectory = "M.Secrets";

        public Ec2SecureFileBroker(string mSecretsSecretKey, string ivName, string dataFileName, string saltName)
        {
            _secretKey = mSecretsSecretKey;
            _ivName = ivName;
            _dataFileName = dataFileName;//"masternew.enc"
            _saltName = saltName;
        }

        public byte[] GetIv()
        {
            return DownloadFileFromMuiDir(_ivName);
        }

        public byte[] GetSalt()
        {
            return DownloadFileFromMuiDir(_saltName);
        }

        public byte[] GetDataFile()
        {
            return DownloadFileFromMuiDir(_dataFileName);
        }

        private byte[] DownloadFileFromMuiDir(string fileName)
        {
            using (IAmazonS3 s3Client = CreateS3Client())
            {
                var fileKey = $"{_secretsDirectory}/{fileName}";

                var s3FileInfo = new S3FileInfo(s3Client, _bucketName, fileKey);

                if (!s3FileInfo.Exists)
                {
                    return null;
                }

                using (var fileTransferUtility = new TransferUtility(s3Client))
                using (var stream = fileTransferUtility.OpenStream(_bucketName, fileKey))
                using (var ms = new MemoryStream((int)s3FileInfo.Length))
                {
                    stream.CopyTo(ms);
                    var bytes = ms.ToArray();
                    return bytes;
                }
            }
        }

        private AmazonS3Client CreateS3Client()
        {
            var credentials = new BasicAWSCredentials(_accessKey, _secretKey);
            var client = new AmazonS3Client(credentials, RegionEndpoint.APSoutheast2);
            return client;
        }

        public void SaveDataFile(byte[] data, bool branch, int toFileVersion, String stationName)
        {
            using (IAmazonS3 s3Client = CreateS3Client())
            {
                TransferUtility fileTransferUtility = new TransferUtility(s3Client);

                if (branch)
                {
                    //upload a branched copy, but do not update the main version 
                    var newFileName = AddPreSuffixToFileName($"{stationName}_{toFileVersion.ToString().PadLeft(4, '0')}", _dataFileName);
                    var fileKey = $"{_secretsDirectory}/{newFileName}";

                    using (var ms = new MemoryStream(data))
                    {
                        fileTransferUtility.Upload(ms, _bucketName, fileKey);
                    }
                }
                else
                {
                    //copy the current main to a new name propertyValue as a suffix then
                    //upload a new copy 
                    var fileKey = $"{_secretsDirectory}/{_dataFileName}";
                    var response = s3Client.GetObjectMetadata(new GetObjectMetadataRequest() { BucketName = _bucketName, Key = fileKey });
                    var existingVersion = response.Metadata[$"x-amz-meta-{VersionIdPropertyName.ToLower()}"];
                    var oldFileKey = $"{_secretsDirectory}.old/{_dataFileName}.{existingVersion}";
                    s3Client.CopyObject(new CopyObjectRequest()
                    {
                        SourceBucket = _bucketName,
                        DestinationBucket = _bucketName,
                        SourceKey = fileKey,
                        DestinationKey = oldFileKey
                    });
                    using (var ms = new MemoryStream(data))
                    {
                        var uploadRequest = new TransferUtilityUploadRequest()
                        {
                            BucketName = _bucketName,
                            Key = fileKey,
                            InputStream = ms
                        };
                        uploadRequest.Metadata[VersionIdPropertyName] = toFileVersion.ToString();
                        fileTransferUtility.Upload(uploadRequest);
                    }
                }
            }
        }


        private static string AddPreSuffixToFileName(String preSuffix, string dataFileName)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(dataFileName);
            var fileNameExtension = Path.GetExtension(dataFileName);
            string newFileName = $"{fileNameWithoutExtension}.{preSuffix}{fileNameExtension}";
            return newFileName;
        }


        public DataFileVersion GetDataFileVersion()
        {
            var currentVersionId = LookupVersionId();
            return new DataFileVersion
            {
                CurrentVersionId = currentVersionId,
                FromVersionId = currentVersionId
            };
        }

        private int LookupVersionId()
        {
            var fileKey = $"{_secretsDirectory}/{_dataFileName}";
            using (IAmazonS3 s3Client = CreateS3Client())
            {
                var s3FileInfo = new S3FileInfo(s3Client, _bucketName, fileKey);

                if (!s3FileInfo.Exists)
                {
                    return 0;
                }

                var response = s3Client.GetObjectMetadata(new GetObjectMetadataRequest() { BucketName = _bucketName, Key = fileKey });
                var fileProperty = response.Metadata["x-amz-meta-mversionid"];
                int versionId;
                if (fileProperty == null || !int.TryParse(fileProperty, out versionId))
                {
                    return 0;
                }
                return versionId;
            }
        }

        public void Init()
        {
        }
    }
}
