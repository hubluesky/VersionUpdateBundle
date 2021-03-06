﻿using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System;
using System.Net;
using System.Threading;
using UnityEditor.Utils;

namespace VersionUpdateEditor {
    internal class LaunchAssetBundleServer : ScriptableSingleton<LaunchAssetBundleServer> {
        private static string overloadedDevelopmentServerURL = "";
        private const string kLocalAssetbundleServerMenu = "AssetBundles/Local AssetBundle Server";

        [SerializeField]
        int m_ServerPID = 0;

        [MenuItem(kLocalAssetbundleServerMenu)]
        public static void ToggleLocalAssetBundleServer() {
            bool isRunning = IsRunning();
            if (!isRunning) {
                Run();
            } else {
                KillRunningAssetBundleServer();
            }
        }

        [MenuItem(kLocalAssetbundleServerMenu, true)]
        public static bool ToggleLocalAssetBundleServerValidate() {
            bool isRunnning = IsRunning();
            Menu.SetChecked(kLocalAssetbundleServerMenu, isRunnning);
            return true;
        }

        static bool IsRunning() {
            if (instance.m_ServerPID == 0)
                return false;

            try {
                var process = Process.GetProcessById(instance.m_ServerPID);
                if (process != null)
                    return !process.HasExited;
            } catch (Exception e) {
                UnityEngine.Debug.LogException(e);
                return false;
            }

            return false;
        }

        static void KillRunningAssetBundleServer() {
            // Kill the last time we ran
            try {
                if (instance.m_ServerPID == 0)
                    return;

                var lastProcess = Process.GetProcessById(instance.m_ServerPID);
                lastProcess.Kill();
            } catch {
            }
            instance.m_ServerPID = 0;
        }

        public static void WriteServerURL() {
            string downloadURL;
            if (string.IsNullOrEmpty(overloadedDevelopmentServerURL) == false) {
                downloadURL = overloadedDevelopmentServerURL;
            } else {
                IPHostEntry host;
                string localIP = "";
                host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList) {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                        localIP = ip.ToString();
                        break;
                    }
                }
                downloadURL = "http://" + localIP + ":7888/";
            }

            string assetBundleManagerResourcesDirectory = "Assets/Resources";
            string assetBundleUrlPath = Path.Combine(assetBundleManagerResourcesDirectory, "AssetBundleServerURL.bytes");
            Directory.CreateDirectory(assetBundleManagerResourcesDirectory);
            File.WriteAllText(assetBundleUrlPath, downloadURL);
            AssetDatabase.Refresh();
        }

        static void Run() {
            string pathToAssetServer = Path.Combine(Application.dataPath, "VersionUpdateManager/Editor/AssetBundleServer.exe");
            string pathToApp = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));

            KillRunningAssetBundleServer();

            WriteServerURL();

            string args = string.Format("\"{0}\" {1}", pathToApp, Process.GetCurrentProcess().Id);
            ProcessStartInfo startInfo = ExecuteInternalMono.GetProfileStartInfoForMono(MonoInstallationFinder.GetMonoInstallation("MonoBleedingEdge"), "4.0", pathToAssetServer, args, true);
            startInfo.WorkingDirectory = System.Environment.CurrentDirectory;
            startInfo.UseShellExecute = false;
            Process launchProcess = Process.Start(startInfo);
            if (launchProcess == null || launchProcess.HasExited == true || launchProcess.Id == 0) {
                //Unable to start process
                UnityEngine.Debug.LogError("Unable Start AssetBundleServer process");
            } else {
                //We seem to have launched, let's save the PID
                instance.m_ServerPID = launchProcess.Id;
            }
            
            UnityEngine.Debug.LogFormat("Start Server, Process Id {0} Working Directory : {1}", launchProcess.Id, startInfo.WorkingDirectory);
        }
    }
}