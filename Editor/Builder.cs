using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

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
        #pragma warning disable 0414
        [HideInInspector, SerializeField] private List<BuildSettings> buildSettings = new List<BuildSettings>();
        [HideInInspector, SerializeField] private bool keepCurrentBuildTarget = false;
        [HideInInspector, SerializeField] private bool automaticallySaveReport = true;

        [HideInInspector, SerializeField] private string deployPath;
        [HideInInspector, SerializeField] private string report;
        #pragma warning restore 0414

        /// <summary>
        /// Executes build settings from the build settings list sequentially.
        /// </summary>
        /// <returns>Report of the whole process.</returns>
        public string BuildAll()
        {
            deployPath = BuilderHelper.GetDeployPath();
            if (string.IsNullOrEmpty(deployPath)) return "";

            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;

            StringBuilder newReport = new StringBuilder($"\n----- Build All process started at {DateTime.Now.ToString()}");
            
            foreach (var build in buildSettings)
            {
                var rep = build.Build(deployPath);
                newReport.Append($"\n\n{rep}");
            }

            if (keepCurrentBuildTarget)
            {
                var rep = BuilderHelper.SwitchBuildTarget(buildTargetGroup, buildTarget);
                newReport.Append($"\n{rep}");
            }

            newReport.Append($"\n\n-----Build All process was done at {DateTime.Now.ToString()}");
            report = newReport.ToString();
            return report;
        }
    }
}