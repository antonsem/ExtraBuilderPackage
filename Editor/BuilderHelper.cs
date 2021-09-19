using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace ExtraTools.ExtraBuilder
{
    /// <summary>
    /// A helper class to select or create the builder
    /// </summary>
    public static class BuilderHelper
    {
        /// <summary>
        /// Find all Builder assets. Offers to create one if none is found. Selects if only one is found.
        /// Opens up a window with all available builders if more then one exists in the project.
        /// </summary>
        [MenuItem("Extra Tools/Builder")]
        private static void GetBuilder()
        {
            var builders = AssetDatabase.FindAssets("t:Builder");

            for (int i = 0; i < builders.Length; i++)
            {
                builders[i] = AssetDatabase.GUIDToAssetPath(builders[i]);
            }

            if (builders.Length == 0)
            {
                bool answer = EditorUtility.DisplayDialog("No builder assets in project!",
                    $"Could not find any asset of type 'Builder' in project. Do you want to create one?", "Sure",
                    "Nah, I'm OK");

                if (answer)
                {
                    CreateBuilderAsset();
                }

                return;
            }

            if (builders.Length == 1)
            {
                SelectObject(builders[0]);
                return;
            }
            
            SelectBuilder.ShowWindow(builders);
        }
        
        private static void CreateBuilderAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Cool, let's make one!", "Builder", "asset",
                "Bestow a name upon thy creation!");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            Builder asset = ScriptableObject.CreateInstance<Builder>();

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            SelectObject(asset);
        }

        /// <summary>
        /// Selects an asset at the given path
        /// </summary>
        /// <param name="obj">Path of the asset</param>
        public static void SelectObject(string obj)
        {
            var finder = AssetDatabase.LoadAssetAtPath<Builder>(obj);
            SelectObject(finder);
        }
        
        /// <summary>
        /// Selects a given asset
        /// </summary>
        /// <param name="obj">Asset to select</param>
        private static void SelectObject(Object obj)
        {
            if (obj == null)
            {
                return;
            }

            Selection.activeObject = obj;
            Selection.selectionChanged.Invoke();
            EditorGUIUtility.PingObject(obj);
        }
        
        /// <summary>
        /// Switches build group and the build target.
        /// </summary>
        /// <returns>Report of the process</returns>
        public static string SwitchBuildTarget(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            StringBuilder report = new StringBuilder($"---Switching build target to {buildTarget}\n");

            bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);

            stopwatch.Stop();
            report.Append($"---Build target {(switched ? "switched" : "failed to switch")} " +
                          $"at {DateTime.Now.ToString()} in {stopwatch.ElapsedMilliseconds / 1000}s");

            if (!switched)
            {
                Debug.LogError($"Couldn't switch back to the original build target '{buildTarget}'");
            }

            return report.ToString();
        }
        
        /// <summary>
        /// Saves a report.
        /// </summary>
        /// <param name="report">Report to save.</param>
        /// <param name="path">File path for the report. Asks in a window if null.</param>
        [ContextMenu("Save Report")]
        public static void SaveReport(string report, string path = "")
        {
            if (string.IsNullOrEmpty(report))
            {
                Debug.Log("Report is empty, nothing to save");
                return;
            }

            var fileName = !string.IsNullOrEmpty(path)
                ? path
                : EditorUtility.SaveFilePanel("Save report", $"{Application.dataPath}/Build", "Report", "txt");

            if (string.IsNullOrEmpty(fileName)) return;

            try
            {
                File.WriteAllText(fileName, report);
                Debug.Log($"Report saved to {fileName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Couldn't save a report to {fileName}. ERROR:\n{e}");
            }
        }
        
        /// <summary>
        /// Get a path from the folder panel.
        /// </summary>
        public static string GetDeployPath()
        {
            return EditorUtility.OpenFolderPanel("Select relevant folder", $"{Application.dataPath}/Build",
                Application.productName);
        }
    }
}