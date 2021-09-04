using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ExtraTools.ExtraBuilder
{
    /// <summary>
    /// This class is only useful if you want to have an option to create a build from an individual itch setting.
    /// If you ignore this class you can get a build for all itch settings with a context menu on a scriptable object.
    /// </summary>
    [CustomEditor(typeof(Builder))]
    public class BuilderEditor : UnityEditor.Editor
    {
        private const BindingFlags _flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;
        private bool _showButtons = false;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(15);

            var builder = (Builder)target;

            _showButtons = EditorGUILayout.Foldout(_showButtons, "Individual Builds");
            if (_showButtons)
            {
                var settings = target.GetType().GetField("itchSettings", _flags);
                var list = (Builder.ItchSettings[])settings.GetValue(target);

                foreach (var item in list)
                {
                    if (GUILayout.Button($"Build {item.channelName}"))
                    {
                        item.Build();
                    }
                }
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Build All"))
            {
                builder.BuildAll();
            }
            
            var reportField = target.GetType().GetField("_report", _flags);
            var report = (string)reportField.GetValue(target);
            if (string.IsNullOrEmpty(report)) return;

            if (GUILayout.Button("Save report"))
                Builder.SaveReport();
        }
    }
}