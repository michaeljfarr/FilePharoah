using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using m.Indexer;
using NUnit.Framework;

namespace m.Tests
{
    [TestFixture]
    public class PathEnumeratorTests
    {

        [Test]
        public void NullThrows()
        {
            Assert.That(() => new PathEnumerator(null, new String[0]).Index(), Throws.InstanceOf<ApplicationException>());
        }

        [Test]
        public void IndexThis()
        {

            var pathEnumerator = new PathEnumerator("C:\\", new[]
            {
                @":\\Windows",
                @":\\Program Files \(x86\)\\",
                @":\\Program Files\\",
                @":\\Users\\.*\\\..*",
                @":\\Users\\.*\\AppData\\Local.*\\",
                @":\\Users\\.*\\AppData\\Roaming\\",
                @":\\Users\\.*\\Searches",
                @"IISExpress\\TraceLogFiles",
                @"\\node_modules",
                @"\\wwwroot\\lib"
            });
            Assert.That(() => pathEnumerator.Index(), Throws.Nothing);
            var path = pathEnumerator.NodeEnvelopes.First().ToString();
        }

        [Test]
        public void CalculateStats()
        {
            var readValue = JsonStringHelper.ReadFromCompressedFile("envelopes.json.gz");
            var envelopes = JsonStringHelper.CreateObject<List<NodeEnvelope>>(readValue);
            var root = NodeHierarchyRoot.CreateNodeHierarchy(envelopes);
            root.CalculateStats();
        }


        [Test, Ignore("manual")]
        public void IndexPictures()
        {
            var pathEnumerator = new PathEnumerator(@"\\192.168.178.22\public", new[]
            {
                @":\\Windows",
                @":\\Program Files \(x86\)\\",
                @":\\Program Files\\",
                @":\\Users\\.*\\\..*",
                @":\\Users\\.*\\AppData\\Local.*\\",
                @":\\Users\\.*\\AppData\\Roaming\\",
                @":\\Users\\.*\\Searches",
                @"IISExpress\\TraceLogFiles",
                @"\\node_modules",
                @"\\wwwroot\\lib"
            });
            pathEnumerator.Index();

            var nodeEnvelopes = pathEnumerator.NodeEnvelopes;
            var root = NodeHierarchyRoot.CreateNodeHierarchy(nodeEnvelopes);
            var envelopes = JsonStringHelper.CreateString(pathEnumerator.NodeEnvelopes);
            JsonStringHelper.WriteToCompressedFile("envelopes.json.gz", envelopes);


        }

    }
}
