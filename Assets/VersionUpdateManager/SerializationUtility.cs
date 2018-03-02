using System.IO;

namespace VersionUpdate {
    public static class SerializationUtility {
        public static void Serialize(string filename, object graph) {
            using (Stream stream = File.Open(filename, FileMode.OpenOrCreate)) {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(stream, graph);
            }
        }

        public static object Deserialize(byte[] bytes) {
            using (Stream stream = new MemoryStream(bytes)) {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                return binaryFormatter.Deserialize(stream);
            }
        }
    }
}