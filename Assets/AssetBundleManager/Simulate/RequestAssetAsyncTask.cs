using UnityEngine;

namespace AssetBundles {
    public class RequestAssetAsyncTask : AssetAsyncTask {
        private AsyncOperation operation;

        public RequestAssetAsyncTask(AsyncOperation operation) {
            this.operation = operation;
        }

        public override float progress { get { return 1.0f; } }

        public override T GetAsset<T>() {
            return null;
        }

        public override bool IsDone() {
            return operation.isDone;
        }
    }
}