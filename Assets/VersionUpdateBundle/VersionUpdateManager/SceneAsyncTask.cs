using UnityEngine;

namespace VersionUpdate {
    public abstract class SceneAsyncTask : AssetAsyncTask {
        protected AsyncOperation operation;
        private bool _notActivatedScene;

        public override bool notActivatedScene {
            get { return _notActivatedScene; }
            set {
                _notActivatedScene = value;
                if (operation != null)
                    operation.allowSceneActivation = !value;
            }
        }

        public override bool sceneLoadFinished {
            get {
                if (operation == null) return false;
                return operation.allowSceneActivation ? operation.isDone : operation.progress >= 0.9f;
            }
        }
    }
}