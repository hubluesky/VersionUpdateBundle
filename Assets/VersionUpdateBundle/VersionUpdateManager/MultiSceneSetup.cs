using UnityEngine;

namespace VersionUpdate {
    [CreateAssetMenu(fileName = "MultiSceneSetup", menuName = "AssetBundles/MultiSceneSetup", order = 0)]
    public class MultiSceneSetup : ScriptableObject {
        public string[] scenePaths;
        public int indexActiveScene;
    }
}