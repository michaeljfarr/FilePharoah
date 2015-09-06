using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace m.Indexer
{

    public class PathEnumerator
    {
        private const string GitFolderRegex = "[/\\\\].git";

        private readonly string _rootPath;
        private readonly string[] _excluded;
        public List<NodeEnvelope> NodeEnvelopes { get; set; }

        public PathEnumerator(string rootPath, string[] excluded)
        {
            _rootPath = rootPath;
            _excluded = excluded;
            NodeEnvelopes = new List<NodeEnvelope>();

        }

        private bool IsExcluded(FileSystemInfo fileOrDirectory)
        {
            var fullName = fileOrDirectory.FullName;
            foreach (var exclude in _excluded)
            {
                if (Regex.IsMatch(fullName, exclude, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
        private static bool IsGit(FileSystemInfo fileOrDirectory)
        {
            return Regex.IsMatch(fileOrDirectory.FullName, GitFolderRegex, RegexOptions.IgnoreCase);
        }

        public void Index()
        {
            if (String.IsNullOrWhiteSpace(_rootPath) || !Directory.Exists(_rootPath))
            {
                throw new ApplicationException($"Root path does not exist {_rootPath}");
            }
            var rootInfo = new DirectoryInfo(Path.GetFullPath(_rootPath));

            List<DirectoryInfo> toIndex = new List<DirectoryInfo>();
            NodeEnvelopes.Add(CreateNodeFromDirectory(rootInfo, toIndex, true));
            while (toIndex.Any())
            {
                var currentDirectory = toIndex[0];
                toIndex.RemoveAt(0);
                IEnumerable<FileSystemInfo> directoryContents = null;
                try
                {
                    directoryContents = currentDirectory.EnumerateFileSystemInfos();
                }
                catch (UnauthorizedAccessException e)
                {
                    continue;
                }
                foreach (var fileOrDirectory in directoryContents)
                {
                    var dirInfo = fileOrDirectory as DirectoryInfo;
                    var fileInfo = fileOrDirectory as FileInfo;
                    if (dirInfo != null)
                    {
                        var node = CreateNodeFromDirectory(dirInfo, toIndex, false);
                        NodeEnvelopes.Add(node);
                    }
                    else if (fileInfo != null)
                    {
                        if(fileInfo.Attributes.HasFlag(FileAttributes.System) || fileInfo.Attributes.HasFlag(FileAttributes.Hidden) || fileInfo.Extension.Equals(".lnk"))
                        {
                            continue;
                        }
                        var node = new NodeEnvelope()
                        {
                            Header = new NodeHeader()
                            {
                                NodeType = NodeType.File,
                                FileName = fileInfo.Name,
                                FullDirectoryPath = fileInfo.DirectoryName,
                                FileNumBytes = fileInfo.Length,
                                LastModifieDateTime = fileInfo.LastWriteTimeUtc
                            }
                        };
                        NodeEnvelopes.Add(node);
                    }
                }
            }
        }

        private NodeEnvelope CreateNodeFromDirectory(DirectoryInfo dirInfo, List<DirectoryInfo> toIndex, bool isRoot)
        {
            var gitUrl = GetGitUrl(dirInfo);
            var isGoogleDrive = IsGoogleDrive(dirInfo);
            var isLink = dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
            var isSystem = dirInfo.Attributes.HasFlag(FileAttributes.System) ||
                           dirInfo.Attributes.HasFlag(FileAttributes.Hidden);
            var node = new NodeEnvelope()
            {
                Header = new NodeHeader()
                {
                    NodeType = gitUrl != null || isGoogleDrive ? NodeType.Clone : isLink ? NodeType.Link : isSystem ? NodeType.System : NodeType.Directory,
                    FileName = dirInfo.Name,
                    FullDirectoryPath = Path.GetDirectoryName(dirInfo.FullName) ?? dirInfo.FullName.Substring(0, dirInfo.FullName.Length - dirInfo.Name.Length),
                    FileNumBytes = null,
                    LastModifieDateTime = dirInfo.LastWriteTimeUtc,
                    CloneUrl = gitUrl
                }
            };
            bool isExcluded = IsExcluded(dirInfo);
            if (string.IsNullOrWhiteSpace(gitUrl) && !isLink && !isSystem && !isGoogleDrive && !isExcluded)
            {
                toIndex.Add(dirInfo);
            }
            return node;
        }

        private bool IsGoogleDrive(DirectoryInfo dirInfo)
        {

            //IconFile=C:\Program Files (x86)\Google\Drive\googledrivesync.exe
            var iniFile = Path.Combine(dirInfo.FullName, "desktop.ini");
            if (File.Exists(iniFile))
            {
                var configLines = File.ReadAllLines(iniFile);
                foreach (var configLine in configLines)
                {
                    var match = Regex.Match(configLine, "IconFile.*googledrivesync");
                    if (match.Success)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static String GetGitUrl(DirectoryInfo dirInfo)
        {
            var gitConfig = Path.Combine(dirInfo.FullName, ".git/config");
            if (File.Exists(gitConfig))
            {
                var gitConfigLines = File.ReadAllLines(gitConfig);
                foreach (var gitConfigLine in gitConfigLines)
                {
                    var match = Regex.Match(gitConfigLine, "url = (.*)");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
            }
            return null;
        }
    }
}