using System.Collections.Generic;
using UnityEngine;

namespace AssetBundles {
    [System.Serializable]
    public sealed class BundleManifest : ScriptableObject, ISerializationCallbackReceiver {
        private static readonly string[] EMPTY_STRING_ARRAY = new string[0];

        [System.Serializable]
        class BundleData {
            [SerializeField]
            public string bundleName;
            [SerializeField]
            public string[] allDependencies;
        }

        [SerializeField]
        private List<BundleData> bundleList = new List<BundleData>();
        [SerializeField]
        private List<string> assetPathList = new List<string>();
        [SerializeField]
        private List<string> bundlePathList = new List<string>();

        private string[] allAssetBundles;
        private Dictionary<string, string[]> bundleDependenciesMap;
        private Dictionary<string, string> bundlePathMap;

        public string[] GetAllAssetBundles() {
            if (allAssetBundles == null) {
                allAssetBundles = new string[bundleList.Count];
                for (int i = 0; i < allAssetBundles.Length; i++)
                    allAssetBundles[i] = bundleList[i].bundleName;
            }
            return allAssetBundles;
        }

        public string[] GetAllAssetBundlesWithVariant() {
            return EMPTY_STRING_ARRAY;
        }

        public string[] GetAllDependencies(string assetBundleName) {
            string[] dependencies;
            if (bundleDependenciesMap.TryGetValue(assetBundleName, out dependencies))
                return dependencies;
            return EMPTY_STRING_ARRAY;
        }

        public void ClearBundleData() {
            bundleList.Clear();
            assetPathList.Clear();
            bundlePathList.Clear();
        }

        public void AddBundleData(string bundleName, string[] allDependencies) {
            bundleList.Add(new BundleData() {
                bundleName = bundleName,
                allDependencies = allDependencies,
            });
        }

        public void AddBundleAssetPath(string assetPath, string bundleName) {
            assetPathList.Add(assetPath);
            bundlePathList.Add(bundleName);
        }

        public bool GetBundleNameByAssetPath(string assetPath, out string bundleName) {
            return bundlePathMap.TryGetValue(assetPath, out bundleName);
        }

        public void OnBeforeSerialize() {
        }

        public void OnAfterDeserialize() {
            bundlePathMap = new Dictionary<string, string>();
            bundleDependenciesMap = new Dictionary<string, string[]>();

            for (int i = 0; i < assetPathList.Count; i++)
                bundlePathMap.Add(assetPathList[i], bundlePathList[i]);

            for (int i = 0; i < bundleList.Count; i++)
                bundleDependenciesMap.Add(bundleList[i].bundleName, bundleList[i].allDependencies);
        }
    }
}