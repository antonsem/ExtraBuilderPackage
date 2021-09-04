using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
//https://forum.unity.com/threads/c-compression-zip-missing.577492/
using System.IO.Compression;
using System.Text;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;

namespace ExtraTools.ExtraBuilder
{
    /// <summary>
    /// Used to build an object, zip it, and upload it to the itch.io.
    /// --------------------------------------------------------------
    /// Requires a csc.rsp file to be present in the Assets folder.
    /// Just create a text file, rename it to csc.rsp, and paste the following line inside:
    /// -r:System.IO.Compression.FileSystem.dll
    /// </summary>
    [CreateAssetMenu(fileName = "ExtraBuilder", menuName = "ExtraTools/Builder")]
    public class Builder : ScriptableObject
    {
        [Serializable]
        public class ItchSettings
        {
            [Tooltip("An upload channel name on itch.\ne.g. win, osx, linux etc.")]
            public string channelName;
            [Tooltip(
                "The name of the executable file including extension.\ne.g. myGame.exe, myGame.app, myGame.x86_64 etc.")]
            public string buildName;
            [Tooltip(
                "Relative build directory for a platform inside a master build folder.'\ne.g. 'C://.../MyGame/win' where MyGame is the master build folder, and win is the directory.")]
            public string directory;
            [Tooltip("Name of your itch.io account.\ne.g. for 'myaccountname.itch.io' it should be myaccountname.")]
            public string itchAccountName;
            [Tooltip(
                "The itch game url.\ne.g. if you game page is 'myaccountname.itch.io/my-game' itchGameURL should be 'my-game'")]
            public string itchGameURL;
            public BuildTargetGroup buildGroup;
            public BuildTarget buildTarget;
            [Tooltip(
                "Scene paths relative to the project folder.\ne.g. 'Assets/Scenes/SampleScene' or 'Assets/Scenes/SampleScene.unity'")]
            public string[] scenes;
            [Tooltip("Should this build be zipped? It won't be uploaded to itch if it is not zipped.")]
            public bool createZipFile = true;
            [Tooltip("Should this build be uploaded to itch?")]
            public bool uploadToItch = true;
            [Tooltip("Should this be included in builds when building all?")]
            public bool includeInBuild = true;
            [Tooltip("Should the report be saved automatically to the build folder?")]
            public bool saveReportToBuildFolder = false;

            public void Build()
            {
                Builder.Build(this);
            }
        }

        [SerializeField] private ItchSettings[] itchSettings;
        [SerializeField,
         Tooltip("Should the build target switched back to the current one after all builds are complete?")]
        private bool keepCurrentBuildTarget = false;
        [SerializeField,
         Tooltip("Should the report be saved automatically to the build folder when using Build All method?")]
        private bool saveReportToBuildFolder = true;

        private static string _deployPath;
        private static string _report;

        /// <summary>
        /// Iterates through itchSettings and builds all that included in build
        /// </summary>
        [ContextMenu("Build All")]
        public void BuildAll()
        {
            if (itchSettings == null || itchSettings.Length == 0)
            {
                EditorUtility.DisplayDialog("No Settings!", "No itch settings! You should have at least one setting.",
                    "Oh, sorry :(");
                return;
            }

            GetDeployPath();
            if (string.IsNullOrEmpty(_deployPath)) return;

            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;

            StringBuilder report = new StringBuilder();
            
            foreach (var setting in itchSettings)
            {
                if (!setting.includeInBuild)
                {
                    report.Append($"----------\n\n{setting.channelName} was not included in the build. Skipping.\n");
                    Debug.Log($"{setting.buildName} is not included in the build process. Skipping...");
                    continue;
                }

                report.Append(Build(setting, _deployPath, false, false));
            }

            if (keepCurrentBuildTarget)
                report.Append(SwitchBuildTarget(buildTargetGroup, buildTarget));

            _report = report.ToString();
            if (saveReportToBuildFolder)
                SaveReport(_report, $"{_deployPath}/Report.txt");

        }

        /// <summary>
        /// Gets a build for the relevant target, zips it, and uploads to itch
        /// </summary>
        /// <param name="settings">Settings for the build</param>
        /// <param name="path">Path for the build</param>
        /// <param name="switchTargetOnceDone">Should the build target switch back once the build is done?</param>
        /// <param name="overwriteReport">Should the static _report be overwritten?</param>
        private static string Build(ItchSettings settings, string path = "", bool switchTargetOnceDone = true, bool overwriteReport = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = GetDeployPath();
                if (string.IsNullOrEmpty(path)) return "";
            }

