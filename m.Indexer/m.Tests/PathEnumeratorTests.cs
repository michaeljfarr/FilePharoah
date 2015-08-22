using System;
using System.Collections.Generic;
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
        public void Test()
        {
            Assert.That(()=> new PathEnumerator(null).IndexPath(), Throws.Nothing);
        }
    }
}
