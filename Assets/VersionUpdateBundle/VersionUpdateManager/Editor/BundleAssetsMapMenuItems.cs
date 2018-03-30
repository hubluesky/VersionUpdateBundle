using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VersionUpdate {
    public class BundleAssetsMapMenuItems {
        private static readonly string manifestName = typeof(BundleManifest).Name;
        private static readonly string manifestAssetPath = "Assets/" + manifestName + ".asset";

        public static void PrepareBundleAssetsMap() {
            BundleManifest manifest = AssetDatabase.LoadAssetAtPath<BundleManifest>(manifestAssetPath);
            if (manifest == null) {
                manifest = ScriptableObject.CreateInstance<BundleManifest>();
                AssetDatabase.CreateAsset(manifest, manifestAssetPath);
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

            AssetDatabase.LoadAssetAtPath<BundleManifest>(manifestAssetPath);
            AssetImporter importer = AssetImporter.GetAtPath(manifestAssetPath);
            importer.assetBundleName = manifestName;
        }

        public static void BuildBundleCompleted(UnityEngine.AssetBundles.AssetBundleDataSource.ABBuildInfo buildInfo) {
            AssetDatabase.RemoveAssetBundleName(manifestName.ToLower(), true);
            string assetManifestName = Path.Combine(buildInfo.outputDirectory, Path.GetFileName(buildInfo.outputDirectory));
            string newAssetManifestName = Path.Combine(buildInfo.outputDirectory, typeof(AssetBundleManifest).Name);
            if (File.Exists(newAssetManifestName))
                File.Delete(newAssetManifestName);
            if (File.Exists(newAssetManifestName + ".manifest"))
                File.Delete(newAssetManifestName + ".manifest");

            File.Move(assetManifestName, newAssetManifestName);
            File.Move(assetManifestName + ".manifest", newAssetManifestName + ".manifest");
            AssetDatabase.DeleteAsset(manifestAssetPath);
        }
    }
}