using AssetBundles;
using UnityEngine;
using UnityEngine.UI;

namespace Kola {
    public class VersionUpdate : MonoBehaviour {
        public const string VERSION_NUMBER_TEXT = "VersionNumber";
        public const string versionPath = "VersionUpdate/VersionUpdate.bytes";
        public Slider progressbar;
        public Text text;
        public bool startGameIgnoreError = true;

        DownloadUpdateTask downloadTask;
        DownloadUrl downloadUrl;

        void CheckCleanUpdateCache() {
            AssetBundleManager.Initialize();
            DownloadUrl exter = DownloadUrl.LoadExternal(versionPath);
            DownloadUrl inter = DownloadUrl.LoadInternal(versionPath);
            downloadUrl = exter ?? inter;
            if (exter != null && inter != null &&  inter.versionNumber >= exter.versionNumber) {
                Debug.Log("CheckCleanUpdateCache");
                // IOUtil.DeleteFolder(AssetManager.ExternalPath);
                // IOUtil.CreateDirectory(AssetManager.ExternalPath);
                AssetBundleManager.Instance.UnloadAllAssetBundle();
                AssetBundleManager.Instance.LoadedManifestAssetBundle();
            }
        }

        void Awake() {
            float t1 = Time.realtimeSinceStartup;
            CheckCleanUpdateCache();
            GameObject backgroundPrefab = AssetBundleManager.Instance.LoadAsset("VersionUpdate/Background.prefab", typeof(GameObject)) as GameObject;
            GameObject.Instantiate(backgroundPrefab);
            float t2 = Time.realtimeSinceStartup;
            Debug.LogFormat("Load update background : {0}", t2 - t1);
        }

        System.Collections.IEnumerator Start() {
            string error = null;
            Debug.Log("Download update url :" + downloadUrl.url);
            downloadTask = new DownloadUpdateTask(this, downloadUrl.url, downloadUrl.versionNumber, Application.temporaryCachePath, AssetBundleManager.GetExternalPath());
            yield return downloadTask;
            error = downloadTask.error;
            if (downloadTask.updateVersionCount > 0 && string.IsNullOrEmpty(error)) {
                if (!string.IsNullOrEmpty(downloadTask.installFilePath)) {
                    if (!InstallApp(downloadTask.installFilePath))
                        error = "安装失败!";
                }
                downloadUrl.versionNumber = System.Math.Max(downloadUrl.versionNumber, downloadTask.maxVersionNumber);
                DownloadUrl.Save(downloadUrl, versionPath);
                AssetBundleManager.Instance.UnloadAllAssetBundle();
                AssetBundleManager.Instance.LoadedManifestAssetBundle();
            }
            downloadTask.Dispose();

            if (string.IsNullOrEmpty(error) || startGameIgnoreError)
                EnterGame();
        }

        string GetTaskString(DownloadUpdateTask.TaskType taskType) {
            if (downloadTask.error != null)
                return "下载失败! " + downloadTask.error;
            switch (downloadTask.taskType) {
                case DownloadUpdateTask.TaskType.Check:
                    return "正在检查更新";
                case DownloadUpdateTask.TaskType.Download:
                    return "正在下载更新";
                case DownloadUpdateTask.TaskType.Decompression:
                    return "正在解压更新包 (不消耗流量)";
                case DownloadUpdateTask.TaskType.Completed:
                    return "完成下载";
                case DownloadUpdateTask.TaskType.Install:
                    return "正在安装";
                default:
                    return null;
            }
        }

        void Update() {
            if (downloadTask != null) {
                if (progressbar != null)
                    progressbar.value = downloadTask.progress;
                if (text != null)
                    text.text = GetTaskString(downloadTask.taskType);
            }
        }

        public void EnterGame() {
            AssetAsyncTask sceneTask = AssetBundleManager.Instance.LoadSceneAsync("Scenes/1_Main.unity", UnityEngine.SceneManagement.LoadSceneMode.Single);
            StartCoroutine(sceneTask);
        }



        public bool InstallApp(string path) {
            if (!InstallAPK_1(path))
                return InstallAPK_2(path);
            return true;
        }

        bool InstallAPK_1(string apkPath) {
            Debug.Log("InstallAPK_1:" + apkPath);
            try {
                var Intent = new AndroidJavaClass("android.content.Intent");
                var ACTION_VIEW = Intent.GetStatic<string>("ACTION_VIEW");
                var FLAG_ACTIVITY_NEW_TASK = Intent.GetStatic<int>("FLAG_ACTIVITY_NEW_TASK");
                var intent = new AndroidJavaObject("android.content.Intent", ACTION_VIEW);

                var file = new AndroidJavaObject("java.io.File", apkPath);
                var Uri = new AndroidJavaClass("android.net.Uri");
                var uri = Uri.CallStatic<AndroidJavaObject>("fromFile", file);

                intent.Call<AndroidJavaObject>("setDataAndType", uri, "application/vnd.android.package-archive");
                var UnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var currentActivity = UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                currentActivity.Call("startActivity", intent);

                return true;
            } catch (System.Exception e) {
                Debug.LogError(e.Message);
                return false;
            }
        }

        //For API 24 and above
        bool InstallAPK_2(string apkPath) {
            Debug.Log("InstallAPK_2:" + apkPath);
            bool success = true;
            try {
                //Get Activity then Context
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject unityContext = currentActivity.Call<AndroidJavaObject>("getApplicationContext");

                //Get the package Name
                string packageName = unityContext.Call<string>("getPackageName");
                string authority = packageName + ".fileprovider";

                AndroidJavaClass intentObj = new AndroidJavaClass("android.content.Intent");
                string ACTION_VIEW = intentObj.GetStatic<string>("ACTION_VIEW");
                AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", ACTION_VIEW);


                int FLAG_ACTIVITY_NEW_TASK = intentObj.GetStatic<int>("FLAG_ACTIVITY_NEW_TASK");
                int FLAG_GRANT_READ_URI_PERMISSION = intentObj.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION");

                //File fileObj = new File(String pathname);
                AndroidJavaObject fileObj = new AndroidJavaObject("java.io.File", apkPath);
                //FileProvider object that will be used to call it static function
                AndroidJavaClass fileProvider = new AndroidJavaClass("android.support.v4.content.FileProvider");
                //getUriForFile(Context context, String authority, File file)
                AndroidJavaObject uri = fileProvider.CallStatic<AndroidJavaObject>("getUriForFile", unityContext, authority, fileObj);

                intent.Call<AndroidJavaObject>("setDataAndType", uri, "application/vnd.android.package-archive");
                intent.Call<AndroidJavaObject>("addFlags", FLAG_ACTIVITY_NEW_TASK);
                intent.Call<AndroidJavaObject>("addFlags", FLAG_GRANT_READ_URI_PERMISSION);
                currentActivity.Call("startActivity", intent);
            } catch (System.Exception e) {
                Debug.LogError(e.Message);
                success = false;
            }

            return success;
        }
    }
}
