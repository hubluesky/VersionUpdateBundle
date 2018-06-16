using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VersionUpdate {
    public class LoadedMultiSceneTask : AssetAsyncTask {
        private AsyncOperation operation;
        private AssetBundleLoaded bundleLoaded;
        private int indexLoaded = -1;
        private int maxLoaded;
        public override float progress {
            get {
                if (indexLoaded == -1 && operation == null) {
                    if (bundleLoaded == null) return 0.0f;
                    return (bundleLoaded.progress + 1) * 0.5f;
                }
                float progress = indexLoaded / (float)maxLoaded;
                return progress + (operation.progress + 1) * 0.5f / (float)maxLoaded;
            }
        }

        public LoadedMultiSceneTask(AssetBundleManager assetBundleManager, string assetBundleName, string assetName) {
            assetBundleManager.StartCoroutine(LoadedAsset(assetBundleManager, assetBundleName, assetName));
        }

        public IEnumerator LoadedAsset(AssetBundleManager assetBundleManager, string assetBundleName, string assetName) {
            assetBundleName = assetBundleManager.RemapVariantName(assetBundleName);
            bundleLoaded = assetBundleManager.LoadAssetBundleAsync(assetBundleName);
            yield return bundleLoaded;

            AssetBundleRequest request = bundleLoaded.assetBundle.LoadAssetAsync<MultiSceneSetup>(assetName);
            yield return request;
            MultiSceneSetup multiSceneSetup = request.asset as MultiSceneSetup;
            if (multiSceneSetup != null) {
                maxLoaded = multiSceneSetup.scenePaths.Length;
                for (int i = 0; i < multiSceneSetup.scenePaths.Length; i++) {
                    indexLoaded = i;
                    string bundleName;
                    if (!assetBundleManager.GetBundleName(multiSceneSetup.scenePaths[i], out bundleName))
                        continue;
                    yield return LoadedAsset(assetBundleManager, bundleName, Path.GetFileNameWithoutExtension(multiSceneSetup.scenePaths[i]));
                }
            }
            indexLoaded = maxLoaded;
        }

        public IEnumerator LoadedAsset(AssetBundleManager assetBundleManager, string assetBundleName, string sceneName, LoadSceneMode mode) {
            assetBundleName = assetBundleManager.RemapVariantName(assetBundleName);
            bundleLoaded = assetBundleManager.LoadAssetBundleAsync(assetBundleName);
            yield return bundleLoaded;
            operation = SceneManager.LoadSceneAsync(sceneName, mode);
            yield return operation;
            operation = null;
        }

        public override T GetAsset<T>() {
            return null;
        }

        public override bool IsDone() {
            return indexLoaded == maxLoaded;
        }
    }
}