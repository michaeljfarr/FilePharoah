using System.IO;
using EtoTest.Interfaces;

namespace EtoTest.IO
{
    public class FolderBasedFilePathProvider : IFilePathProvider
    {
        private readonly string _directory;

        public FolderBasedFilePathProvider(string directory)
        {
            _directory = directory;
        }

        public string GetFilePath(string fileName)
        {
            return Path.Combine(_directory, fileName);
        }
    }
}