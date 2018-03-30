﻿using System.IO;
using UnityEngine;

namespace VersionUpdate {
	public sealed class VersionUpdateManager {
		public static readonly string VersionPath = typeof(VersionUpdateData).Name + ".asset";
		private static DownloadUpdateTask downloadTask;
		private static VersionUpdateData versionData;

		public static void Initialize() {
			AssetBundleManager.Initialize();
			versionData = AssetBundleManager.Instance.LoadAsset<VersionUpdateData>(VersionPath);
		}

		public static GameObject LoadBackgroundAsset(string assetName, Transform parent = null) {
			GameObject backgroundPrefab = AssetBundleManager.Instance.LoadAsset<GameObject>(assetName);
			return GameObject.Instantiate(backgroundPrefab, parent, false);
		}

		public static System.Collections.IEnumerator CheckUpdate(MonoBehaviour monoBehaviour) {
			using(downloadTask = new DownloadUpdateTask(monoBehaviour, versionData.versionUrl, versionData.versionNumber, Application.temporaryCachePath, PlatformUtility.GetExternalPath())) {
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
		public static TaskType taskType { get { return downloadTask != null ? downloadTask.taskType : TaskType.Failure; } }
	}
}