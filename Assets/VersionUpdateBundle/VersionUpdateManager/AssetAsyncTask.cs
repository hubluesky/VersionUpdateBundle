using System.Collections;
using UnityEngine;

namespace VersionUpdate {
    public abstract class AssetAsyncTask : IEnumerator {
        public object Current { get { return null; } }
        public bool MoveNext() {
            return !IsDone();
        }
        public void Reset() {
        }
        public abstract bool IsDone();
        public abstract T GetAsset<T>() where T : UnityEngine.Object;
        public abstract float progress { get; }
    }
}