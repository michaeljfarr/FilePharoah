using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace m.Comparitor
{
    class Program
    {
        private static readonly string[] ArchivePrefixes = new[]
        {
            "./AA/",
            "./Casper/",
            "./Sorted/",
        };

        private static readonly string[] CopyPrefixes = new[]
        {
            "./Mesh",
            "./WWF",
        };

        static void Main(string[] args)
        {
            var lines = System.IO.File.ReadAllLines(@"C:\Users\micha\Documents\SyncFiles\dropboxfiles.txt");
            var regex = new Regex("staff\\s*(\\d*) ([^.]*) (.*)");
            //10703914      328 -rwxr-xr-x    1 username      staff              167225 12 May 17:39 ./AA/.sync/bla/bla.jpg
            var dropboxFiles = lines.Where(a => !a.Contains("drwx")).Select(line =>
            {
                var matches = regex.Match(line);
                return new FileReference { size = matches.Groups[1].Value, date = matches.Groups[2].Value.Trim(), path = matches.Groups[3].Value };
            }).Where(a => !a.path.Contains("/.sync/") && !a.path.Contains(".qicon") && !a.path.Contains(".DS_Store")).ToList();

            var dropboxRoots = new List<String>();
            foreach (var dropboxFile in dropboxFiles)
            {
                var root = GetRootPath(dropboxFile);
                if (dropboxRoots.All(a => a != root))
                {
                    dropboxRoots.Add(root);
                }
            }

            //btsync.txt
            var lines2 = System.IO.File.ReadAllLines(@"C:\Users\micha\Documents\SyncFiles\btsyncfiles.txt");
            var regex2 = new Regex("administ\\s*(\\d*) ([^.]*)(.*)");
            //-rwxrwxr-x    1 username    administ    281574 Tue Jun 19 13:51:45 2012 /share/CACHEDEV1_DATA/Designers/bla/bla.jpg*
            var bittorrentFiles = lines2.Select(line =>
            {
                var matches = regex2.Match(line);
                return new FileReference { size = matches.Groups[1].Value, date = matches.Groups[2].Value.Trim(), path = matches.Groups[3].Value.TrimEnd('*') };
            }).Where(a => a.path != "" && a.path != "./" && !a.path.Contains("/.sync/") && !a.path.Contains(".qicon") && !a.path.Contains(".DS_Store")).ToDictionary(a => a.path.ToLower());
            var filesInDropboxNotInBittorrentSync = new List<String>();
            var filesInDropboxWrongSizeAndNewer = new List<String>();
            var filesInDropboxWrongSizeAndOlder = new List<String>();
            var stringBuilderNotes = new List<String>();
            stringBuilderNotes.Add(string.Format("found: {0} files in dropbox\n", dropboxFiles.Count));
            stringBuilderNotes.Add(string.Format("found: {0} files in bit torrent sync\n", bittorrentFiles.Count));
            var bittorrentFilesByName = bittorrentFiles.ToLookup(a => GetFileSearchkey(a.Value), a => a.Value);

            var filesUniqueToBitsync = new StringBuilder();
            //var dropboxDictionary = dropboxFiles.ToDictionary(a => a.path.ToLower());
            //var foldersWithFilesUniqueToBTSync = new List<String>();
            //foreach (var bittorrentFile in bittorrentFiles.Where(a => !IsArchive(a.Value) && !MatchInDropBox(dropboxDictionary, a.Value)))
            //{
            //    if (CopyToDropbox(bittorrentFile.Value, dropboxRoots))
            //    {
            //        CopyFile(filesUniqueToBitsync, bittorrentFile.Value, "../FilesUniqueToBitsync");
            //    }
            //    stringBuilderNotes.Add($"{bittorrentFile.Value.path} \tDate {bittorrentFile.Value.date} Bitsync only: \n");
            //    var folder = GetRootPath(bittorrentFile.Value);
            //    if (!foldersWithFilesUniqueToBTSync.Any(a => a.StartsWith(folder)))
            //    {
            //        stringBuilderNotes.Add($"0.4 Bitsync folder: {folder}\n");
            //        foldersWithFilesUniqueToBTSync.Add(folder);
            //    }

            //}
            var newerInDropboxFiles = new StringBuilder();
            var obsoleteDropboxFiles = new StringBuilder();
            
            var sameSizedDropboxFilesForComparison = new StringBuilder();
            var sameSizedBitsyncFilesForComparison = new StringBuilder();
            var filesUniqueToDropbox = new StringBuilder();

            foreach (var dropboxFile in dropboxFiles.Where(a=> !IsArchive(a)))
            {
                //./Fonterra/ => ./rd1/
                //./IAG/ => ./iag/
                //./MSD/ => ./msd/
                var matchingBittorrentFile = MatchInBitTorrentFiles(bittorrentFiles, dropboxFile, bittorrentFilesByName);
                if (matchingBittorrentFile == null)
                {
                    filesInDropboxNotInBittorrentSync.Add(dropboxFile.path);
                    CopyFile(filesUniqueToDropbox, dropboxFile, "/Volumes/Public/FilesUniqueToDropbox");
                    //stringBuilderNotes.Add(String.Format("{0} \tdropbox only \tDate {1}\n", dropboxFile.path, dropboxFile.date));
                }
                else 
                {
                    var bittorrentDate = DateTime.ParseExact(dropboxFile.date, new[] { "d MMM H:mm:ss yyyy" }, null, DateTimeStyles.AllowWhiteSpaces);
                    var dropboxDate = DateTime.ParseExact(matchingBittorrentFile.date, "ddd MMM d H:mm:ss yyyy", null, DateTimeStyles.AllowWhiteSpaces);
                    var sizesMatch = matchingBittorrentFile.size == dropboxFile.size;

                    if (dropboxDate == bittorrentDate && sizesMatch)
                    {
                        //completely ignore these, they are the same file
                    }
                    else if (dropboxDate < DateTime.Now.AddMonths(-5) && dropboxDate != bittorrentDate)
                    {
                        //completely ignore these, the dropbox file was last edited before we started using bitsync
                    }
                    else if (bittorrentDate < DateTime.Now.AddMonths(-5) && dropboxDate > DateTime.Now.AddMonths(-2))
                    {
                        //completely ignore these, the copy on bitsync is wrong, the dropbox file will overrite it,
                    }
                    else if (IsVariableSizedFile(dropboxFile.path) && sizesMatch)
                    {
                        //completely ignore files that have a matchine size and one of these extensions ".jpg", ".png", ".otf", ".ttf", ".eps", ".ps", ".pdf"
                    }
                    else
                    {
                        if (dropboxDate > bittorrentDate)
                        {
                            //this is not OK, the dropbox file is newer
                            filesInDropboxWrongSizeAndNewer.Add(dropboxFile.path);
                            CopyFile(newerInDropboxFiles, dropboxFile, "NewerInDropboxFiles");
                            stringBuilderNotes.Add($"{dropboxFile.path} \tNewerInDropbox\tDropboxDate\t{dropboxFile.date}\tBitTorrentDate\t{matchingBittorrentFile.date}\t({(sizesMatch ? "Sizes match" : "Sizes differ")})\n");
                        }
                        else //if (dropboxDate < bittorrentDate)
                        {
                            //this is OK, the bittorrent file is newer.
                            filesInDropboxWrongSizeAndOlder.Add(dropboxFile.path);
                            if (sizesMatch)
                            {
                                CopyFile(sameSizedDropboxFilesForComparison, dropboxFile, "DropboxCompare");
                                CopyFile(sameSizedBitsyncFilesForComparison, matchingBittorrentFile, "BitsyncCompare");
                            }
                            else
                            {
                                CopyFile(obsoleteDropboxFiles, dropboxFile, "ObsoleteDropboxFiles");
                            }
                            //stringBuilderNotes.Add($"{dropboxFile.path} ObsoleteDropbox DropboxDate {dropboxFile.date} BitTorrentDate {matchingBittorrentFile.date} ({(sizesMatch ? "Sizes match" : "Sizes differ")})\n");
                        }
                    }
                }
            }
            //stringBuilderNotes.Add($"0.0 mkdir ../Archive\n");
            //foreach (var archive in ArchivePrefixes)
            //{
            //    if (stringBuilderNotes.All(a => a.IndexOf(archive, StringComparison.OrdinalIgnoreCase) < 0))
            //    {
            //        stringBuilderNotes.Add($"0.1 cp -r '{archive}' ../Archive\n");
            //        stringBuilderNotes.Add($"0.2 rm -rf '{archive}'\n");
            //        stringBuilderNotes.Add($"1. Ready To Archive: {archive}\n");
            //    }
            //}

            stringBuilderNotes.Sort();
            File.WriteAllText("C:/Users/micha/Desktop/synccompare.txt", string.Join("", stringBuilderNotes));

            File.WriteAllText("C:/Users/micha/Desktop/bitsyncscript.sh", filesUniqueToBitsync.ToString());
            //File.AppendAllText("C:/Users/micha/Desktop/bitsyncscript.sh", sameSizedBitsyncFilesForComparison.ToString());
            File.WriteAllText("C:/Users/micha/Desktop/dropboxscript.sh", newerInDropboxFiles.ToString());
            File.AppendAllText("C:/Users/micha/Desktop/dropboxscript.sh", obsoleteDropboxFiles.ToString());
            File.AppendAllText("C:/Users/micha/Desktop/dropboxscript.sh", sameSizedDropboxFilesForComparison.ToString());
            File.AppendAllText("C:/Users/micha/Desktop/dropboxscript.sh", sameSizedBitsyncFilesForComparison.ToString());
            File.WriteAllText("C:/Users/micha/Desktop/dropboxscript2.sh", filesUniqueToDropbox.ToString());
        }


        private static string GetRootPath(FileReference fileReference)
        {
            var slashPos = fileReference.path.IndexOf('/', 2);
            if (slashPos < 0)
            {
                return fileReference.path;
            }
            return fileReference.path.Substring(0, slashPos);
        }

        private static bool CopyToDropbox(FileReference value, List<String> dropboxRoots)
        {
            var rootPath = GetRootPath(value);
            var matchesExistingDropboxPath = dropboxRoots.Any(a=>a == rootPath);
            return CopyPrefixes.Any(a => a.StartsWith(rootPath)) && matchesExistingDropboxPath;
        }

        private static bool IsVariableSizedFile(string path)
        {
            //note: linux paths include characters that cannot existing in windows, so the standard .net path methods dont work.
            var dot = path.LastIndexOf('.');
            if (dot < 0 || dot>=(path.Length-1))
            {
                return true;
            }
            var extension = path.Substring(dot, path.Length - dot);
            //return new[] { ".jpg", ".png", ".psd", ".otf", ".ttf", ".eps", ".sketch", ".ps", ".pdf" }.Any(a => a == extension);
            return new[] { ".jpg", ".png", ".otf", ".ttf", ".eps", ".ps", ".pdf", ".sketch", ".ai" }.Any(a => a == extension);
        }

        private static void CopyFile(StringBuilder copyBitSyncFilesToHighlightTheAdditionalFilesThatWillBeAddedToDropbox,
            FileReference file, string targetFolder)
        {
            copyBitSyncFilesToHighlightTheAdditionalFilesThatWillBeAddedToDropbox.AppendFormat(
                "mkdir -p '{0}/{1}'\n", targetFolder.Replace("'","''"), Dir(file).Replace("'", "''"));
            copyBitSyncFilesToHighlightTheAdditionalFilesThatWillBeAddedToDropbox.AppendFormat("cp '{1}' '{0}/{1}'\n",
                targetFolder.Replace("'", "''"), file.path.Replace("'", "''"));
        }

        private static string Dir(FileReference bittorrentFile)
        {
            var slash = bittorrentFile.path.LastIndexOf('/');
            var dir = bittorrentFile.path.Substring(0, slash);
            return dir;
        }


        public class FileReference
        {
            public String size;
            public String date;
            public String path;
        }


        private static String GetFileSearchkey(FileReference fileReference)
        {
            try
            {
                return $"{Path.GetFileName(fileReference.path)}::{fileReference.size}";
            }
            catch (Exception)
            {
                return $"{fileReference.path}::{fileReference.size}";
            }
        }

        private static bool MatchInDropBox(Dictionary<string, FileReference> dropboxDictionary, FileReference bittorrentFile)
        {
            var path = bittorrentFile.path.Replace("./TargetName/", "./SourceName/").Replace("./TargetName2/", "./SourceName2/").ToLower();
            if (path.StartsWith("./rd1/"))
            {
                return true;
            }
            return dropboxDictionary.ContainsKey(path);
        }

        private static FileReference MatchInBitTorrentFiles(Dictionary<string, FileReference> bittorrentFiles, FileReference dropboxFile, ILookup<string, FileReference> bittorrentFilesByName)
        {
            var path = dropboxFile.path.Replace("./SourceName/", "./TargetName/").Replace("./SourceName2/", "./TargetName2/").ToLower();

            FileReference matchingBittorrentFile;
            if (bittorrentFiles.TryGetValue(path, out matchingBittorrentFile))
            {
                return matchingBittorrentFile;
            }
            matchingBittorrentFile = bittorrentFilesByName[GetFileSearchkey(dropboxFile)].FirstOrDefault();
            if (matchingBittorrentFile == null)
            {

            }
            else
            {

            }

            return matchingBittorrentFile;
        }

        private static bool IsArchive(FileReference file)
        {
            return ArchivePrefixes.Any(a => file.path.StartsWith(a));
        }
    }
}
