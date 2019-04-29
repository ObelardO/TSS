// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru/tss
// MIT License

using UnityEngine;
using UnityEditor;
using System;

namespace TSS.Editor
{
    [InitializeOnLoad]
    public static class TSSEditorHerarchyModifier
    {
        #region Init

        static TSSEditorHerarchyModifier()
        {
            EditorApplication.hierarchyWindowItemOnGUI =
                (EditorApplication.HierarchyWindowItemCallback)
                    Delegate.Combine(EditorApplication.hierarchyWindowItemOnGUI,
                        (EditorApplication.HierarchyWindowItemCallback)DrawHierarchyIcon);
        }

        #endregion

        #region GUI

        private static void DrawHierarchyIcon(int instanceID, Rect selectionRect)
        {
            if (TSSEditorTextures.TSSIcon == null) return;

            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (gameObject == null) return;

            var item = gameObject.GetComponent<TSSItem>();
            var core = gameObject.GetComponent<TSSCore>();

            if (item == null && core == null) return;

            var rect = new Rect(selectionRect.x + selectionRect.width - 18f, selectionRect.y, 16f, 16f);

            if (item != null && (item.isOpened || item.isOpening)) GUI.color = TSSEditorUtils.greenColor;
            if (item != null && item.isSlave) GUI.color = TSSEditorUtils.cyanColor;

            if (!gameObject.activeSelf || (item != null && !item.enabled) || (core != null && !core.enabled)) GUI.color = Color.gray;

            GUI.DrawTexture(rect, TSSEditorTextures.TSSIcon);
            GUI.color = Color.white;
        }

        #endregion

    }
}