            StringBuilder report = new StringBuilder(
                $"----------\n\n{settings.channelName} Build\nStarted: {DateTime.Now.ToString()}\nBuild path: {path}\n" +
                $"Channel: {settings.channelName}\nBuild target group: {settings.buildGroup}\n" +
                $"Build target: {settings.buildTarget}\nWill be zipped: {settings.createZipFile}\n");

            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var deployed = Deploy(settings.scenes, $"{path}/{settings.directory}", settings.buildName,
                settings.buildGroup, settings.buildTarget);

            stopwatch.Stop();

            report.Append(
                $"{(deployed ? "Deployed" : "Failed")} at {DateTime.Now.ToString()} in {stopwatch.ElapsedMilliseconds / 1000}s\n");

            stopwatch.Reset();

            if (!deployed || !settings.createZipFile)
            {
                report.Append("Will not be creating a zip file.\n");

                if (switchTargetOnceDone)
                    report.Append(SwitchBuildTarget(buildTargetGroup, buildTarget));

                if (settings.saveReportToBuildFolder)
                    SaveReport(report.ToString(), $"{path}/{settings.directory}/Report.txt");
                if (overwriteReport)
                    _report = report.ToString();
                return report.ToString();
            }

            stopwatch.Restart();
            report.Append($"Zipping {path} to {path}.zip\n");
            var zipped = Zip($"{path}/{settings.directory}");
            stopwatch.Stop();
            report.Append(
                $"{(zipped ? "Zipped" : "Failed to zip")} {path} at {DateTime.Now} in {stopwatch.ElapsedMilliseconds / 1000}s\n");

            if (!zipped || !settings.uploadToItch)
            {
                report.Append("Will not be uploading to itch\n");

                if (switchTargetOnceDone)
                    report.Append(SwitchBuildTarget(buildTargetGroup, buildTarget));

                if (settings.saveReportToBuildFolder)
                    SaveReport(report.ToString(), $"{path}/{settings.directory}/Report.txt");
                if (overwriteReport)
                    _report = report.ToString();
                return report.ToString();
            }

            report.Append($"Started uploading to itch at {DateTime.Now.ToString()}\n");
            stopwatch.Reset();
            stopwatch.Restart();
            bool pushed = PushToItch($"{path}/{settings.directory}.zip", settings.itchAccountName, settings.itchGameURL,
                settings.channelName);

            stopwatch.Stop();
            report.Append(
                $"{(pushed ? "Pushed" : "Failed to push")} to itch at {DateTime.Now.ToString()} in {stopwatch.ElapsedMilliseconds / 1000}s\n");

            if (!switchTargetOnceDone)
            {
                if (settings.saveReportToBuildFolder)
                    SaveReport(report.ToString(), $"{path}/{settings.directory}/Report.txt");
                if (overwriteReport)
                    _report = report.ToString();
                return report.ToString();
            }

            report.Append(SwitchBuildTarget(buildTargetGroup, buildTarget));
            
            if (settings.saveReportToBuildFolder)
                SaveReport(report.ToString(), $"{path}/{settings.directory}/Report.txt");

            if (overwriteReport)
                _report = report.ToString();
            return report.ToString();
        }

        private static string SwitchBuildTarget(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            StringBuilder report = new StringBuilder($"Switching build target to {buildTarget}\n");

            bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);

            stopwatch.Stop();
            report.Append(
                $"Build target switched at {DateTime.Now.ToString()} in {stopwatch.ElapsedMilliseconds / 1000}s\n");

            if (!switched)
            {
                Debug.LogError(
                    $"All builds were completed and uploaded. However, couldn't switch back to the original build target '{buildTarget}'");
            }

