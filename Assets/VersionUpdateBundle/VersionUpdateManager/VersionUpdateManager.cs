using System.IO;
using UnityEngine;

namespace VersionUpdate {
    public sealed class VersionUpdateManager {
        public static readonly string VersionUpdateDataName = typeof(VersionUpdateData).Name + ".asset";
        private static DownloadUpdateTask downloadTask;
        private static VersionUpdateData versionData;
        private static TaskType updateTaskType;

        public static void Initialize(string versionPath) {
            AssetBundleManager.Initialize();
            versionData = AssetBundleManager.Instance.LoadAsset<VersionUpdateData>(versionPath);
            updateTaskType = TaskType.Failure;
        }

        public static void CheckInstallationPackage(string versionPath) {
            updateTaskType = TaskType.CheckInstallationPackage;
            string bundleName;
            if (!AssetBundleManager.Instance.GetBundleName(versionPath, out bundleName))
                return;

            string bundlePath = Path.Combine(PlatformUtility.GetExternalPath(), bundleName);
            if (!File.Exists(bundlePath))
                return;

            AssetBundleManager.Instance.UnloadAssetBundle(bundleName);
            bundlePath = Path.Combine(PlatformUtility.GetStreamingAssetsPath(), bundleName);
            AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
                return;

            string assetName = Path.GetFileNameWithoutExtension(versionPath);
            VersionUpdateData packageVersionData = bundle.LoadAsset<VersionUpdateData>(assetName);
            bundle.Unload(false);
            if (packageVersionData == null || versionData.versionNumber >= packageVersionData.versionNumber)
                return;

            Debug.LogFormat("The external path version is {0}, package version is {1}, Delete the external path.", versionData.versionNumber, packageVersionData.versionNumber);
            AssetBundleManager.Instance.UnloadAllAssetBundle();
            Directory.Delete(PlatformUtility.GetExternalPath(), true);
            AssetBundleManager.Instance.LoadedManifestAssetBundle();
        }

        public static GameObject LoadBackgroundAsset(string assetName, Transform parent = null) {
            GameObject backgroundPrefab = AssetBundleManager.Instance.LoadAsset<GameObject>(assetName);
            return GameObject.Instantiate(backgroundPrefab, parent, false);
        }

        public static System.Collections.IEnumerator CheckUpdate(MonoBehaviour monoBehaviour) {
            using (downloadTask = new DownloadUpdateTask(monoBehaviour, versionData.versionUrl, versionData.versionNumber, Application.temporaryCachePath, PlatformUtility.GetExternalPath())) {
                yield return downloadTask;
                error = downloadTask.error;
                if (downloadTask.updateVersionCount > 0 && string.IsNullOrEmpty(downloadTask.error)) {
                    AssetBundleManager.Instance.UnloadAllAssetBundle();
                    AssetBundleManager.Instance.LoadedManifestAssetBundle();
                }
            }
        }

        public static int versionNumber { get { return versionData != null ? versionData.versionNumber : 0; } }
        public static string versionName { get { return versionData != null ? versionData.versionName : null; } }
        public static float progress { get { return downloadTask != null ? downloadTask.progress : 0; } }
        public static string error { get; private set; }
        public static TaskType taskType { get { return downloadTask != null ? downloadTask.taskType : updateTaskType; } }
    }
}