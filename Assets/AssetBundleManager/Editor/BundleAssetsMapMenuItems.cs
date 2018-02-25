using System.Collections.Generic;
using System.IO;
using UnityEditor;
using AssetBundles;
using UnityEngine.AssetBundles.AssetBundleDataSource;

namespace UnityEngine.AssetBundles {
    public class BundleAssetsMapMenuItems {
        private static readonly string manifestName = typeof(BundleManifest).Name;
        private static readonly string assetPath = "Assets/" + manifestName + ".asset";

        public static void PrepareBundleAssetsMap() {
            BundleManifest manifest = AssetDatabase.LoadAssetAtPath<BundleManifest>(assetPath);
            if (manifest == null) {
                manifest = ScriptableObject.CreateInstance<BundleManifest>();
                AssetDatabase.CreateAsset(manifest, assetPath);
            } else {
                manifest.ClearBundleData();
            }
            string[] bundleNames = AssetDatabase.GetAllAssetBundleNames();
            foreach (string bundleName in bundleNames) {
                foreach (string assetName in AssetDatabase.GetAssetPathsFromAssetBundle(bundleName)) {
                    manifest.AddBundleAssetPath(assetName.Remove(0, "Assets/".Length), bundleName);
                }

                manifest.AddBundleData(bundleName, AssetDatabase.GetAssetBundleDependencies(bundleName, true));
            }

            AssetDatabase.LoadAssetAtPath<BundleManifest>(assetPath);
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            importer.assetBundleName = manifestName;
        }

        public static void RemoveBundleAssetsMap() {
            AssetDatabase.RemoveAssetBundleName(manifestName.ToLower(), true);
        }
    }
}