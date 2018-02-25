using System.IO;
using UnityEngine;

namespace UnityEngine.Networking {

    public class DownloadHandlerFile : DownloadHandlerScript, System.IDisposable {
        public int contentLength { get; protected set; }
        public int downloadedBytes { get; protected set; }
        private FileStream fileStream;

        ~DownloadHandlerFile() {
            CloseStream();
        }

        public DownloadHandlerFile(string localFilename, int bufferSize = 4096) : base(new byte[bufferSize]) {
            if (File.Exists(localFilename)) {
                fileStream = new FileStream(localFilename, FileMode.Append, FileAccess.Write, FileShare.Write);
                downloadedBytes = (int) fileStream.Length;
            } else {
                string localPath = Path.GetDirectoryName(localFilename);
                if (!Directory.Exists(localPath)) Directory.CreateDirectory(localPath);
                fileStream = new FileStream(localFilename, FileMode.Create);
            }
        }

        protected override float GetProgress() {
            return contentLength <= 0 ? -1 : (float) ((double) downloadedBytes / contentLength);
        }

        protected override void ReceiveContentLength(int contentLength) {
            this.contentLength = downloadedBytes + contentLength;
        }

        protected override bool ReceiveData(byte[] data, int dataLength) {
            if (data == null || data.Length == 0 || contentLength <= 0) return false;

            downloadedBytes += dataLength;
            fileStream.Write(data, 0, dataLength);
            return true;
        }

        protected override void CompleteContent() {
            CloseStream();
        }

        public new void Dispose() {
            CloseStream();
            base.Dispose();
        }

        private void CloseStream() {
            if (fileStream != null) {
                fileStream.Close();
                fileStream = null;
            }
        }
    }
}