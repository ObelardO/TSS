// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru/tss
// MIT License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using TSS.Base;

namespace TSS.Editor
{

    [CustomEditor(typeof(TSSBehaviour))]
    public class TSSBehaviourEditor : UnityEditor.Editor
    {
        #region Properties

        private static TSSBehaviour behaviour;
        private static SerializedObject serializedBehaviour;

        #endregion


        #region Init

        private void OnEnable()
        {
            behaviour = (TSSBehaviour)target;
            serializedBehaviour = new SerializedObject(behaviour);
        }

        #endregion

        #region Inspector GUI

        public override void OnInspectorGUI()
        {
            /*
            serializedBehaviour.Update();

            EditorGUILayout.PropertyField(serializedBehaviour.FindProperty("updatingItems"));

            serializedBehaviour.ApplyModifiedProperties();
            */
        }

        #endregion

    }
}

