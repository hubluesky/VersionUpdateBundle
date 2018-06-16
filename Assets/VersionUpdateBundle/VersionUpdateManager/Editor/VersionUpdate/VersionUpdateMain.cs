using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VersionUpdate;

namespace VersionUpdateEditor {
    public class VersionUpdateMain : EditorWindow {
        private static readonly string versionDataPath = "Assets/" + VersionUpdateManager.VersionUpdateDataName;
        private static readonly string lastManifestFolder = "LastManifest";
        private static readonly string assetBundlesFolder = "AssetBundles";
        private static readonly string publishFolder = "Publish";
        private string versionUpdateCreateFolder = PlatformUtility.EditorWindowsAssetsPath;
        private bool hasCreateVersionFolder;
        private string versionUpdatePath = PlatformUtility.EditorWindowsAssetsPath;
        private bool hasPackageVersion;
        private bool copyToLastManifest = true;
        private VersionUpdateData lastVersionData;
        private VersionUpdateData versionData;

        [MenuItem("AssetBundles/VersionUpdatePackage")]
        static void ShowWindow() {
            var window = GetWindow<VersionUpdateMain>();
            window.titleContent = new GUIContent("VersionUpdate");
            window.Show();
        }

        [MenuItem("AssetBundles/CreateVersionUpdateData")]
        static void CreateVersionUpdateData() {
            Object versionData = AssetDatabase.LoadAssetAtPath<VersionUpdateData>(versionDataPath);
            if (versionData == null) {
                CreateScriptableObjectAsset<VersionUpdateData>(versionDataPath);
                AssetImporter importer = AssetImporter.GetAtPath(versionDataPath);
                importer.assetBundleName = typeof(VersionUpdateData).Name;
                Debug.Log("Create version data successed! " + versionDataPath);
            } else {
                Debug.Log("Version data has existed! " + versionDataPath);
            }
        }

        public static T CreateScriptableObjectAsset<T>(string assetPath) where T : ScriptableObject {
            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return asset;
        }

        private void BrowseForFolder(ref string path) {
            var newPath = EditorUtility.OpenFolderPanel("Bundle Folder", path, string.Empty);
            if (!string.IsNullOrEmpty(newPath)) {
                var gamePath = Path.GetFullPath(".");
                gamePath = gamePath.Replace("\\", "/");
                if (newPath.StartsWith(gamePath) && newPath.Length > gamePath.Length)
                    newPath = newPath.Remove(0, gamePath.Length + 1);
                path = newPath;
            }
        }

        private static void WriteVersionUpdateTable(string outPath, int versionNumber, string versionZipName) {
            string versionUpdateTablePath = Path.Combine(outPath, typeof(VersionUpdateTable).Name + ".json");
            VersionUpdateTable versionUpdateTable;
            if (!File.Exists(versionUpdateTablePath)) {
                versionUpdateTable = new VersionUpdateTable();
                versionUpdateTable.versions = new Version[] { };

                if (!Directory.Exists(outPath))
                    Directory.CreateDirectory(outPath);
            } else {
                using (StreamReader readStream = new StreamReader(versionUpdateTablePath)) {
                    versionUpdateTable = JsonUtility.FromJson<VersionUpdateTable>(readStream.ReadToEnd());
                }
            }

            Version version = new Version();
            version.versionNumber = versionNumber;
            version.packageList = new string[] { versionZipName };
            version.removeList = new string[] { };
            ArrayUtility.Add(ref versionUpdateTable.versions, version);

            string jsonText = JsonUtility.ToJson(versionUpdateTable);
            using (StreamWriter writeStream = new StreamWriter(versionUpdateTablePath)) {
                writeStream.Write(jsonText);
            }
        }

        private static bool LoadAssetBundleAsset<T>(string path, out T asset) where T : Object {
            asset = null;
            string assetName = typeof(T).Name;
            string bundlePath = Path.Combine(path, assetName.ToLower());
            if (File.Exists(bundlePath)) {
                AssetBundle assetBundle = AssetBundle.LoadFromFile(bundlePath);
                if (assetBundle != null) {
                    asset = assetBundle.LoadAsset<T>(assetName);
                    assetBundle.Unload(false);
                }
            }
            return asset != null;
        }

