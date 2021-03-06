using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VersionUpdate {
    public class LoadedSceneTask : SceneAsyncTask {
        private AssetBundleLoaded bundleLoaded;

        public override float progress {
            get {
                if (bundleLoaded == null) return 0.0f;
                if (operation == null) return bundleLoaded.progress * 0.5f;
                return (bundleLoaded.progress + operation.progress) * 0.5f;
            }
        }

        public LoadedSceneTask(AssetBundleManager assetBundleManager, string assetBundleName, string sceneName, LoadSceneMode mode) {
            assetBundleManager.StartCoroutine(LoadedAsset(assetBundleManager, assetBundleName, sceneName, mode));
        }

        public IEnumerator LoadedAsset(AssetBundleManager assetBundleManager, string assetBundleName, string sceneName, LoadSceneMode mode) {
            assetBundleName = assetBundleManager.RemapVariantName(assetBundleName);
            bundleLoaded = assetBundleManager.LoadAssetBundleAsync(assetBundleName);
            yield return bundleLoaded;
            operation = SceneManager.LoadSceneAsync(sceneName, mode);
            operation.allowSceneActivation = !notActivatedScene;
        }

        public override T GetAsset<T>() {
            return null;
        }

        public override bool IsDone() {
            return operation != null ? operation.isDone : false;
        }
    }
}