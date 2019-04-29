// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru/tss
// MIT License

using UnityEngine;
using UnityEditor;

namespace TSS.Editor
{
    public class TSSEditorGlobal
    {
        #region Init

        [MenuItem("Window/TSS/About", false, 10)]
        public static void OpenGlobalWindow(MenuCommand menuCommand)
        {
            EditorWindow window = EditorWindow.GetWindow(typeof(TSSEditorGlobalWindow), true, "About TSS");

            window.maxSize = new Vector2(340f, 128f);
            window.minSize = window.maxSize;
        }

        #endregion
    }

    public class TSSEditorGlobalWindow : EditorWindow
    {
        #region GUI

        private void OnGUI()
        {
            GUI.DrawTexture(new Rect(16, 0, 128, 128), TSSEditorTextures.TSSIcon, ScaleMode.StretchToFill);

            EditorGUI.LabelField(new Rect(160, 16, 150, EditorGUIUtility.singleLineHeight), string.Format("Version: {0}", TSSInfo.version));
            EditorGUI.LabelField(new Rect(160, 64, 150, EditorGUIUtility.singleLineHeight), string.Format("Author: {0}", TSSInfo.author));
            EditorGUI.SelectableLabel(new Rect(160, 80, 150, EditorGUIUtility.singleLineHeight), string.Format(TSSInfo.email), EditorStyles.label);

        }

        #endregion
    }
}