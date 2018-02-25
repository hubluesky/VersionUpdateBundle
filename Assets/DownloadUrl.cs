using AssetBundles;
using System.IO;
using UnityEngine;

namespace Kola {
    [System.Serializable]
    public class DownloadUrl {
        [SerializeField]
        protected int version;
        [SerializeField]
        protected string androidUrl;
        [SerializeField]
        protected string IOSUrl;
        [SerializeField]
        protected string standaloneWindowsUrl;

        public int versionNumber { get { return version; } set { version = value; } }
        public string url {
            get {
                switch (Application.platform) {
                    case RuntimePlatform.Android:
                        return androidUrl;
                    case RuntimePlatform.IPhonePlayer:
                        return IOSUrl;
                    case RuntimePlatform.WindowsPlayer:
                        return standaloneWindowsUrl;
                    default:
                        return standaloneWindowsUrl;
                }
            }
        }

        public static DownloadUrl LoadExternal(string relativePath) {
            string externalPath = AssetBundleManager.GetExternalPath() + relativePath;
            if (File.Exists(externalPath))
                return JsonUtility.FromJson<DownloadUrl>(File.ReadAllText(externalPath));
            return null;
        }

        public static DownloadUrl LoadInternal(string relativePath) {
            TextAsset textAsset = AssetBundleManager.Instance.LoadAsset(relativePath, typeof(TextAsset)) as TextAsset;
            if (null == textAsset)
                return null;
            return JsonUtility.FromJson<DownloadUrl>(textAsset.text);
        }

        public static DownloadUrl Load(string relativePath) {
            DownloadUrl ret = LoadExternal(relativePath);
            if (ret == null)
                ret = LoadInternal(relativePath);
            return ret;
        }

        public static void Save(DownloadUrl url, string relativePath) {
            string absPath = AssetBundleManager.GetExternalPath() + relativePath;
            if (File.Exists(absPath))
                File.Delete(absPath);
            string txt = JsonUtility.ToJson(url);
            FileInfo info = new FileInfo(absPath);
            if (!info.Directory.Exists)
                info.Directory.Create();
            // File.WriteAllBytes(absPath, Util.StringToBytes(txt));
        }
    }
}