using UnityEngine;

namespace AssetBundles {
    public class CacheAssetAsyncTask : AssetAsyncTask {
        private Object asset;

        public CacheAssetAsyncTask(Object asset) {
            this.asset = asset;
        }

        public override float progress { get { return 1.0f; } }

        public override T GetAsset<T>() {
            return asset as T;
        }

        public override bool IsDone() {
            return true;
        }
    }
}