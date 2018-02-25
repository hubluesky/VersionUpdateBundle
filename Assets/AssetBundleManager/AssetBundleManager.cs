using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundles {
    public class AssetBundleManager : MonoBehaviour {
        protected List<string> tempList = new List<string>();
        protected Dictionary<string, AssetBundleLoaded> cacheMap = new Dictionary<string, AssetBundleLoaded>();
        protected Dictionary<string, AssetBundleCreateRequest> loadingMap = new Dictionary<string, AssetBundleCreateRequest>();
        protected Dictionary<string, string[]> dependenciesMap = new Dictionary<string, string[]>();

        public string[] activeVariants { get; set; }
        public BundleManifest assetBundleManifest { get; private set; }
        public static AssetBundleManager Instance { get; private set; }

        public static string GetExternalPath() {
            switch (Application.platform) {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return Application.dataPath + "/patch/update/";
                case RuntimePlatform.Android:
                case RuntimePlatform.IPhonePlayer:
                    return Application.persistentDataPath + "/update/";
                default:
                    return Application.persistentDataPath + "/update/";
            }
        }

        public static string GetStreamingAssetsPath() {
            if (Application.isEditor)
                return System.Environment.CurrentDirectory + "/AssetBundles/" + GetPlatformName() + "/";
            else if (Application.isWebPlayer)
                return System.IO.Path.GetDirectoryName(Application.absoluteURL).Replace("\\", "/") + "/StreamingAssets/";
            else if (Application.isMobilePlatform || Application.isConsolePlatform)
                return Application.streamingAssetsPath + "/";
            else // For standalone player.
                return Application.streamingAssetsPath + "/";
        }


        public static string GetPlatformName() {
#if UNITY_EDITOR
            switch (UnityEditor.EditorUserBuildSettings.activeBuildTarget) {
                case UnityEditor.BuildTarget.Android:
                    return "Android";
                case UnityEditor.BuildTarget.iOS:
                    return "IOS";
                case UnityEditor.BuildTarget.WebGL:
                    return "WebGL";
                case UnityEditor.BuildTarget.StandaloneWindows:
                case UnityEditor.BuildTarget.StandaloneWindows64:
                    return "StandaloneWindows";
                // Add more build targets for your own.
                // If you add more targets, don't forget to add the same platforms to GetPlatformForAssetBundles(RuntimePlatform) function.
                default:
                    return null;
            }
#else
            switch (Application.platform) {
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.IPhonePlayer:
                    return "IOS";
                case RuntimePlatform.WindowsPlayer:
                    return "StandaloneWindows";
                // Add more build targets for your own.
                // If you add more targets, don't forget to add the same platforms to GetPlatformForAssetBundles(RuntimePlatform) function.
                default:
                    return null;
            }
#endif
        }

        public static void Initialize() {
            GameObject gameObject = new GameObject("AssetBundleManager");
            Object.DontDestroyOnLoad(gameObject);
            AssetBundleManager assetBundleManager = null;
#if UNITY_EDITOR
            assetBundleManager = gameObject.AddComponent(typeof(AssetBundleManagerSimulate)) as AssetBundleManager;
#else
            assetBundleManager = gameObject.AddComponent<AssetBundleManager>();
#endif

#if UNITY_EDITOR
            if (AssetBundleManagerSimulate.simulateAssetBundle) {
            } else
#endif
            {
                assetBundleManager.LoadedManifestAssetBundle();
            }
            Instance = assetBundleManager;
        }

        internal string RemapVariantName(string assetBundleName) {
            string[] bundlesWithVariant = assetBundleManifest.GetAllAssetBundlesWithVariant();

            string[] split = assetBundleName.Split('.');

            int bestFit = int.MaxValue;
            int bestFitIndex = -1;
            // Loop all the assetBundles with variant to find the best fit variant assetBundle.
            for (int i = 0; i < bundlesWithVariant.Length; i++) {
                string[] curSplit = bundlesWithVariant[i].Split('.');
                if (curSplit[0] != split[0])
                    continue;

                int found = System.Array.IndexOf(activeVariants, curSplit[1]);

                // If there is no active variant found. We still want to use the first 
                if (found == -1)
                    found = int.MaxValue - 1;

                if (found < bestFit) {
                    bestFit = found;
                    bestFitIndex = i;
                }
            }

            if (bestFit == int.MaxValue - 1) {
                Debug.LogWarning("Ambigious asset bundle variant chosen because there was no matching active variant: " + bundlesWithVariant[bestFitIndex]);
            }

            if (bestFitIndex != -1) {
                return bundlesWithVariant[bestFitIndex];
            } else {
                return assetBundleName;
            }
        }

        public void LoadedManifestAssetBundle() {
            float t1 = Time.realtimeSinceStartup;
            string bundleName = typeof(BundleManifest).Name;
            AssetBundle bundleRequest = AssetBundle.LoadFromFile(GetBundlePath(bundleName.ToLower()));
            float t2 = Time.realtimeSinceStartup;
            if (bundleRequest == null) {
                Debug.LogError("Can not load manifest asset bundle.");
            } else {
                Object assetRequest = bundleRequest.LoadAsset(bundleName);
                assetBundleManifest = assetRequest as BundleManifest;
                bundleRequest.Unload(false);
            }
            float t3 = Time.realtimeSinceStartup;
            Debug.LogFormat("LoadedManifestAssetBundle: {0}, {1}", t2 - t1, t3 - t2);
        }

        internal AssetBundleLoaded LoadAssetBundle(string bundleName, AssetBundleLoaded dependencyAssetBundle = null) {
            AssetBundleLoaded loadAssetBundle;
            if (!cacheMap.TryGetValue(bundleName, out loadAssetBundle)) {
                AssetBundle bundle = AssetBundle.LoadFromFile(GetBundlePath(bundleName));
                string[] dependencies = assetBundleManifest.GetAllDependencies(bundleName);
                loadAssetBundle = new AssetBundleLoaded(dependencies);
                loadAssetBundle.SetLoadedAssetBundle(bundle);
                cacheMap.Add(bundleName, loadAssetBundle);

                if (bundle == null) {
                    Debug.LogError("Load asset bundle failed: " + bundleName);
                    loadAssetBundle.referencedCount++;
                    return null;
                }

                for (int i = 0; i < dependencies.Length; i++) {
                    dependencies[i] = RemapVariantName(dependencies[i]);
                    LoadAssetBundle(dependencies[i], loadAssetBundle);
                }

                if (!dependenciesMap.ContainsKey(bundleName))
                    dependenciesMap.Add(bundleName, dependencies);
            }

            loadAssetBundle.AddDependencyAssetBundle(dependencyAssetBundle);
            loadAssetBundle.referencedCount++;
            return loadAssetBundle;
        }

        void Update() {
            if (loadingMap.Count == 0)
                return;

            foreach (KeyValuePair<string, AssetBundleCreateRequest> entry in loadingMap) {
                if (entry.Value.isDone) {
                    tempList.Add(entry.Key);
                    cacheMap[entry.Key].SetLoadedAssetBundle(entry.Value.assetBundle);
                    if (entry.Value.assetBundle == null)
                        Debug.LogError("Load asset bundle failed: " + entry.Key);
                }
            }

            foreach (string key in tempList)
                loadingMap.Remove(key);
            tempList.Clear();
        }

        internal AssetBundleLoaded LoadAssetBundleAsync(string bundleName, AssetBundleLoaded dependencyAssetBundle = null) {
            AssetBundleLoaded assetBundleLoad;
            if (!cacheMap.TryGetValue(bundleName, out assetBundleLoad)) {
                string[] dependencies = assetBundleManifest.GetAllDependencies(bundleName);
                assetBundleLoad = new AssetBundleLoaded(dependencies);
                cacheMap.Add(bundleName, assetBundleLoad);

                AssetBundleCreateRequest loading = AssetBundle.LoadFromFileAsync(GetBundlePath(bundleName));
                loadingMap.Add(bundleName, loading);

                for (int i = 0; i < dependencies.Length; i++) {
                    dependencies[i] = RemapVariantName(dependencies[i]);
                    LoadAssetBundleAsync(dependencies[i], assetBundleLoad);
                }

                if (!dependenciesMap.ContainsKey(bundleName))
                    dependenciesMap.Add(bundleName, dependencies);
            }
            assetBundleLoad.AddDependencyAssetBundle(dependencyAssetBundle);
            assetBundleLoad.referencedCount++;
            return assetBundleLoad;
        }

        public static string GetBundlePath(string bundleName) {
            string bundlePath = GetExternalPath() + bundleName;
            if (System.IO.File.Exists(bundlePath))
                return bundlePath;
            return GetStreamingAssetsPath() + bundleName;
        }

        protected virtual bool GetBundleName(string assetName, out string bundleName) {
            if (!assetBundleManifest.GetBundleNameByAssetPath(assetName, out bundleName)) {
                Debug.LogError("Load asset failed: " + assetName);
                return false;
            }
            return true;
        }

        public T LoadAsset<T>(string assetName) where T : Object {
            return LoadAsset(assetName, typeof(T)) as T;
        }

        public Object LoadAsset(string assetName, System.Type type) {
            string bundleName;
            if (!GetBundleName(assetName, out bundleName))
                return null;
            return LoadBundleAsset(bundleName, System.IO.Path.GetFileNameWithoutExtension(assetName), type);
        }

        public T LoadSubAsset<T>(string assetName, string subAssetName) where T : Object {
            return LoadSubAsset(assetName, subAssetName, typeof(T)) as T;
        }

        public Object LoadSubAsset(string assetName, string subAssetName, System.Type type) {
            string bundleName;
            if (!GetBundleName(assetName, out bundleName))
                return null;
            return LoadBundleSubAsset(bundleName, System.IO.Path.GetFileNameWithoutExtension(assetName), subAssetName, type);
        }

        public AssetAsyncTask LoadAssetAsync<T>(string assetName) {
            return LoadAssetAsync(assetName, typeof(T));
        }

        public AssetAsyncTask LoadAssetAsync(string assetName, System.Type type) {
            string bundleName;
            if (!GetBundleName(assetName, out bundleName))
                return new CacheAssetAsyncTask(null);
            return LoadBundleAssetAsync(bundleName, System.IO.Path.GetFileNameWithoutExtension(assetName), type);
        }

        public AssetAsyncTask LoadSubAssetAsync<T>(string assetName, string subAssetName) {
            return LoadSubAssetAsync(assetName, subAssetName, typeof(T));
        }

        public AssetAsyncTask LoadSubAssetAsync(string assetName, string subAssetName, System.Type type) {
            string bundleName;
            if (!GetBundleName(assetName, out bundleName))
                return new CacheAssetAsyncTask(null);
            return LoadBundleSubAssetAsync(bundleName, System.IO.Path.GetFileNameWithoutExtension(assetName), subAssetName, type);
        }

        public AssetAsyncTask LoadSceneAsync(string sceneName, UnityEngine.SceneManagement.LoadSceneMode mode) {
            string bundleName;
            if (!GetBundleName(sceneName, out bundleName))
                return new CacheAssetAsyncTask(null);
            return LoadBundleSceneAsync(bundleName, System.IO.Path.GetFileNameWithoutExtension(sceneName), mode);
        }

        public T LoadBundleAsset<T>(string bundleName, string assetName) where T : Object {
            return LoadBundleAsset(bundleName, assetName, typeof(T)) as T;
        }

        public virtual Object LoadBundleAsset(string bundleName, string assetName, System.Type type) {
            AssetBundleLoaded assetBundleWrap = LoadAssetBundle(bundleName);
            if (assetBundleWrap.assetBundle == null)
                return null;
            return assetBundleWrap.assetBundle.LoadAsset(assetName, type);
        }

        public T LoadBundleSubAsset<T>(string bundleName, string assetName, string subAssetName) where T : Object {
            return LoadBundleSubAsset(bundleName, assetName, subAssetName, typeof(T)) as T;
        }

        public virtual Object LoadBundleSubAsset(string bundleName, string assetName, string subAssetName, System.Type type) {
            AssetBundleLoaded loadAssetBundle = LoadAssetBundle(bundleName);
            if (loadAssetBundle.assetBundle == null)
                return null;
            Object[] subAssets = loadAssetBundle.assetBundle.LoadAssetWithSubAssets(assetName, type);
            foreach (Object subAsset in subAssets) {
                if (subAsset.name == subAssetName)
                    return subAsset;
            }
            return null;
        }

        public AssetAsyncTask LoadBundleAssetAsync<T>(string bundleName, string assetName) where T : Object {
            return LoadBundleAssetAsync(bundleName, assetName, typeof(T));
        }

        public AssetAsyncTask LoadBundleAssetAsync(string bundleName, string assetName, System.Type type) {
            return CreateLoadBundleAssetAsyncTask(bundleName, assetName, type);
        }

        public AssetAsyncTask LoadBundleSubAssetAsync<T>(string bundleName, string assetName, string subAssetName) where T : Object {
            return LoadBundleSubAssetAsync(bundleName, assetName, subAssetName, typeof(T));
        }

        public AssetAsyncTask LoadBundleSubAssetAsync(string bundleName, string assetName, string subAssetName, System.Type type) {
            return CreateLoadBundleSubAssetAsyncTask(bundleName, assetName, subAssetName, type);
        }

        public AssetAsyncTask LoadBundleSceneAsync(string bundleName, string sceneName, UnityEngine.SceneManagement.LoadSceneMode mode) {
            return CreateLoadBundleSceneAsyncTask(bundleName, sceneName, mode);
        }

        protected virtual AssetAsyncTask CreateLoadBundleAssetAsyncTask(string bundleName, string assetName, System.Type type) {
            return new LoadedAssetTask(this, bundleName, assetName, type);
        }

        protected virtual AssetAsyncTask CreateLoadBundleSubAssetAsyncTask(string bundleName, string assetName, string subAssetName, System.Type type) {
            return new LoadedSubAssetTask(this, bundleName, assetName, subAssetName, type);
        }

        protected virtual AssetAsyncTask CreateLoadBundleSceneAsyncTask(string bundleName, string sceneName, UnityEngine.SceneManagement.LoadSceneMode mode) {
            return new LoadedSceneTask(this, bundleName, sceneName, mode);
        }

        public void UnloadAllAssetBundle() {
            tempList.Clear();
            tempList.AddRange(cacheMap.Keys);
            foreach (string bundleName in tempList)
                UnloadAssetBundle(bundleName, true);
            tempList.Clear();
            Resources.UnloadUnusedAssets();
            assetBundleManifest = null;
        }

        public virtual void UnloadAssetBundle(string bundleName, bool forceUnload = false) {
            UnloadAssetBundleInternal(bundleName, forceUnload);
            UnloadDependencies(bundleName, forceUnload);
        }

        protected void UnloadAssetBundleInternal(string bundleName, bool forceUnload) {
            AssetBundleLoaded bundle = null;
            if (!cacheMap.TryGetValue(bundleName, out bundle))
                return;

            if (!forceUnload && --bundle.referencedCount > 0)
                return;

            bundle.assetBundle.Unload(false);
            cacheMap.Remove(bundleName);
        }

        protected void UnloadDependencies(string bundleName, bool forceUnload) {
            string[] dependencies = null;
            if (!dependenciesMap.TryGetValue(bundleName, out dependencies))
                return;

            foreach (string dependency in dependencies)
                UnloadAssetBundleInternal(dependency, forceUnload);

            dependenciesMap.Remove(bundleName);
        }
    }
}