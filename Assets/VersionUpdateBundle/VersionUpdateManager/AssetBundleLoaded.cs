using System.Collections.Generic;
using UnityEngine;

namespace VersionUpdate {
    public class AssetBundleLoaded : CustomYieldInstruction {
        private int dependencyLoadedCount { get; set; }
        private List<AssetBundleLoaded> mainAssetBundleList = new List<AssetBundleLoaded>();
        public AssetBundle assetBundle { get; private set; }
        public int referencedCount { get; set; }
        internal string bundleName { get; private set; }
        internal string[] dependencies { get; private set; }
        private bool selfLoadedCompleted;
        public override bool keepWaiting { get { return dependencyLoadedCount != dependencies.Length; } }
        public float progress { get { return AssetBundleManager.Instance.GetAssetBundleLoadProgress(bundleName, dependencies); } }

        public AssetBundleLoaded(string bundleName, string[] dependencies) {
            this.bundleName = bundleName;
            this.dependencies = dependencies;
            dependencyLoadedCount = -1;
        }

        public void AddDependencyAssetBundle(AssetBundleLoaded dependencyAssetBundle) {
            if (dependencyAssetBundle == null) return;

            mainAssetBundleList.Add(dependencyAssetBundle);
            if (selfLoadedCompleted)
                dependencyAssetBundle.AddLoadedDependency();
        }

        internal void SetLoadedAssetBundle(AssetBundle assetBundle) {
            this.assetBundle = assetBundle;
            AddLoadedDependency();
            foreach (AssetBundleLoaded dependencyAssetBundle in mainAssetBundleList)
                dependencyAssetBundle.AddLoadedDependency();
            selfLoadedCompleted = true;
        }

        private void AddLoadedDependency() {
            dependencyLoadedCount++;
        }
    }
}