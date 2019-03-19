// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru
// MIT License

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
        public static bool drawAllPaths;

        #endregion

        #region Save & Load

        private static void Load()
        {
            showTweenProperties = EditorPrefs.GetBool("TSS_showTweenProperties", showTweenProperties);

            drawAllPaths = EditorPrefs.GetBool("TSS_drawAllPaths", drawAllPaths);

            prefsLoaded = true;
        }

        private static void Save()
        {
            EditorPrefs.SetBool("TSS_showTweenProperties", showTweenProperties);
            EditorPrefs.SetBool("TSS_drawAllPaths", drawAllPaths);
        }

        #endregion

        #region GUI

        [PreferenceItem("TSS")]
        public static void PreferencesGUI()
        {
            if (!prefsLoaded) Load();

            EditorGUILayout.LabelField("Version: " + TSSInfo.version);

            TSSEditorUtils.DrawGenericProperty(ref showTweenProperties, "showTweenProperties");

            TSSEditorUtils.DrawGenericProperty(ref drawAllPaths, "showAllPaths");

            if (GUI.changed) 
            {
                Save();

                TSSPrefs.Save();
            }

        }

        #endregion
    }
}
