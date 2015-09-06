using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;

namespace EtoTest.Serialisation
{

    public static class JsonStringHelper
    {
        public const int MaxJsonSize = 5 * 1024 * 1024;
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        public static String ToString(this Stream s, int maxJsonSize)
        {
            var byteArray = s.ToByteArray(maxJsonSize);
            if (byteArray == null || byteArray.Length == 0)
            {
                return null;
            }
            if (byteArray[0] != 31)
            {
                using (var ms = new MemoryStream(byteArray))
                using (var reader = new StreamReader(ms, Encoding.UTF8))
                {
                    var stringVal = reader.ReadToEnd();
                    return stringVal;
                }
            }
            else
            {
                using (var ms = new MemoryStream(byteArray))
                using (var gz = new GZipStream(ms, CompressionMode.Decompress))
                using (var reader = new StreamReader(gz, Encoding.UTF8))
                {
                    var stringVal = reader.ReadToEnd();
                    return stringVal;
                }
            }
        }


        private static byte[] ToByteArray(this Stream stream, int maxJsonSize)
        {
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                var bytes = reader.ReadBytes(maxJsonSize);
                if (bytes.Length >= maxJsonSize)
                {
                    throw new NotImplementedException(String.Format("MaxJsonSize of {0}B exceeded", maxJsonSize));
                }
                return bytes;
            }
        }

        public static T ReadJsonObject<T>(Stream s, int maxJsonSize)
        {
            //check if compressed
            var byteArray = s.ToByteArray(maxJsonSize);
            if (byteArray == null || byteArray.Length == 0)
            {
                return default(T);
            }
            if (byteArray[0] != 31)
            {
                using (var ms = new MemoryStream(byteArray))
                using (var reader = new StreamReader(ms, Encoding.UTF8))
                {
                    var stringValue = reader.ReadToEnd();
                    var readObject = JsonConvert.DeserializeObject<T>(stringValue, JsonSerializerSettings);
                    return readObject;
                }
            }
            else
            {
                using (var ms = new MemoryStream(byteArray))
                using (var gz = new GZipStream(ms, CompressionMode.Decompress))
                using (var reader = new StreamReader(gz, Encoding.UTF8))
                {
                    var stringValue = reader.ReadToEnd();
                    var readObject = JsonConvert.DeserializeObject<T>(stringValue, JsonSerializerSettings);
                    return readObject;
                }
            }
        }

        public static void WriteJsonObject<T>(Stream s, T val, bool compress)
        {
            if (!compress)
            {
                using (var writer = new StreamWriter(s, Encoding.UTF8, 4096, true))
                {
                    var stringValue = JsonConvert.SerializeObject(val, typeof(T), Formatting.Indented, JsonSerializerSettings);
                    writer.Write(stringValue);
                }
            }
            else
            {
                using (var gz = new GZipStream(s, CompressionMode.Compress, true))
                using (var writer = new StreamWriter(gz, Encoding.UTF8))
                {
                    var stringValue = JsonConvert.SerializeObject(val, typeof(T), Formatting.Indented, JsonSerializerSettings);
                    writer.Write(stringValue);
                }
            }
        }

    }
}
