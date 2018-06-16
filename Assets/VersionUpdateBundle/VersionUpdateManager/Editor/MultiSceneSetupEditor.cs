using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using VersionUpdate;

namespace VersionUpdateEditor {

    [CustomEditor(typeof(MultiSceneSetup))]
    public class MultiSceneSetupEditor : Editor {
        public static readonly System.Type SceneAssetType = typeof(SceneAsset);
        private ReorderableList scenePathList;

        void OnEnable() {
            SerializedProperty propertyObject = serializedObject.FindProperty("scenePaths");
            scenePathList = new ReorderableList(serializedObject, propertyObject);
            scenePathList.drawElementCallback += ElementCallbackDelegate;
        }

        void ElementCallbackDelegate(Rect rect, int index, bool isActive, bool isFocused) {
            float toggleWidth = 20;
            Rect rectScene = new Rect(rect.x, rect.y, rect.width - toggleWidth, rect.height);
            Rect rectActive = new Rect(rect.x + rect.width - toggleWidth + 5, rect.y, toggleWidth - 5, rect.height);

            SerializedProperty elementProperty = scenePathList.serializedProperty.GetArrayElementAtIndex(index);
            EditorGUI.BeginChangeCheck();
            Object sceneObject = AssetDatabase.LoadAssetAtPath("Assets/" + elementProperty.stringValue, SceneAssetType);
            sceneObject = EditorGUI.ObjectField(rectScene, sceneObject, SceneAssetType, false);
            if (EditorGUI.EndChangeCheck())
                elementProperty.stringValue = AssetDatabase.GetAssetPath(sceneObject).Replace("Assets/", "");

            SerializedProperty indexActiveScene = serializedObject.FindProperty("indexActiveScene");
            bool selected = EditorGUI.Toggle(rectActive, indexActiveScene.intValue == index);
            if (selected && indexActiveScene.intValue != index)
                indexActiveScene.intValue = index;
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            scenePathList.DoLayoutList();


            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }

        [OnOpenAssetAttribute(1)]
        public static bool OnOpenMultiSceneSetup(int instanceID, int line) {
            Object openObject = EditorUtility.InstanceIDToObject(instanceID);
            if (!(openObject is MultiSceneSetup)) return false;
            MultiSceneSetup multiSceneSetup = openObject as MultiSceneSetup;
            SceneSetup[] sceneSetup = new SceneSetup[multiSceneSetup.scenePaths.Length];
            for (int i = 0; i < sceneSetup.Length; i++) {
                sceneSetup[i] = new SceneSetup();
                sceneSetup[i].path = "Assets/" + multiSceneSetup.scenePaths[i];
                sceneSetup[i].isActive = multiSceneSetup.indexActiveScene == i;
                sceneSetup[i].isLoaded = true;
            }
            EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
            return true;
        }
    }
}