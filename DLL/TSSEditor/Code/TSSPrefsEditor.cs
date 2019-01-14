using UnityEngine;
using UnityEditor;

namespace TSS.Editor
{
    public class TSSPrefsEditor
    {
        #region Properties

        private static bool prefsLoaded = false;
        private static bool foldOutEditor;
        private static bool foldOutRuntime;

        public static bool showTweenProperties;

        #endregion

        #region Save & Load

        private static void Load()
        {
            showTweenProperties = EditorPrefs.GetBool("TSS_showTweenProperties", showTweenProperties);

            prefsLoaded = true;
        }

        private static void Save()
        {
            EditorPrefs.SetBool("TSS_showTweenProperties", showTweenProperties);
        }

        #endregion

        #region GUI

        [PreferenceItem("TSS")]
        public static void PreferencesGUI()
        {
            if (!prefsLoaded) Load();

            //foldOutEditor = EditorGUILayout.Foldout(foldOutEditor, "Editor", true);

            //if (foldOutEditor)
            //{
            //EditorGUI.indentLevel += 1;

            EditorGUILayout.LabelField("Version: " + TSSInfo.version);

                TSSEditorUtils.DrawGenericProperty(ref showTweenProperties, "showTweenProperties");

                //EditorGUI.indentLevel -= 1;
            //}

            /*
            foldOutRuntime = EditorGUILayout.Foldout(foldOutRuntime, "Runtime", true);

            if (foldOutRuntime)
            {
                EditorGUI.indentLevel += 1;

                TSSEditorUtils.DrawGenericProperty(ref TSSPrefs.dynamicPathSampling, "dynamicPathSampling");

                TSSEditorUtils.DrawGenericProperty(ref TSSPrefs.Symbols.percent, "textLerpPercentSymbol");
                TSSEditorUtils.DrawGenericProperty(ref TSSPrefs.Symbols.space, "textLerpSpaceSymbol");
                TSSEditorUtils.DrawGenericProperty(ref TSSPrefs.Symbols.dot, "floatFormatDotSymbol");

                EditorGUI.indentLevel -= 1;
            }
            */

            if (GUI.changed) 
            {
                Save();

                TSSPrefs.Save();
            }

        }

        #endregion
    }
}
