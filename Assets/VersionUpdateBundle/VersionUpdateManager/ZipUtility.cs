using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace VersionUpdate {
    public static class ZipUtility {

        public static string DecompressionFile(string archiveFilenameIn, string password, string outTemporaryPath, string outPath, System.Action<float> progressCallback) {
            ZipFile zipFile = null;
            string error = null;
            try {
                FileStream fs = File.OpenRead(archiveFilenameIn);
                zipFile = new ZipFile(fs);
                if (!string.IsNullOrEmpty(password)) {
                    zipFile.Password = password;     // AES encrypted entries are handled automatically
                }
                int readSize;
                byte[] buffer = new byte[4096];     // 4K is optimum
                float fileUnitProgress = 1.0f / zipFile.Count;
                for (int i = 0; i < zipFile.Count; i++) {
                    ZipEntry zipEntry = zipFile[i];
                    if (!zipEntry.IsFile)
                        continue;           // Ignore directories

                    Stream zipStream = zipFile.GetInputStream(zipEntry);

                    string filename = Path.Combine(outTemporaryPath, zipEntry.Name);
                    string directoryName = Path.GetDirectoryName(filename);
                    if (!Directory.Exists(directoryName))
                        Directory.CreateDirectory(directoryName);

                    using (FileStream streamWriter = File.Create(filename)) {
                        int readBytes = 0;
                        while ((readSize = zipStream.Read(buffer, 0, buffer.Length)) > 0) {
                            streamWriter.Write(buffer, 0, buffer.Length);
                            readBytes += readSize;
                            progressCallback(((float)readBytes / zipEntry.Size + i) * fileUnitProgress);
                        }
                    }
                }

                for (int i = 0; i < zipFile.Count; i++) {
                    ZipEntry zipEntry = zipFile[i];
                    if (!zipEntry.IsFile)
                        continue;           // Ignore directories
                    string temporaryFile = Path.Combine(outTemporaryPath, zipEntry.Name);
                    string outFile = Path.Combine(outPath, zipEntry.Name);
                    string directoryName = Path.GetDirectoryName(outFile);
                    if (!Directory.Exists(directoryName))
                        Directory.CreateDirectory(directoryName);

                    File.Copy(temporaryFile, outFile, true);
                    File.Delete(temporaryFile);
                }
            } catch (System.Exception exception) {
                error = exception.Message;
            } finally {
                if (zipFile != null) {
                    zipFile.IsStreamOwner = true; // Makes close also shut the underlying stream
                    zipFile.Close(); // Ensure we release resources
                }
            }
            return error;
        }


        // Compresses the files in the nominated folder, and creates a zip file on disk named as outPathname.
        //
        public static void CompressesFileList(string rootPath, string[] files, string password, string outFilename) {
            ZipOutputStream zipStream = new ZipOutputStream(File.Create(outFilename));
            zipStream.SetLevel(3); //0-9, 9 being the highest level of compression
            zipStream.Password = password;  // optional. Null is the same as not setting. Required if using AES.

            foreach (string filename in files) {
                string filePath = Path.Combine(rootPath, filename);
                FileInfo fi = new FileInfo(filePath);
                string entryName = ZipEntry.CleanName(filename); // Removes drive from name and fixes slash direction
                ZipEntry newEntry = new ZipEntry(entryName);
                newEntry.DateTime = fi.LastWriteTime; // Note the zip format stores 2 second granularity

                // Specifying the AESKeySize triggers AES encryption. Allowable values are 0 (off), 128 or 256.
                // A password on the ZipOutputStream is required if using AES.
                //   newEntry.AESKeySize = 256;

                // To permit the zip to be unpacked by built-in extractor in WinXP and Server2003, WinZip 8, Java, and other older code,
                // you need to do one of the following: Specify UseZip64.Off, or set the Size.
                // If the file may be bigger than 4GB, or you do not need WinXP built-in compatibility, you do not need either,
                // but the zip will be in Zip64 format which not all utilities can understand.
                //   zipStream.UseZip64 = UseZip64.Off;
                newEntry.Size = fi.Length;
                zipStream.PutNextEntry(newEntry);

                // Zip the file in buffered chunks
                // the "using" will close the stream even if an exception occurs
                byte[] buffer = new byte[4096];
                using (FileStream streamReader = File.OpenRead(filePath)) {
                    StreamUtils.Copy(streamReader, zipStream, buffer);
                }
                zipStream.CloseEntry();
            }

            zipStream.IsStreamOwner = true; // Makes the Close also Close the underlying stream
            zipStream.Close();
        }
    }
}