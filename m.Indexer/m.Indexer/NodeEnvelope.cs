using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace m.Indexer
{
    public class NodeEnvelope
    {
        public NodeHeader Header { get; set; }

        public int NumDescendents { get; set; }

        public string GetFullPath()
        {
            var fullPath = Path.Combine(Header.FullDirectoryPath, Header.FileName);
            return fullPath;
        }

        public string GetClass()
        {
            if (Header.NodeType != NodeType.File)
            {
                return Header.NodeType.ToString();
            }
            var extension = Path.GetExtension(Header.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                return "Unknown";
            }
            else if (new[] { ".jpg", ".cr2", ".png", ".mp4", ".avi", ".mp3" }.Any(ext => string.Equals(extension, ext, StringComparison.OrdinalIgnoreCase)))
            {
                return "Media";
            }
            else if (new[] { ".cs", ".csproj", ".sln", ".config", ".manifest", ".xml" }.Any(ext => string.Equals(extension, ext, StringComparison.OrdinalIgnoreCase)))
            {
                return "Code";
            }
            else if (new[] { ".dll", ".exe", ".pdb" }.Any(ext => string.Equals(extension, ext, StringComparison.OrdinalIgnoreCase)))
            {
                return "Application";
            }
            else if (new[] { ".pdf", ".doc", ".docx", ".xlsx", ".xls", ".pptx", ".ppt", ".swf", ".htm" }.Any(ext => string.Equals(extension, ext, StringComparison.OrdinalIgnoreCase)))
            {
                return "Document";
            }
            return "Unknown";
        }


        public override string ToString()
        {
            return $"{Header.NodeType} ({NumDescendents}): {Header.FullDirectoryPath}{System.IO.Path.DirectorySeparatorChar}{Header.FileName} {(Header.FileNumBytes.HasValue ? Header.FileNumBytes + " bytes" : "")} {Header.CloneUrl})";
        }
    }
}