            return report.ToString();
        }

        /// <summary>
        /// Builds the project
        /// </summary>
        /// <param name="scenes">Scenes to include to this build</param>
        /// <param name="directory">Directory for this specific build</param>
        /// <param name="buildName">Executable name including the extension</param>
        /// <param name="buildTargetGroup">Build target group</param>
        /// <param name="buildTarget">Build target</param>
        /// <returns>True if the build was successful</returns>
        private static bool Deploy(string[] scenes, string directory, string buildName,
            BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
        {
            try
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
                Directory.CreateDirectory(directory);

                EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);

                for (var i = 0; i < scenes.Length; i++)
                {
                    var extension = scenes[i].Split('.');
                    if (extension.Length == 0 || extension[extension.Length - 1] != "unity")
                        scenes[i] = $"{scenes[i]}.unity";
                }

                var report =
                    BuildPipeline.BuildPlayer(scenes, $"{directory}/{buildName}", buildTarget, BuildOptions.None);

                if (report.summary.result == BuildResult.Failed)
                {
                    Debug.LogError($"Failed to build '{buildName}' to '{directory}'.");
                    return false;
                }

                Debug.Log($"Deployed to {buildTarget}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed building {buildName} for {buildTarget}. ERROR:\n{e}");
                return false;
            }
        }

        /// <summary>
        /// Zips a folder
        /// </summary>
        /// <param name="path">Path for the folder</param>
        /// <returns>True if zipped successfully</returns>
        private static bool Zip(string path)
        {
            try
            {
                if (File.Exists($"{path}.zip"))
                    File.Delete($"{path}.zip");

                ZipFile.CreateFromDirectory(path, $"{path}.zip");

                if (!File.Exists($"{path}.zip"))
                {
                    Debug.LogError($"Couldn't create a zip file {path}.zip.");
                    return false;
                }

                Debug.Log($"Created zip file {path}.zip");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Couldn't zip folder {path}! ERROR:\n{e}");
                return false;
            }
        }

        /// <summary>
        /// Pushes a zip file to the relevant itch account and channel
        /// </summary>
        /// <param name="zipFile">Zip file to push</param>
        /// <param name="accountName">Name of the account owning the project</param>
        /// <param name="gameName">URL of the game</param>
        /// <param name="channel">Channel for the build</param>
        private static bool PushToItch(string zipFile, string accountName, string gameName, string channel)
        {
            try
            {
                var strCmdText = $"/C butler push {zipFile} {accountName}/{gameName}:{channel}";
                Process process = Process.Start("CMD.exe", strCmdText);

                while (true)
                {
                    process.Refresh();

                    if (EditorUtility.DisplayCancelableProgressBar("Pushing to itch...",
                        $"Pushing {zipFile} to {accountName}/{gameName}:{channel}", 0))
                    {
                        process.Kill();
                        break;
                    }

                    if (process.HasExited) break;
                }

                EditorUtility.ClearProgressBar();

                if (process.ExitCode != 0)
                {
                    Debug.LogError($"Couldn't push to butler! Exit code: {process.ExitCode.ToString()}. Check your username, game url, and internet connection.");
                    return false;
                }

                Debug.Log($"Pushing {zipFile} to {accountName}/{gameName}:{channel}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Couldn't push '{zipFile}' to '{accountName}/{gameName}:{channel}'! ERROR:\n{e}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sets the _deployPath variable
        /// </summary>
        /// <returns>True if a path is selected. False otherwise.</returns>
        private static string GetDeployPath()
        {
            _deployPath =
                EditorUtility.OpenFolderPanel("Select relevant folder", $"{Application.dataPath}/Build",
                    Application.productName);

            return _deployPath;
        }

        /// <summary>
        /// Saves a report
        /// </summary>
        /// <param name="report">Report to save. If null will be used static _report</param>
        /// <param name="path">File path for the report. Asks in a window if null</param>
        [ContextMenu("Save Report")]
        public static void SaveReport(string report = "", string path = "")
        {
            var reportToSave = string.IsNullOrEmpty(report) ? _report : report;
            if (string.IsNullOrEmpty(reportToSave))
            {
                Debug.Log("Report is empty, nothing to save");
                return;
            }

            var fileName = !string.IsNullOrEmpty(path)
                ? path
                : EditorUtility.SaveFilePanel("Save report", _deployPath, "Report", "txt");

            if (string.IsNullOrEmpty(fileName)) return;

            try
            {
                File.WriteAllText(fileName, reportToSave);
                Debug.Log($"Report saved to {fileName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Couldn't save a report to {fileName}. ERROR:\n{e}");
            }
        }
    }
}