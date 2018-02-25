using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace AssetBundles {
    public class DownloadUpdateTask : AssetAsyncTask, System.IDisposable {
        public enum TaskType {
            Check,
            Download,
            Decompression,
            Install,
            Completed,
        }
        private List<Version> versionVersionList = new List<Version>();
        private UnityWebRequest wwwCheck;
        private UnityWebRequest wwwDownload;
        private Thread decompressionThread;
        private float downProgress;
        private float fileUnitProgress;
        private float decompressionProgress;
        public TaskType taskType { get; private set; }
        public string error { get; private set; }
        public int updateVersionCount { get { return versionVersionList.Count; } }
        public int maxVersionNumber { get; private set; }
        public string installFilePath { get; private set; }
        public override float progress {
            get {
                switch (taskType) {
                    case TaskType.Check:
                        return wwwCheck != null ? wwwCheck.downloadProgress : 0;
                    case TaskType.Download:
                        return wwwDownload != null ? (downProgress + wwwDownload.downloadProgress * fileUnitProgress) : 0;
                    case TaskType.Decompression:
                        return downProgress + decompressionProgress * fileUnitProgress;
                    default:
                        return 1.0f;
                }
            }
        }

        public override T GetAsset<T>() { return null; }

        public override bool IsDone() { return taskType == TaskType.Completed; }

        public DownloadUpdateTask(MonoBehaviour monoBehaviour, string url, int versionNumber, string temporaryPath, string downloadPath) {
            monoBehaviour.StartCoroutine(CheckAssetBundlesDownload(url, versionNumber, temporaryPath, downloadPath));
        }

        static T FromJson<T>(string json) {
            try {
                return JsonUtility.FromJson<T>(json);
            } catch (System.Exception) {
                return default(T);
            }
        }

        public IEnumerator CheckAssetBundlesDownload(string url, int versionNumber, string temporaryPath, string downloadPath) {
            taskType = TaskType.Check;
            string filename = Path.Combine(url, typeof(VersionUpdateTable).Name + ".json");
            using(wwwCheck = UnityWebRequest.Get(filename)) {
                yield return wwwCheck.Send();
                if (wwwCheck.isError) {
                    error = wwwCheck.error;
                    Debug.LogWarning(error);
                } else {
                    if (wwwCheck.downloadedBytes == 0) {
                        error = "Download file failed: " + filename;
                    } else {
                        VersionUpdateTable versionTable = FromJson<VersionUpdateTable>(wwwCheck.downloadHandler.text);
                        if (versionTable != null) {
                            foreach (Version version in versionTable.versions) {
                                maxVersionNumber = Math.Max(maxVersionNumber, version.versionNumber);
                                if (version.versionNumber > versionNumber)
                                    versionVersionList.Add(version);
                            }
                            versionVersionList.Sort((x, y) => { return x.versionNumber - y.versionNumber; });
                            taskType = TaskType.Download;
                            yield return DownloadVersionUpdates(url, downloadPath);
                        } else {
                            error = "VersionUpdateTable null";
                        }
                    }
                }
            }

            wwwCheck = null;
            if (error != null) {
                taskType = TaskType.Completed;
            } else {
                installFilePath = GetInstallFile(downloadPath);
                if (string.IsNullOrEmpty(installFilePath)) {
                    taskType = TaskType.Decompression;
                    decompressionThread = new Thread(() => {
                        DecompressionFiles(temporaryPath, downloadPath);
                        taskType = TaskType.Completed;
                    });
                    decompressionThread.IsBackground = true;
                    decompressionThread.Start();
                } else {
                    taskType = TaskType.Install;
                }
            }
        }

        public IEnumerator DownloadVersionUpdates(string url, string downloadPath) {
            float unitProgress = 1.0f / versionVersionList.Count;
            for (int v = 0; v < versionVersionList.Count; v++) {
                Version version = versionVersionList[v];
                for (int d = 0; d < version.removeList.Length; d++) {
                   File.Delete(Path.Combine(downloadPath, version.removeList[d]));
                }
                fileUnitProgress = 1.0f / version.packageList.Length;
                for (int u = 0; u < version.packageList.Length; u++) {
                    downProgress = (float) v / versionVersionList.Count + (float) u / version.packageList.Length * unitProgress;
                    yield return DownloadAssetBundleZip(url, version.packageList[u], downloadPath);
                }
            }
        }

        public IEnumerator DownloadAssetBundleZip(string url, string filename, string localPath) {
            using(wwwDownload = UnityWebRequest.Get(Path.Combine(url, filename))) {
                string localFilename = Path.Combine(localPath, filename);
                var downloadHandlerFile = new DownloadHandlerFile(localFilename);
                wwwDownload.chunkedTransfer = true;
                wwwDownload.disposeDownloadHandlerOnDispose = true;
                wwwDownload.SetRequestHeader("Range", "bytes=" + downloadHandlerFile.downloadedBytes + "-");
                wwwDownload.downloadHandler = downloadHandlerFile;
                yield return wwwDownload.Send();
                if (wwwDownload.isError) {
                    error = wwwDownload.error;
                    Debug.LogWarning(wwwDownload.error);
                }
            }
            wwwDownload = null;
        }

        public string GetInstallFile(string downloadPath) {
            for (int v = 0; v < versionVersionList.Count; v++) {
                Version version = versionVersionList[v];
                for (int u = 0; u < version.packageList.Length; u++) {
                    string zipName = Path.Combine(downloadPath, version.packageList[u]);
                    if (zipName.ToLower().EndsWith("apk"))
                        return zipName;
                }
            }
            return null;
        }

        public void DecompressionFiles(string temporaryPath, string downloadPath) {
            float versionUnitProgress = 1.0f / versionVersionList.Count;
            for (int v = 0; v < versionVersionList.Count; v++) {
                Version version = versionVersionList[v];
                fileUnitProgress = 1.0f / version.packageList.Length;
                for (int u = 0; u < version.packageList.Length; u++) {
                    downProgress = (float) v / versionVersionList.Count + (float) u / version.packageList.Length * versionUnitProgress;
                    string zipName = Path.Combine(downloadPath, version.packageList[u]);
                    error = ZipUtility.DecompressionFile(zipName, string.Empty, temporaryPath, downloadPath, (progress) => { decompressionProgress = progress; });
                    if (error != null) {
                        Debug.LogWarning(error);
                        return;
                    }
                    File.Delete(zipName);
                }
            }
        }

        public void Dispose() {
            if (wwwCheck != null)
                wwwCheck.Dispose();
            if (wwwDownload != null)
                wwwDownload.Dispose();
            if (decompressionThread != null && decompressionThread.IsAlive)
                decompressionThread.Abort();
            versionVersionList.Clear();
        }
    }
}