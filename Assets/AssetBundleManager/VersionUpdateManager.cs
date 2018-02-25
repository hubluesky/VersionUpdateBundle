using System.IO;
using AssetBundles;
using UnityEngine;

public sealed class VersionUpdateManager {
	public static string VersionPath = "VersionUpdate.asset";
	private static DownloadUpdateTask downloadTask;

	public static void Initialize() {
		AssetBundleManager.Initialize();
	}

	public static GameObject LoadBackgroundAsset(string assetName) {
		GameObject backgroundPrefab = AssetBundleManager.Instance.LoadAsset<GameObject>(assetName);
		return GameObject.Instantiate(backgroundPrefab);
	}

	public static System.Collections.IEnumerator CheckUpdate(MonoBehaviour monoBehaviour) {
		AssetAsyncTask task = AssetBundleManager.Instance.LoadAssetAsync<VersionUpdateData>(VersionPath);
		yield return task;
		VersionUpdateData versionData = task.GetAsset<VersionUpdateData>();
		if (versionData == null) {
			Debug.LogWarning("Load version data failed.");
			yield break;
		}

		using(downloadTask = new DownloadUpdateTask(monoBehaviour, versionData.versionUrl, versionData.versionNumber, Application.temporaryCachePath, AssetBundleManager.GetExternalPath())) {
			yield return downloadTask;
			if (downloadTask.updateVersionCount > 0 && string.IsNullOrEmpty(downloadTask.error)) {
				AssetBundleManager.Instance.UnloadAllAssetBundle();
				AssetBundleManager.Instance.LoadedManifestAssetBundle();
			}
		}
	}

	public DownloadUpdateTask.TaskType taskType { get { return downloadTask.taskType; } }
}