using System.IO;
using UnityEngine;

namespace VersionUpdate {
    public sealed class PlatformUtility {
        public static readonly string EditorWindowsAssetsPath = "VersionUpdate/StandaloneWindows";
        public static readonly string EditorStreamingAssetsPath = EditorWindowsAssetsPath + "/AssetBundles";
        public static readonly string ExternalPath = "ExternalPath";

        public static string GetExternalPath() {
            switch (Application.platform) {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return Path.Combine(System.Environment.CurrentDirectory, ExternalPath);
                case RuntimePlatform.Android:
                case RuntimePlatform.IPhonePlayer:
                    return Path.Combine(Application.persistentDataPath, ExternalPath);
                default:
                    return Path.Combine(Application.persistentDataPath, ExternalPath);
            }
        }

        public static string GetStreamingAssetsPath() {
            if (Application.isEditor)
                return Path.Combine(System.Environment.CurrentDirectory, EditorStreamingAssetsPath);
            else if (Application.isWebPlayer)
                return Application.streamingAssetsPath;
            else if (Application.isMobilePlatform || Application.isConsolePlatform)
                return Application.streamingAssetsPath;
            else // For standalone player.
                return Application.streamingAssetsPath;
        }

        public static string GetPlatformName() {
#if UNITY_EDITOR
            switch (UnityEditor.EditorUserBuildSettings.activeBuildTarget) {
                case UnityEditor.BuildTarget.Android:
                    return "Android";
                case UnityEditor.BuildTarget.iOS:
                    return "IOS";
                case UnityEditor.BuildTarget.WebGL:
                    return "WebGL";
                case UnityEditor.BuildTarget.StandaloneWindows:
                case UnityEditor.BuildTarget.StandaloneWindows64:
                    return "StandaloneWindows";
                    // Add more build targets for your own.
                    // If you add more targets, don't forget to add the same platforms to GetPlatformForAssetBundles(RuntimePlatform) function.
                default:
                    return null;
            }
#else
            switch (Application.platform) {
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.IPhonePlayer:
                    return "IOS";
                case RuntimePlatform.WindowsPlayer:
                    return "StandaloneWindows";
                    // Add more build targets for your own.
                    // If you add more targets, don't forget to add the same platforms to GetPlatformForAssetBundles(RuntimePlatform) function.
                default:
                    return null;
            }
#endif
        }
    }
}