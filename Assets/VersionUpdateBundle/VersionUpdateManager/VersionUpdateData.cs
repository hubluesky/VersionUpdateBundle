using UnityEngine;

namespace VersionUpdate {
	public class VersionUpdateData : ScriptableObject {
		public int versionNumber;
		public string versionName;
		public string androidUrl;
		public string iosUrl;
		public string windowsUrl;

		public string versionUrl {
			get {
				switch (Application.platform) {
					case RuntimePlatform.Android:
						return androidUrl;
					case RuntimePlatform.IPhonePlayer:
						return iosUrl;
					case RuntimePlatform.WindowsPlayer:
						return windowsUrl;
					default:
						return windowsUrl;
				}
			}
		}
	}
}