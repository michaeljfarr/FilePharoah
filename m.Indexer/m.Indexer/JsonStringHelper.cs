using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace m.Indexer
{
    public static class JsonStringHelper
    {
        public static readonly CultureInfo NzCultureInfo = CultureInfo.CreateSpecificCulture("en-NZ");
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            DateTimeZoneHandling = DateTimeZoneHandling.Local,
            Culture = NzCultureInfo
        };

        public static string CreateString<T>(T value)
        {
            var readObject = JsonConvert.SerializeObject((object)value, typeof(T), Formatting.None, JsonSerializerSettings);
            return readObject;
        }
        public static T CreateObject<T>(String value)
        {
            var readObject = JsonConvert.DeserializeObject<T>(value, JsonSerializerSettings);
            return readObject;
        }


        public static void WriteToCompressedFile(String fileTarget, String value)
        {
            using (var fs = File.Create(fileTarget, 1024 * 1024, FileOptions.None))
            using (var gz = new GZipStream(fs, CompressionMode.Compress))
            using (var ts = new StreamWriter(gz))
            {
                ts.Write(value);
                ts.Flush();
            }
        }
        public static String ReadFromCompressedFile(String fileTarget)
        {
            using (var fs = File.OpenRead(fileTarget))
            using (var gz = new GZipStream(fs, CompressionMode.Decompress))
            using (var ts = new StreamReader(gz))
            {
                return ts.ReadToEnd();
            }
        }
    }
}