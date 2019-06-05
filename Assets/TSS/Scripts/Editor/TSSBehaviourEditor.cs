// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru/tss
// MIT License

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TSS.Base;

namespace TSS.Editor
{

    [CustomEditor(typeof(TSSBehaviour))]
    public class TSSBehaviourEditor : UnityEditor.Editor
    {
        #region Inspector GUI

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField(string.Format("Updates: {0}", TSSBehaviour.updatingItemsCount));
            EditorGUILayout.LabelField(string.Format("Fixed updates: {0}", TSSBehaviour.fixedUpdatingItemsCount));
            EditorGUILayout.LabelField(string.Format("Late updates: {0}", TSSBehaviour.lateUpdateingItemsCount));
        }

        #endregion
    }
}

