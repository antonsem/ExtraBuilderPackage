using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
//https://forum.unity.com/threads/c-compression-zip-missing.577492/
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ExtraTools.ExtraBuilder
{
    /// <summary>
    /// Build settings
    /// </summary>
    [CreateAssetMenu(fileName = "BuildSettings", menuName = "ExtraTools/Build Settings")]
    public class BuildSettings : ScriptableObject
    {
        #pragma warning disable 0414
        
        [Tooltip(
            "The name of the executable file including extension.\ne.g. myGame.exe, myGame.app, myGame.x86_64 etc.\nField name: 'buildName'")]
        public string buildName;
        [Tooltip(
            "Relative build directory for a platform inside a master build folder.'\ne.g. 'C://.../MyGame/win' where MyGame is the master build folder, and win is the directory.\n Field name: 'directory'")]
        public string directory;
        public BuildTargetGroup buildGroup;
        public BuildTarget buildTarget;
        [Tooltip(
            "Scene paths relative to the project folder.\ne.g. 'Assets/Scenes/SampleScene' or 'Assets/Scenes/SampleScene.unity'")]
        public SceneAsset[] scenes;
        
        [HideInInspector, SerializeField] public DefaultAsset batchFile;
        [HideInInspector, SerializeField] private string[] batchArguments;
        [HideInInspector, SerializeField] private bool createZipFile = true;
        [HideInInspector, SerializeField] private bool useBatchFile = false;
        [HideInInspector, SerializeField] private bool keepCurrentBuildTarget = false;
        [HideInInspector, SerializeField] private bool automaticallySaveReport = false;
        [HideInInspector, SerializeField] private string zipFile;
        [HideInInspector, SerializeField] private string buildDirectory;
        [HideInInspector, SerializeField] private string report;
        
        #pragma warning restore 0414
        
        /// <summary>
        /// Builds the project and depending on settings zips it, and executes the batch file.
        /// </summary>
        /// <param name="path">A path for the build. If null or empty will be asked at the start of the method.</param>
        /// <param name="keepBuildTarget">Should the platform be switched back once the build is done.</param>
        /// <returns>Report of the whole process.</returns>
        public string Build(string path = "", bool keepBuildTarget = false)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = BuilderHelper.GetDeployPath();
                if (string.IsNullOrEmpty(path)) return "";
            }

            BuildTargetGroup defaultBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            BuildTarget defaultBuildTarget = EditorUserBuildSettings.activeBuildTarget;

            buildDirectory = $"{path}/{directory}";
            var newReport = new StringBuilder($"\n----- Starting '{buildName}' build at {DateTime.Now}\n" +
                                           $"- Directory: {buildDirectory}\n- Build group: {buildGroup}\n" +
                                           $"- Build target: {buildTarget}");
            
            Stopwatch stopwatch = Stopwatch.StartNew();
            var deployed = Deploy(scenes, buildDirectory, buildName, buildGroup, buildTarget);

            stopwatch.Stop();
            newReport.Append(
                $"\n--- Build {(deployed ? "completed" : "failed")} at {DateTime.Now.ToString()} in {stopwatch.ElapsedMilliseconds / 1000} seconds");
            
            if (!deployed)
            {
                if (keepBuildTarget)
                {
                    var targetReport = BuilderHelper.SwitchBuildTarget(defaultBuildTargetGroup, defaultBuildTarget);
                    newReport.Append($"\n-- {targetReport}");
                }
                newReport.Append($"\n----- Done at {DateTime.Now.ToString()}");
                report = newReport.ToString(); 
                return report;
            }

            if (createZipFile)
            {
                newReport.Append($"\n--- Zipping {buildDirectory} at {DateTime.Now.ToString()}");
            
                stopwatch.Restart();
                var zipped = Zip(buildDirectory);
                stopwatch.Stop();
                zipFile = zipped ? $"{buildDirectory}.zip" : "";
                newReport.Append($"\n--- {(zipped ? "Zipped" : "Failed to zip")} to '{zipFile}' " +
                                 $"at {DateTime.Now.ToString()} in {stopwatch.ElapsedMilliseconds / 1000} seconds");
            
                if (!zipped)
                {
                    if (keepBuildTarget)
                    {
                        var targetReport = BuilderHelper.SwitchBuildTarget(defaultBuildTargetGroup, defaultBuildTarget);
                        newReport.Append($"\n-- {targetReport}");
                    }
                    newReport.Append($"\n----- Done at {DateTime.Now.ToString()}");
                    report = newReport.ToString(); 
                    return report;
                }
            }
            else
            {
                newReport.Append("\n---Skipped zipping");
            }
            

            if (useBatchFile)
            {
                newReport.Append($"\n--- Executing batch file '{AssetDatabase.GetAssetPath(batchFile)}' " +
                                 $"at {DateTime.Now.ToString()}");
                stopwatch.Restart();
                var executed = PrepareAndExecuteBatchFile();
                stopwatch.Stop();
                newReport.Append($"\n--- Batch file {(executed ? "was executed" : "failed execution")} " +
                                 $"at {DateTime.Now.ToString()} in {stopwatch.ElapsedMilliseconds / 1000} seconds");
            }
            else
            {
                newReport.Append("\n---Skipped batch execution");
            }
            
            
            if (keepBuildTarget)
            {
                var targetReport = BuilderHelper.SwitchBuildTarget(defaultBuildTargetGroup, defaultBuildTarget);
                newReport.Append($"\n-- {targetReport}");
            }
            newReport.Append($"\n----- Done at {DateTime.Now.ToString()}");

            report = newReport.ToString(); 
            return report;
        }

        /// <summary>
        /// Formats arguments and calls ExecuteBatchFile()
        /// </summary>
        /// <returns>Result of ExecuteBatchFile()</returns>
        private bool PrepareAndExecuteBatchFile()
        {
            var bat = AssetDatabase.GetAssetPath(batchFile);

            var parsedArgs = ParseArguments(batchArguments);

            var args = string.Join(" ", parsedArgs);
            return ExecuteBatchFile(bat, args);
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
        private static bool Deploy(IReadOnlyList<SceneAsset> scenes, string directory, string buildName,
            BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
        {
            try
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
                Directory.CreateDirectory(directory);

                EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);

                var buildOptions = new BuildPlayerOptions
                {
                    scenes = new string[scenes.Count]
                };

                for (var i = 0; i < scenes.Count; i++)
                {
                    buildOptions.scenes[i] = AssetDatabase.GetAssetPath(scenes[i]);
                }

                buildOptions.target = buildTarget;
                buildOptions.locationPathName = $"{directory}/{buildName}";

                var report = BuildPipeline.BuildPlayer(buildOptions);

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
        /// Executes the batch file at the given address with given arguments.
        /// </summary>
        /// <param name="batchFile">Address of the batch file to be executed.</param>
        /// <param name="arguments">Arguments to pass to the batch file.</param>
        /// <returns>True if the batch file was executed successfully.</returns>
        private static bool ExecuteBatchFile(string batchFile, params string[] arguments)
        {
            try
            {
                var separator = Path.DirectorySeparatorChar;
                var folders = Application.dataPath.Split(separator, Path.AltDirectorySeparatorChar).ToList();
                folders.RemoveAt(folders.Count - 1);
                var assetsPath = string.Join(separator.ToString(), folders);
                
                batchFile = $"{assetsPath}/{batchFile}".Replace(separator, Path.AltDirectorySeparatorChar);

                var args = string.Join(" ", arguments);
                Process process = Process.Start(batchFile, args);

                while (true)
                {
                    process.Refresh();

                    if (EditorUtility.DisplayCancelableProgressBar("Executing batch file", batchFile, 0.5f))
                    {
                        process.Kill();
                        break;
                    }

                    if (process.HasExited)
                        break;
                }

                EditorUtility.ClearProgressBar();

                if (process.ExitCode != 0)
                {
                    Debug.LogError($"Error while executing '{batchFile}'! Exit code: {process.ExitCode.ToString()}.");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error while executing '{batchFile}'! ERROR:\n<color=red>{e}</color>.");
                return false;
            }

            Debug.Log($"'{batchFile}' executed successfully");
            return true;
        }

        /// <summary>
        /// Checks arguments and replaces with field values of any is passed.
        /// </summary>
        /// <param name="argumentsList">Argument list.</param>
        /// <returns>Argument list with arguments replaced with relevant field values.</returns>
        public string[] ParseArguments(IReadOnlyList<string> argumentsList)
        {
            var parsedArgs = new string[argumentsList.Count];

            for (var i = 0; i < argumentsList.Count; i++)
            {
                if (argumentsList[i][0] != '%')
                {
                    parsedArgs[i] = argumentsList[i];
                    continue;
                }

                const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance |
                                           BindingFlags.Public | BindingFlags.GetField | BindingFlags.GetProperty;

                var arg = argumentsList[i].Remove(0, 1);
                var val = GetType().GetField(arg, flags).GetValue(this);
                parsedArgs[i] = val.ToString();
            }

            return parsedArgs;
        }
    }
}