using System;
using System.IO;
using System.Linq;

namespace m.Indexer
{
    public class NodeHeader
    {
        public NodeType NodeType { get; set; }
        public String FullDirectoryPath { get; set; }
        public String FileName { get; set; }
        public long? FileNumBytes { get; set; }
        public DateTime LastModifieDateTime { get; set; }
        public String PathOffset { get; set; }
        public String CloneUrl { get; set; }
        public long? CRC64 { get; set; }

    }
}