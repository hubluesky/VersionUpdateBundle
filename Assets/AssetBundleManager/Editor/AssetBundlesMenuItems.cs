using UnityEditor;
using UnityEngine;

namespace AssetBundles {
    public class AssetBundlesMenuItems {
        private const string kSimulationMode = "Kola/AssetBundles/Simulation Mode";

        [MenuItem(kSimulationMode)]
        public static void ToggleSimulationMode() {
            AssetBundleManagerSimulate.simulateAssetBundle = !AssetBundleManagerSimulate.simulateAssetBundle;
        }

        [MenuItem(kSimulationMode, true)]
        public static bool ToggleSimulationModeValidate() {
            Menu.SetChecked(kSimulationMode, AssetBundleManagerSimulate.simulateAssetBundle);
            return true;
        }
    }
}