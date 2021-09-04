using UnityEditor;
using UnityEngine;

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
    }
}