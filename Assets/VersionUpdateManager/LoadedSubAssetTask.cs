using System.Collections;
using UnityEngine;

namespace VersionUpdate {
    public class LoadedSubAssetTask : AssetAsyncTask {
        private AssetBundleLoaded bundleLoaded;
        private AssetBundleRequest request;
        private string assetName;
        private string subAssetName;
        private bool isCompleted;

        public override float progress {
            get {
                if (bundleLoaded == null) return 0.0f;
                if (request == null) return (bundleLoaded.progress + 1) * 0.5f;
                return (request.progress + 1) * 0.5f;
            }
        }

        public LoadedSubAssetTask(AssetBundleManager assetBundleManager, string assetBundleName, string assetName, string subAssetName, System.Type type) {
            assetBundleManager.StartCoroutine(LoadedSubAsset(assetBundleManager, assetBundleName, assetName, subAssetName, type));
        }

        public IEnumerator LoadedSubAsset(AssetBundleManager assetBundleManager, string assetBundleName, string assetName, string subAssetName, System.Type type) {
            assetBundleName = assetBundleManager.RemapVariantName(assetBundleName);
            bundleLoaded = assetBundleManager.LoadAssetBundleAsync(assetBundleName);
            yield return bundleLoaded;
            if (bundleLoaded.assetBundle != null) {
                request = bundleLoaded.assetBundle.LoadAssetWithSubAssetsAsync(assetName, type);
                this.assetName = assetName;
                this.subAssetName = subAssetName;
            }
            isCompleted = true;
        }

        public override T GetAsset<T>() {
            if (request == null) return null;

            if (request.allAssets.Length == 0) {
                Debug.LogError("Load asset failed: " + assetName);
                return null;
            } else {
                foreach (Object asset in request.allAssets) {
                    if (asset.name == subAssetName)
                        return asset as T;
                }

                Debug.LogError("Load sub asset failed: " + assetName + "." + subAssetName);
                return null;
            }
        }

        public override bool IsDone() {
            return isCompleted && (request != null && request.isDone);
        }
    }
}