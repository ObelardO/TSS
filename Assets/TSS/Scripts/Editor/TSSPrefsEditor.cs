// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru/tss
// MIT License

using UnityEngine;
using UnityEditor;
using TSS.Base;

namespace TSS.Editor
{
    public class TSSPrefsEditor
    {
        #region Properties

        private static bool prefsLoaded = false;
        private static bool foldOutEditor;
        private static bool foldOutRuntime;

        public static bool showTweenProperties { get; private set; }
        public static bool showAllPaths { get; private set; }
        public static bool showBehaviour {
            get { return TSSBehaviour.showBehaviour; }
            private set { TSSBehaviour.showBehaviour = value; } }

        #endregion

        #region Save & Load

        [RuntimeInitializeOnLoadMethod]
        public static void Load()
        {
            showTweenProperties = EditorPrefs.GetBool("TSS_showTweenProperties", false);
            showAllPaths = EditorPrefs.GetBool("TSS_showAllPaths", false);
            showBehaviour = EditorPrefs.GetBool("TSS_showBehaviour", false);

            prefsLoaded = true;
        }

        private static void Save()
        {
            EditorPrefs.SetBool("TSS_showTweenProperties", showTweenProperties);
            EditorPrefs.SetBool("TSS_showAllPaths", showAllPaths);
            EditorPrefs.SetBool("TSS_showBehaviour", showBehaviour);
        }

        #endregion

        #region GUI

        [PreferenceItem("TSS")]
        public static void PreferencesGUI()
        {
            if (!prefsLoaded) Load();

            EditorGUILayout.LabelField("Version: " + TSSInfo.version);

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            showTweenProperties = EditorGUILayout.Toggle("Show Properties", showTweenProperties);
            showAllPaths = EditorGUILayout.Toggle("Show all path", showAllPaths);
            showBehaviour = EditorGUILayout.Toggle("Show behaviour object", showBehaviour);

            if (EditorGUI.EndChangeCheck()) Save();
        }

        #endregion
    }
}
