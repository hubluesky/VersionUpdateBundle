using System.Collections.Generic;
using UnityEngine;

namespace VersionUpdate {
    public class AssetBundleLoaded : CustomYieldInstruction {
        private int dependencyLoadedCount { get; set; }
        private List<AssetBundleLoaded> mainAssetBundleList = new List<AssetBundleLoaded>();
        public AssetBundle assetBundle { get; private set; }
        public int referencedCount { get; set; }
        internal string[] dependencies { get; set; }
        private bool selfLoadedCompleted;
        public override bool keepWaiting { get { return dependencyLoadedCount != dependencies.Length; } }
        public float progress { get { return (dependencyLoadedCount + 1) / (dependencies.Length + 1); } }

        public AssetBundleLoaded(string[] dependencies) {
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