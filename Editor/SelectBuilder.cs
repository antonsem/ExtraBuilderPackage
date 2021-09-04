using UnityEditor;
using UnityEngine;

namespace ExtraTools.ExtraBuilder
{
    public class SelectBuilder : EditorWindow
    {
        private static string[] _builders;
        
        public static void ShowWindow(string[] builders)
        {
            _builders = builders;
            var window = GetWindow<SelectBuilder>();
            window.titleContent = new GUIContent("Builders");
            window.Show();
        }

        private void OnGUI()
        {
            foreach (var builder in _builders)
            {
                if(!GUILayout.Button(builder)) continue;
                BuilderHelper.SelectObject(builder);
                GetWindow<SelectBuilder>().Close();
            }
        }
    }
}