        private void CreateVersionPackage() {
            Resources.UnloadUnusedAssets();

            string lasetManifestPath = Path.Combine(versionUpdatePath, lastManifestFolder);
            string assetBundlesPath = Path.Combine(versionUpdatePath, assetBundlesFolder);
            string publishPath = Path.Combine(versionUpdatePath, publishFolder);

            AssetBundleManifest newManifest;
            if (!LoadAssetBundleAsset(assetBundlesPath, out newManifest)) {
                Debug.LogWarning("Load Asset Bundle Manifest Failed! " + assetBundlesPath);
                return;
            }

            if (!LoadAssetBundleAsset(assetBundlesPath, out versionData)) {
                Debug.LogWarning("Load Version Update Data Failed!" + assetBundlesPath);
                return;
            }

            string zipName = versionData.versionNumber + ".zip";
            string[] newBundles = newManifest.GetAllAssetBundles();
            WriteVersionUpdateTable(publishPath, versionData.versionNumber, zipName);
            ZipUtility.CompressesFileList(assetBundlesPath, newBundles, string.Empty, Path.Combine(publishPath, zipName));
            if (copyToLastManifest)
                CopyAssetBundleManifestToOldPath(assetBundlesPath, lasetManifestPath);
            hasPackageVersion = true;
            Debug.Log("Create Version Package Successful!");
            EditorUtility.RevealInFinder(publishPath);
        }

        private void GenerateIncrementalPackage() {
            string manifestBundleName = typeof(AssetBundleManifest).Name;
            Resources.UnloadUnusedAssets();

            string lasetManifestPath = Path.Combine(versionUpdatePath, lastManifestFolder);
            string assetBundlesPath = Path.Combine(versionUpdatePath, assetBundlesFolder);
            string publishPath = Path.Combine(versionUpdatePath, publishFolder);

            AssetBundleManifest lastManifest;
            if (!LoadAssetBundleAsset(lasetManifestPath, out lastManifest)) {
                Debug.LogWarning("Load last AssetBundleManifest Failed! " + lasetManifestPath);
                return;
            }

            AssetBundleManifest newManifest;
            if (!LoadAssetBundleAsset(assetBundlesPath, out newManifest)) {
                Debug.LogWarning("Load AssetBundleManifest Failed! " + assetBundlesPath);
                return;
            }

            if (!LoadAssetBundleAsset(assetBundlesPath, out versionData)) {
                Debug.LogWarning("Load Version Update Data Failed!" + assetBundlesPath);
                return;
            }

            List<string> addList = new List<string>();
            List<string> removeList = new List<string>();

            string[] newBundles = newManifest.GetAllAssetBundles();
            string[] oldBundles = lastManifest.GetAllAssetBundles();
            foreach (string bundleName in newBundles) {
                if (ArrayUtility.Contains(oldBundles, bundleName)) {
                    if (newManifest.GetAssetBundleHash(bundleName) == lastManifest.GetAssetBundleHash(bundleName))
                        continue;
                }
                addList.Add(bundleName);
            }
            foreach (string bundleName in oldBundles) {
                if (!ArrayUtility.Contains(newBundles, bundleName))
                    removeList.Add(bundleName);
            }

            if (addList.Count > 0) {
                string zipName = versionData.versionNumber + ".zip";
                WriteVersionUpdateTable(publishPath, versionData.versionNumber, zipName);
                ZipUtility.CompressesFileList(assetBundlesPath, addList.ToArray(), string.Empty, Path.Combine(publishPath, zipName));
                if (copyToLastManifest)
                    CopyAssetBundleManifestToOldPath(assetBundlesPath, lasetManifestPath);
                hasPackageVersion = true;
                Debug.Log("Generate Incremental Package Successful!");
            } else {
                Debug.Log("No Incremental Package Generate!");
            }

            EditorUtility.RevealInFinder(publishPath);
        }

