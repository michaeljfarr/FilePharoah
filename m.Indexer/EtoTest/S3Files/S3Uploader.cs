using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using EtoTest.Interfaces;
using EtoTest.Model;

namespace EtoTest.S3Files
{
    class S3Uploader
    {
        public const string FileSharingBucketName = "alphero-file-sharing";
        private readonly ISecretKeyProvider _secretKeyProvider;

        public S3Uploader(ISecretKeyProvider secretKeyProvider)
        {
            _secretKeyProvider = secretKeyProvider;
        }

        public IAmazonS3 GetS3Client()
        {
            var s3FilesPrivateKey = _secretKeyProvider.GetSecret(SecretKeyConstants.S3FilesPrivateKeyName);
            if (s3FilesPrivateKey == null)
            {
                return null;
            }
            return GetS3Client(s3FilesPrivateKey);
        }

        private static AmazonS3Client GetS3Client(String s3FilesPrivateKey)
        {
            //user=alphero-file-sharing-user
            var credentials = new BasicAWSCredentials("AKIAIKUGNVOPB7ZOOWRA", s3FilesPrivateKey);
            var client = new AmazonS3Client(credentials, RegionEndpoint.APSoutheast2);
            return client;
        }

        public static S3File StoreForLaterAccess(IAmazonS3 client, Stream sourceStream, String fileName)
        {
            var fileKey = $"{Guid.NewGuid().ToString().Replace("-", "")}/{fileName}";
            //var fileKey = fileName;
            TransferUtility fileTransferUtility = new TransferUtility(client);
            fileTransferUtility.Upload(sourceStream, FileSharingBucketName, fileKey);

            return new S3File
            {
                AgeInDays = 0,
                Key = fileKey,
                Check = false
            };
        }

        public Uri GetPublicUri(String s3Key)
        {
            return GetPublicUri(this, s3Key);
        }

        private static Uri GetPublicUri(S3Uploader s3Uploader, String s3Key)
        {
            var client = s3Uploader.GetS3Client();
            var expiryUrlRequest = new GetPreSignedUrlRequest() { BucketName = FileSharingBucketName, Key = s3Key, Expires = DateTime.Now.AddDays(10), };
            string url = client.GetPreSignedURL(expiryUrlRequest);
            return new Uri(url);
        }

        public static IEnumerable<S3File> GetObjects(IAmazonS3 client)
        {
            ListObjectsRequest request = new ListObjectsRequest();
            request.BucketName = FileSharingBucketName;
            do
            {
                ListObjectsResponse response = client.ListObjects(request);

                // Process response.
                // ...
                foreach (var s3Object in response.S3Objects)
                {
                    yield return
                        new S3File
                        {
                            Key = s3Object.Key,
                            Check = false,
                            AgeInDays = (DateTime.Now - s3Object.LastModified).Days
                        };
                }

                // If response is truncated, set the marker to get the next 
                // set of keys.
                if (response.IsTruncated)
                {
                    request.Marker = response.NextMarker;
                }
                else
                {
                    request = null;
                }
            } while (request != null);
        }

        public void RefreshFiles(ObservableCollection<S3File> s3Files)
        {
            using (var s3 = GetS3Client())
            {
                s3Files.Clear();
                foreach (var key in S3Uploader.GetObjects(s3))
                {
                    s3Files.Add(key);
                }
            }
        }
        public void DeleteFiles(List<S3File> selectedItems)
        {
            using (var s3 = GetS3Client())
            {
                foreach (var selectedItem in selectedItems)
                {
                    s3.DeleteObject(S3Uploader.FileSharingBucketName, selectedItem.Key);
                }
            }
        }

    }
}
