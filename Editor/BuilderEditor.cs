using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ExtraTools.ExtraBuilder
{
    [CustomEditor(typeof(Builder))]
    public class BuilderEditor : Editor
    {
        private const BindingFlags _flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;
        
        private SerializedProperty _keepBuildTarget;
        private SerializedProperty _automaticallySaveReport;
        private SerializedProperty _report;

        private void OnEnable()
        {
            _keepBuildTarget = serializedObject.FindProperty("keepCurrentBuildTarget");
            _automaticallySaveReport = serializedObject.FindProperty("automaticallySaveReport");
            _report = serializedObject.FindProperty("report");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var settings = target.GetType().GetField("buildSettings", _flags);
            var list = (List<BuildSettings>)settings.GetValue(target);

            for (var i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                list[i] = EditorGUILayout.ObjectField(list[i], typeof(BuildSettings), target) as BuildSettings;

                if (GUILayout.Button("Build", GUILayout.Width(75)))
                {
                    list[i].Build(keepBuildTarget: _keepBuildTarget.boolValue);
                    return;
                }

                if (GUILayout.Button("+", GUILayout.Width(30)))
                {
                    list.Insert(i + 1, null);
                }
                
                if (GUILayout.Button("-", GUILayout.Width(30)))
                {
                    list.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (list.Count == 0)
            {
                list.Add(null);
            }

            GUILayout.Space(15);

            EditorGUILayout.PropertyField(_keepBuildTarget);
            EditorGUILayout.PropertyField(_automaticallySaveReport);
            
            if (GUILayout.Button("Build All"))
            {
                var rep = (target as Builder).BuildAll();
                
                // At some point during the build process the scriptable object deserializes, and values
                // retrieved with FindProperty becoming stale. So we need to get them in some other way
                var reportField = target.GetType().GetField("automaticallySaveReport", _flags);
                var autoSave = (bool)reportField.GetValue(target);
                
                if (autoSave && !string.IsNullOrEmpty(rep))
                {
                    var deployField = target.GetType().GetField("deployPath", _flags);
                    var deployPath = (string)deployField.GetValue(target);
                    BuilderHelper.SaveReport(rep, $"{deployPath}/Report.txt");
                }
                return;
            }
            
            if (string.IsNullOrEmpty(_report.stringValue)) return;

            if (GUILayout.Button("Save report"))
            {
                BuilderHelper.SaveReport(_report.stringValue);
            }
        }
    }
}