        private void CopyAssetBundleManifestToOldPath(string srcPath, string destPath) {
            string manifestBundleName = typeof(AssetBundleManifest).Name;
            string versionUpdateData = typeof(VersionUpdateData).Name;
            if (!Directory.Exists(destPath))
                Directory.CreateDirectory(destPath);
            File.Copy(Path.Combine(srcPath, manifestBundleName), Path.Combine(destPath, manifestBundleName), true);
            File.Copy(Path.Combine(srcPath, versionUpdateData), Path.Combine(destPath, versionUpdateData), true);
        }

        private void CreateVersionUpdateFolder(string folderName) {
            if (!Directory.Exists(folderName))
                Directory.CreateDirectory(folderName);
            Directory.CreateDirectory(Path.Combine(folderName, lastManifestFolder));
            Directory.CreateDirectory(Path.Combine(folderName, assetBundlesFolder));
            Directory.CreateDirectory(Path.Combine(folderName, publishFolder));
            hasCreateVersionFolder = true;
            Debug.Log("Create Version Update Folder Successed! " + folderName);
        }

        void OnEnable() {
            LoadAssetBundleAsset(Path.Combine(versionUpdatePath, assetBundlesFolder), out versionData);
            LoadAssetBundleAsset(Path.Combine(versionUpdatePath, lastManifestFolder), out lastVersionData);
        }

        private void OnGUI() {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            versionUpdateCreateFolder = EditorGUILayout.TextField("Version Update Folder", versionUpdateCreateFolder);
            if (GUILayout.Button("Browse", GUILayout.MaxWidth(100f)))
                BrowseForFolder(ref versionUpdateCreateFolder);
            GUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(versionUpdateCreateFolder));
            if (GUILayout.Button("Create Version Update Folder"))
                CreateVersionUpdateFolder(versionUpdateCreateFolder);
            if (hasCreateVersionFolder)
                EditorGUILayout.HelpBox("Create Version Folder Successed! Please Generate Asset Bundles in " + Path.Combine(versionUpdateCreateFolder, assetBundlesFolder), MessageType.Info);
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(30);

            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            versionUpdatePath = EditorGUILayout.TextField("Version Update Path", versionUpdatePath);
            if (GUILayout.Button("Browse", GUILayout.MaxWidth(100f)))
                BrowseForFolder(ref versionUpdatePath);
            if (EditorGUI.EndChangeCheck()) {
                LoadAssetBundleAsset(Path.Combine(versionUpdatePath, assetBundlesFolder), out versionData);
            }
            GUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(true);
            string lastManifestPath = Directory.Exists(Path.Combine(versionUpdatePath, lastManifestFolder)) ? lastManifestFolder : null;
            string assetBundlesPath = Directory.Exists(Path.Combine(versionUpdatePath, assetBundlesFolder)) ? assetBundlesFolder : null;
            string publishPath = Directory.Exists(Path.Combine(versionUpdatePath, publishFolder)) ? publishFolder : null;
            EditorGUILayout.TextField("Last Manifest Path", lastManifestPath);
            EditorGUILayout.TextField("Asset Bundles Path", assetBundlesPath);
            EditorGUILayout.TextField("Publish Path", publishPath);

            if (lastVersionData != null) {
                EditorGUILayout.IntField("Last Version Number", lastVersionData.versionNumber);
            } else {
                EditorGUILayout.TextField("Last Version Number", "Can not load last version data.");
            }

            if (versionData != null) {
                EditorGUILayout.IntField("New Version Number", versionData.versionNumber);
            } else {
                EditorGUILayout.TextField("New Version Number", "Can not load version data.");
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            copyToLastManifest = GUILayout.Toggle(copyToLastManifest, "Copy To LastManifest");

            EditorGUI.BeginDisabledGroup(versionData == null);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Version Package"))
                CreateVersionPackage();
            if (GUILayout.Button("Generate Incremental Package"))
                GenerateIncrementalPackage();
            GUILayout.EndHorizontal();
            if (hasPackageVersion)
                EditorGUILayout.HelpBox("Create Version Folder Successed! Please Generate Asset Bundles in " + Path.Combine(versionUpdatePath, publishFolder), MessageType.Info);
            EditorGUI.EndDisabledGroup();

            GUILayout.EndVertical();
        }
    }
}