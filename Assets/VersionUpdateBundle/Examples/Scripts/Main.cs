using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VersionUpdate;

public class Main : MonoBehaviour {
    public string versinoPath = "Assets/VersionUpdateBundle";
    public Canvas canvas;
    public Transform background;
    public Text loadedText;
    public Text versionText;
    public Slider progress;

    void Awake() {
        VersionUpdateManager.Initialize(versinoPath);

        VersionUpdateManager.LoadBackgroundAsset("Assets/VersionUpdateBundle/Examples/Background/Background.prefab", background);
        versionText.text = VersionUpdateManager.versionName;

        VersionUpdateManager.CheckInstallationPackage(versinoPath);
    }

    System.Collections.IEnumerator Start() {
        yield return VersionUpdateManager.CheckUpdate(this);
        if (VersionUpdateManager.taskType == TaskType.Completed || VersionUpdateManager.taskType == TaskType.Failure)
            yield return EnterMainScene();
    }

    System.Collections.IEnumerator EnterMainScene() {
        yield return AssetBundleManager.Instance.LoadSceneAsync("Assets/VersionUpdateBundle/Examples/BundleAssets/Tanks/Scenes/TanksExample.unity", LoadSceneMode.Single);
    }

    void Update() {
        loadedText.text = GetTaskString();
        progress.value = VersionUpdateManager.progress;
    }

    string GetTaskString() {
        if (VersionUpdateManager.error != null)
            return "下载失败! " + VersionUpdateManager.error;
        switch (VersionUpdateManager.taskType) {
            case TaskType.CheckInstallationPackage:
                return "正在安装包";
            case TaskType.CheckUpdate:
                return "正在检查更新";
            case TaskType.Download:
                return "正在下载更新";
            case TaskType.Decompression:
                return "正在解压更新包 (不消耗流量)";
            case TaskType.Completed:
                return "完成下载";
            case TaskType.Install:
                return "正在安装";
            case TaskType.Failure:
                return "更新失败";
            default:
                return null;
        }
    }
}