using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetBundles {
    public class VersionUpdateMain : EditorWindow {
        private static readonly string versionDataPath = "VersionData.asset";
        private string oldPath = "VersionUpdate/Old";
        private string newPath = "VersionUpdate/New";
        private string outPath = "VersionUpdate";
        private VersionUpdateData versionData;

        [MenuItem("AssetBundles/VersionUpdatePackage", priority = 1024)]
        static void ShowWindow() {
            var window = GetWindow<VersionUpdateMain>();
            window.titleContent = new GUIContent("VersionUpdate");
            window.Show();
        }

        [MenuItem("AssetBundles/CreateVersionUpdateData")]
        static void CreateVersionUpdateData() {
            
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

        private void GenerateIncrementalPackage() {
            string manifestBundleName = typeof(AssetBundleManifest).Name;
            Resources.UnloadUnusedAssets();
            AssetBundle assetBundle = AssetBundle.LoadFromFile(Path.Combine(oldPath, manifestBundleName.ToLower()));
            if (assetBundle == null) {
                Debug.LogError("Load old version AssetBundleManifest asset bundle failed!");
                return;
            }
            AssetBundleManifest oldManifest = assetBundle.LoadAsset<AssetBundleManifest>(manifestBundleName);
            assetBundle.Unload(false);
            if (oldManifest == null) {
                Debug.LogError("Load old version AssetBundleManifest object failed!");
                return;
            }

            assetBundle = AssetBundle.LoadFromFile(Path.Combine(newPath, manifestBundleName.ToLower()));
            if (assetBundle == null) {
                Debug.LogError("Load new version AssetBundleManifest asset bundle failed!");
                return;
            }

            AssetBundleManifest newManifest = assetBundle.LoadAsset<AssetBundleManifest>(manifestBundleName);
            assetBundle.Unload(false);
            if (newManifest == null) {
                Debug.LogError("Load new version AssetBundleManifest object failed!");
                return;
            }

            List<string> addList = new List<string>();
            List<string> removeList = new List<string>();

            string[] newBundles = newManifest.GetAllAssetBundles();
            string[] oldBundles = oldManifest.GetAllAssetBundles();
            foreach (string bundleName in newBundles) {
                if (ArrayUtility.Contains(oldBundles, bundleName)) {
                    if (newManifest.GetAssetBundleHash(bundleName) == oldManifest.GetAssetBundleHash(bundleName))
                        continue;
                }
                addList.Add(bundleName);
            }
            foreach (string bundleName in oldBundles) {
                if (!ArrayUtility.Contains(newBundles, bundleName))
                    removeList.Add(bundleName);
            }

            if (addList.Count > 0) {
                string oldVersionPath = Path.Combine(oldPath, typeof(VersionUpdateTable).Name + ".json");
                string newVersionPath = Path.Combine(newPath, typeof(VersionUpdateTable).Name + ".json");

                VersionUpdateTable versionUpdateTable;
                if (!File.Exists(oldVersionPath)) {
                    Debug.LogWarning("Can not find VersionUpdateTable file!");
                    versionUpdateTable = new VersionUpdateTable();
                    versionUpdateTable.versions = new Version[] { };
                } else {
                    StreamReader readerStream = new StreamReader(oldVersionPath);
                    versionUpdateTable = JsonUtility.FromJson<VersionUpdateTable>(readerStream.ReadToEnd());
                    readerStream.Close();
                }
                Version version = new Version();
                version.versionNumber = versionData.versionNumber;
                version.packageList = new string[] { version.versionNumber.ToString() + ".zip" };
                version.removeList = removeList.ToArray();
                ArrayUtility.Add(ref versionUpdateTable.versions, version);

                string jsonText = JsonUtility.ToJson(versionUpdateTable);
                StreamWriter writeStream = new StreamWriter(newVersionPath);
                writeStream.Write(jsonText);
                writeStream.Close();

                File.Copy(newVersionPath, Path.Combine(outPath, typeof(VersionUpdateTable).Name + ".json"), true);

                // addList.Add(manifestBundleName.ToLower());
                ZipUtility.CompressesFileList(newPath, addList.ToArray(), string.Empty, Path.Combine(outPath, version.packageList[0]));
                Debug.Log("Generate Incremental Package Successful!");
            } else {
                Debug.Log("No Incremental Package Generate!");
            }
        }

        void OnEnable() {
            versionData = AssetDatabase.LoadAssetAtPath<VersionUpdateData>(versionDataPath);
        }

        private void OnGUI() {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            oldPath = EditorGUILayout.TextField("Old Version Asset Bundle Path", oldPath);
            if (GUILayout.Button("Browse", GUILayout.MaxWidth(100f)))
                BrowseForFolder(ref oldPath);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            newPath = EditorGUILayout.TextField("New Version Asset Bundle Path", newPath);
            if (GUILayout.Button("Browse", GUILayout.MaxWidth(100f)))
                BrowseForFolder(ref oldPath);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            outPath = EditorGUILayout.TextField("Out Incremental Package Path", outPath);
            if (GUILayout.Button("Browse", GUILayout.MaxWidth(100f)))
                BrowseForFolder(ref outPath);
            GUILayout.EndHorizontal();

            if (versionData != null) {
                EditorGUILayout.IntField("New Version Number", versionData.versionNumber);
            } else {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.BeginHorizontal();
                EditorGUILayout.TextField("New Version Number", "Can not load version data.");
                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button("Reload version data", GUILayout.ExpandWidth(false)))
                    versionData = AssetDatabase.LoadAssetAtPath<VersionUpdateData>(versionDataPath);
                GUILayout.EndHorizontal();
            }

            EditorGUI.BeginDisabledGroup(versionData == null);
            if (GUILayout.Button("Generate Incremental Package"))
                GenerateIncrementalPackage();
            EditorGUI.EndDisabledGroup();

            GUILayout.EndVertical();
        }
    }
}