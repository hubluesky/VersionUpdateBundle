using System.Collections;
using UnityEngine;

namespace AssetBundles {
    public class LoadedAssetTask : AssetAsyncTask {
        private AssetBundleLoaded bundleLoaded;
        private AssetBundleRequest request;
        private string assetName;
        private bool isCompleted;

        public override float progress {
            get {
                if (bundleLoaded == null) return 0.0f;
                if (request == null) return (bundleLoaded.progress + 1) * 0.5f;
                return (request.progress + 1) * 0.5f;
            }
        }

        public LoadedAssetTask(AssetBundleManager assetBundleManager, string assetBundleName, string assetName, System.Type type) {
            assetBundleManager.StartCoroutine(LoadedAsset(assetBundleManager, assetBundleName, assetName, type));
        }

        public IEnumerator LoadedAsset(AssetBundleManager assetBundleManager, string assetBundleName, string assetName, System.Type type) {
            assetBundleName = assetBundleManager.RemapVariantName(assetBundleName);
            bundleLoaded = assetBundleManager.LoadAssetBundleAsync(assetBundleName);
            yield return bundleLoaded;
            if (bundleLoaded.assetBundle != null) {
                request = bundleLoaded.assetBundle.LoadAssetAsync(assetName, type);
                this.assetName = assetName;
            }
            isCompleted = true;
        }

        public override T GetAsset<T>() {
            if (request == null) return null;
            if (request.asset == null) {
                Debug.LogError("Load asset failed: " + assetName);
            }
            return request.asset as T;
        }

        public override bool IsDone() {
            return isCompleted && (request != null && request.isDone);
        }
    }
}