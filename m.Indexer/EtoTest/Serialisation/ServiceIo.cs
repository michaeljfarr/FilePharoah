using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Newtonsoft.Json;

namespace EtoTest.Serialisation
{
    public static class ServiceIo
    {
        public static void WcfBinaryCompressedSerialize<T>(Stream destStream, T objectGraph)
        {
            using (var outStream = new System.IO.Compression.GZipStream(destStream, System.IO.Compression.CompressionMode.Compress, true))
            {
                var writer = XmlDictionaryWriter.CreateBinaryWriter(outStream);
                DataContractSerializer dataContractSerializer = new DataContractSerializer(typeof(T));
                dataContractSerializer.WriteObject(writer, objectGraph);
                writer.Flush();
            }
        }

        public static T WcfBinaryCompressedDeserialize<T>(Stream sourceStream)
        {
            using (var inStream = new System.IO.Compression.GZipStream(sourceStream, System.IO.Compression.CompressionMode.Decompress))
            {
                //using (var stringReader = new StreamReader(inStream, Encoding.UTF8))
                //{
                //    var stringValue = stringReader.ReadToEnd();
                //}

                var reader = XmlDictionaryReader.CreateBinaryReader(inStream, XmlDictionaryReaderQuotas.Max);
                DataContractSerializer dataContractSerializer = new DataContractSerializer(typeof(T));
                return (T)dataContractSerializer.ReadObject(reader);
            }
        }
    }
}