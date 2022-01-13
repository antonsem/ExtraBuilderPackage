using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ExtraTools.ExtraBuilder
{
    [CustomEditor(typeof(BuildSettings))]
    public class BuildSettingsEditor : Editor
    {
        private const BindingFlags _flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static |
                                            BindingFlags.Public;

        private SerializedProperty _batchFile;
        private SerializedProperty _batchArguments;
        private SerializedProperty _createZipFile;
        private SerializedProperty _useBatchFile;
        private SerializedProperty _zipFile;
        private SerializedProperty _buildDirectory;
        private SerializedProperty _keepBuildTarget;
        private SerializedProperty _automaticallySaveReport;
        private SerializedProperty _report;
        private bool _help = false;
        private bool _arguments = false;
        private string _compiledBatchFile;

        private void OnEnable()
        {
            _batchFile = serializedObject.FindProperty("batchFile");
            _batchArguments = serializedObject.FindProperty("batchArguments");
            _createZipFile = serializedObject.FindProperty("createZipFile");
            _useBatchFile = serializedObject.FindProperty("useBatchFile");
            _zipFile = serializedObject.FindProperty("zipFile");
            _keepBuildTarget = serializedObject.FindProperty("keepCurrentBuildTarget");
            _buildDirectory = serializedObject.FindProperty("buildDirectory");
            _automaticallySaveReport = serializedObject.FindProperty("automaticallySaveReport");
            _report = serializedObject.FindProperty("report");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            _createZipFile.boolValue = EditorGUILayout.Toggle(_createZipFile.displayName, _createZipFile.boolValue);

            if (_createZipFile.boolValue)
            {
                if (string.IsNullOrEmpty(_zipFile.stringValue))
                {
                    _zipFile.stringValue = "FullPath/ExampleBuild.zip";
                }
                
                EditorGUILayout.LabelField($"Last zip file was at '{_zipFile.stringValue}'");
            }

            EditorGUILayout.Space(25);

            _useBatchFile.boolValue = EditorGUILayout.Toggle(_useBatchFile.displayName, _useBatchFile.boolValue);

            if (_useBatchFile.boolValue)
            {
                EditorGUILayout.PropertyField(_batchFile);

                var batchFile = target.GetType().GetField("batchFile", _flags).GetValue(target);

                if (batchFile as DefaultAsset != null)
                {
                    EditorGUILayout.PropertyField(_batchArguments);

                    DrawHelp(batchFile as DefaultAsset);
                }
                else if(GUILayout.Button("Create default PushToItch.bat file"))
                {
                    target.GetType().GetField("batchFile", _flags).SetValue(target, BuilderHelper.CreateDefaultBatFile());
                }
            }

            EditorGUILayout.Space(25);

            EditorGUILayout.PropertyField(_keepBuildTarget);
            EditorGUILayout.PropertyField(_automaticallySaveReport);
            
            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Build"))
            {
                var rep = (target as BuildSettings).Build(keepBuildTarget:_keepBuildTarget.boolValue);
                // At some point during the build process the scriptable object deserializes, and values
                // retrieved with FindProperty becoming stale. So we need to get them in some other way
                var reportField = target.GetType().GetField("automaticallySaveReport", _flags);
                var autoSave = (bool)reportField.GetValue(target);

                if (!autoSave || string.IsNullOrEmpty(rep))
                {
                    return;
                }
                
                var deployField = target.GetType().GetField("buildDirectory", _flags);
                var deployPath = (string)deployField.GetValue(target);
                BuilderHelper.SaveReport(rep, $"{deployPath}/Report.txt");
                return;
            }

            if (string.IsNullOrEmpty(_report.stringValue))
            {
                return;
            }

            if (GUILayout.Button("Save Report"))
            {
                BuilderHelper.SaveReport(_report.stringValue);
            }
        }

        /// <summary>
        /// Draws the help boxes. Has an option to "compile" the batch file.
        /// </summary>
        /// <param name="batchFile">Batch file to be executed</param>
        private void DrawHelp(Object batchFile)
        {
            _help = EditorGUILayout.Foldout(_help, "Help");

            if (!_help)
                return;

            EditorGUILayout.HelpBox(
                $"Arguments to pass to the batch file.\nFirst argument will be used instead %1 in the '{batchFile.name}.bat' file, second for %2 etc.\nTo use BuildSettings fields as arguments add % sign before (e.g. %zipFile to pass the path of the zip file)",
                MessageType.Info);

            _arguments = EditorGUILayout.Foldout(_arguments, "Available Arguments");
            if (_arguments)
            {
                EditorGUILayout.HelpBox("%buildName\n%directory\n%buildGroup\n%buildTarget\n%scenes\n%batchFile\n%batchArguments\n%createZipFile\n%useBatchFile\n%keepCurrentBuildTarget\n%zipFile\n%buildDirectory",
                    MessageType.None);
            }

            EditorGUILayout.Space();
            
            if (!string.IsNullOrEmpty(_compiledBatchFile))
            {
                EditorGUILayout.LabelField($"Final '{batchFile.name}.bat' file:");
                EditorGUILayout.HelpBox(_compiledBatchFile, MessageType.None);
            }

            if (GUILayout.Button(string.IsNullOrEmpty(_compiledBatchFile) ? "Compile" : "Recompile"))
            {
                _compiledBatchFile = CompileBatFile(batchFile,
                    target.GetType().GetField("batchArguments", _flags).GetValue(target) as string[]);
            }
        }

        /// <summary>
        /// Inserts arguments to the batch file and returns in as string.
        /// </summary>
        /// <param name="batchFile">Batch file to insert arguments into.</param>
        /// <param name="args">Arguments for the batch file.</param>
        /// <returns>Batch script with inserted arguments.</returns>
        private string CompileBatFile(Object batchFile, IReadOnlyList<string> args)
        {
            var folders = Application.dataPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToList();
            folders.RemoveAt(folders.Count - 1);
            var assetsPath = string.Join(Path.DirectorySeparatorChar.ToString(), folders);
            var batchFilePath = AssetDatabase.GetAssetPath(batchFile);
            var filePath = $"{assetsPath}/{batchFilePath}";
            var batch = File.ReadAllText(filePath);
            var parsedArguments = (target as BuildSettings).ParseArguments(args);

            for (var i = 0; i < parsedArguments.Length; i++)
            {
                batch = batch.Replace($"%{(i + 1).ToString()}",
                    string.IsNullOrEmpty(parsedArguments[i]) ? args[i] : parsedArguments[i]);
            }

            return batch;
        }
    }
}