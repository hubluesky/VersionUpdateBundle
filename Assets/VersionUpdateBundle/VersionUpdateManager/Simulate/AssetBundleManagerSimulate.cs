using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VersionUpdate {
    public class AssetBundleManagerSimulate : AssetBundleManager {
        private const string simulateAssetBundlesText = "SimulateAssetBundles";
        private static int _simulateAssetBundle = -1;
        public static bool simulateAssetBundle {
            get {
#if UNITY_EDITOR
                if (_simulateAssetBundle == -1)
                    _simulateAssetBundle = EditorPrefs.GetBool(simulateAssetBundlesText, true) ? 1 : 0;
#endif
                return _simulateAssetBundle != 0;
            }
            set {
                int newValue = value ? 1 : 0;
                if (newValue != _simulateAssetBundle) {
                    _simulateAssetBundle = newValue;
#if UNITY_EDITOR
                    EditorPrefs.SetBool(simulateAssetBundlesText, value);
#endif
                }
            }
        }

#if UNITY_EDITOR
        public override bool GetBundleName(string assetName, out string bundleName) {
            if (!simulateAssetBundle)
                return base.GetBundleName(assetName, out bundleName);
            bundleName = assetName;
            return true;
        }

        public override Object LoadBundleAsset(string bundleName, string assetName, System.Type type) {
            if (!simulateAssetBundle)
                return base.LoadBundleAsset(bundleName, assetName, type);

            return AssetDatabase.LoadAssetAtPath(bundleName, type);
        }

        public override Object LoadBundleSubAsset(string bundleName, string assetName, string subAssetName, System.Type type) {
            if (!simulateAssetBundle)
                return base.LoadBundleSubAsset(bundleName, assetName, subAssetName, type);

            Object[] targets = AssetDatabase.LoadAllAssetsAtPath(bundleName);
            foreach (Object target in targets) {
                if (target.name == subAssetName) {
                    return target;
                }
            }
            return null;
        }

        public override bool LoadBundleScene(string bundleName, string sceneName, UnityEngine.SceneManagement.LoadSceneMode mode) {
            if (!simulateAssetBundle)
                return base.LoadBundleScene(bundleName, sceneName, mode);

            if (mode == LoadSceneMode.Additive)
                UnityEditor.EditorApplication.LoadLevelAdditiveInPlayMode(bundleName);
            else
                UnityEditor.EditorApplication.LoadLevelInPlayMode(bundleName);
            return true;
        }

        protected override AssetAsyncTask CreateLoadBundleAssetAsyncTask(string bundleName, string assetName, System.Type type) {
            if (!simulateAssetBundle)
                return base.CreateLoadBundleAssetAsyncTask(bundleName, assetName, type);

            return new CacheAssetAsyncTask(AssetDatabase.LoadAssetAtPath(bundleName, type));
        }

        protected override AssetAsyncTask CreateLoadBundleSubAssetAsyncTask(string bundleName, string assetName, string subAssetName, System.Type type) {
            if (!simulateAssetBundle)
                return base.CreateLoadBundleSubAssetAsyncTask(bundleName, assetName, subAssetName, type);

            Object[] targets = AssetDatabase.LoadAllAssetsAtPath(bundleName);
            foreach (Object target in targets) {
                if (target.name == subAssetName) {
                    return new CacheAssetAsyncTask(target);
                }
            }
            return new CacheAssetAsyncTask(null);
        }

        protected override AssetAsyncTask CreateLoadBundleSceneAsyncTask(string bundleName, string sceneName, LoadSceneMode mode) {
            if (!simulateAssetBundle)
                return base.CreateLoadBundleSceneAsyncTask(bundleName, sceneName, mode);

            if (mode == LoadSceneMode.Additive)
                return new RequestAssetAsyncTask(UnityEditor.EditorApplication.LoadLevelAdditiveAsyncInPlayMode(bundleName));
            else
                return new RequestAssetAsyncTask(UnityEditor.EditorApplication.LoadLevelAsyncInPlayMode(bundleName));
        }

        protected override AssetAsyncTask CreateLoadBundleMultiSceneAsyncTask(string bundleName, string sceneName) {
            if (!simulateAssetBundle)
                return base.CreateLoadBundleMultiSceneAsyncTask(bundleName, sceneName);

            return new RequestAssetAsyncTask(UnityEditor.EditorApplication.LoadLevelAdditiveAsyncInPlayMode(bundleName));
        }

        public override void UnloadAssetBundle(string bundleName, bool forceUnload) {
            if (!simulateAssetBundle)
                base.UnloadAssetBundle(bundleName, forceUnload);
        }
#endif
    }
}