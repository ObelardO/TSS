// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru
// MIT License

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.AnimatedValues;

namespace TSS.Editor
{
    [CustomEditor(typeof(TSSCore))]
    public class TSSCoreEditor : UnityEditor.Editor
    {
        #region Properties

        private static GUIContent newStateButtonContent = new GUIContent("+", "Add a new state to this core"),
                                  newStateBigButtonContent = new GUIContent("Add new state", "Add a new state to this core"),
                                  delStateButtonContent = new GUIContent("-", "Remove this state"),
                                  stateSetDefault = new GUIContent("set default"),
                                  stateUnsetDefault = new GUIContent("release"),
                                  propertyItem = new GUIContent("Items"),
                                  hlpBoxMessageNewState = new GUIContent("Click[+] to add first state to this scene"),
                                  confirmRemoveTitle = new GUIContent("Confirm state removing"),
                                  confirmRemoveMessage = new GUIContent("Remove \"{0}\" state?"),
                                  delItemButtonContent = new GUIContent("-", "Remove     item holder from this state"),
                                  addItemButtonContent = new GUIContent("+", "Add item holder to this state");

        private TSSCore core;
        private SerializedObject serializedCore;
        private SerializedProperty statesProperty;

        private static GUIStyle stateFoldAutStyle;
        private static AnimBool foldOutStates;

        #endregion

        #region Init

        private void OnEnable()
        {
            core = (TSSCore)target;
            serializedCore = new SerializedObject(core);
            statesProperty = serializedCore.FindProperty("states");


            if (foldOutStates == null) foldOutStates = new AnimBool(false);
            foldOutStates.valueChanged.AddListener(Repaint);

            EditorUtility.SetDirty(core);
        }

        #endregion

        #region Inspector GUI

        public override void OnInspectorGUI()
        {
            stateFoldAutStyle = new GUIStyle(GUI.skin.label);
            stateFoldAutStyle.fontStyle = FontStyle.Bold;

            serializedCore.Update();

            if (Selection.transforms.Length == 1)
            {
                DrawStatesPanel();
                DrawEventsPanel();
            }

            serializedCore.ApplyModifiedProperties();

            if (GUI.changed && !Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        #endregion

        #region Drawing states panel

        private void DrawStatesPanel()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            foldOutStates.target = EditorGUILayout.Foldout(foldOutStates.target, string.Format("   States ({0})", core.Count), true, GUI.skin.label);
            if (GUILayout.Button(newStateButtonContent, TSSEditorUtils.max18pxWidth)) { AddState(); return; }
            EditorGUILayout.EndHorizontal();

            if (foldOutStates.faded > 0)
            {
                EditorGUILayout.BeginFadeGroup(foldOutStates.faded);

                if (core.Count == 0)
                {
                    EditorGUILayout.HelpBox(hlpBoxMessageNewState.text, MessageType.Info);
                }
                else
                {
                    for (int i =  0; i < core.Count; i++) if (!DrawState(core[i], i)) break;
                }
                if (GUILayout.Button(newStateBigButtonContent)) { AddState(true); return; }

                EditorGUILayout.EndFadeGroup();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            TSSEditorUtils.DrawGenericProperty(ref core.incorrectAction, "Incorrect Name Action", core);

        }

        private void AddState(bool isLast = false)
        {
            Undo.RecordObject(core, "[TSS Core] new state");
            foldOutStates.target = true;
            core.AddState(null).editing = true;
        }

        private bool DrawState(TSSState state, int stateID)
        {
            if (state == null) return false;

            EditorGUILayout.BeginVertical();

                TSSEditorUtils.BeginBlackVertical();

                    EditorGUILayout.BeginHorizontal();

                    TSSEditorUtils.DrawGenericProperty(ref state.enabled, core);

                    state.editing = EditorGUILayout.Foldout(state.editing, string.Format("   {0} {1}", state.name, state == core.defaultState ? " (default)" : string.Empty), true, stateFoldAutStyle);

                        EditorGUI.BeginDisabledGroup(!state.enabled);

                            if (state != core.defaultState && GUILayout.Button(stateSetDefault, TSSEditorUtils.max80pxWidth, TSSEditorUtils.fixedLineHeight))
                            {
                                Undo.RecordObject(core, "[TSS Core] default state");
                                core.SetDefaultState(state);
                            }
                            else if (state == core.defaultState && GUILayout.Button(stateUnsetDefault, TSSEditorUtils.max80pxWidth, TSSEditorUtils.fixedLineHeight))
                            {
                                Undo.RecordObject(core, "[TSS Core] default state");
                                core.SetDefaultState();
                            }

                        EditorGUI.EndDisabledGroup();

                        if (DrawStateDeleteButton(state)) return false;

                    EditorGUILayout.EndHorizontal();

                    EditorGUI.BeginDisabledGroup(!state.enabled);

                    if (state.editing)
                    {
                        TSSEditorUtils.DrawGenericProperty(ref state.name, "State Name", core);

                        TSSEditorUtils.DrawSeparator();
                        DrawStateItems(state);
                        
                        TSSEditorUtils.DrawSeparator();
                        TSSEditorUtils.DrawEventProperty(statesProperty.GetArrayElementAtIndex(stateID), "onOpen", state.onOpen.GetPersistentEventCount());
                        GUILayout.Space(3);
                        TSSEditorUtils.DrawEventProperty(statesProperty.GetArrayElementAtIndex(stateID), "onClose", state.onClose.GetPersistentEventCount());

                        if (core.useInput)
                        {
                            TSSEditorUtils.DrawSeparator();
                            TSSEditorUtils.DrawKeyCodeListProperty(state.onKeyboard, core, statesProperty.GetArrayElementAtIndex(stateID).FindPropertyRelative("onKeyboard"), false);
                        }
                    }

                EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
            GUILayout.Space(3);

            return true;
        }

        private void DrawStateItems(TSSState state)
        {
            EditorGUILayout.BeginHorizontal();

                if (state.overrideModes)
                {
                    EditorGUILayout.LabelField(propertyItem, TSSEditorUtils.max120pxWidth);

                    TSSEditorUtils.DrawGenericProperty(ref state.modeOpenOverride, TSSEditorUtils.greenColor, core);
                    TSSEditorUtils.DrawGenericProperty(ref state.modeCloseOverride, TSSEditorUtils.redColor, core);
                } 
                else
                {
                    EditorGUILayout.LabelField(propertyItem);
                }

                TSSEditorUtils.DrawGenericProperty(ref state.overrideModes, core);

                if (GUILayout.Button(addItemButtonContent, TSSEditorUtils.max18pxWidth, TSSEditorUtils.fixedLineHeight))
                {
                    Undo.RecordObject(core, "[TSS Core] add new item");
                    state.AddItem(null);
                }

            EditorGUILayout.EndHorizontal();
                
            EditorGUILayout.BeginVertical();

                GUI.backgroundColor = Color.white;

                for (int i = 0; i < state.Count; i++)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                        EditorGUILayout.BeginHorizontal();

                            TSSEditorUtils.DrawGenericProperty(ref state[i].enabled, core);

                            EditorGUI.BeginDisabledGroup(!state[i].enabled);

                                TSSEditorUtils.DrawGenericProperty(ref state[i].item, core);

                                if (state[i].item != null && !state.overrideModes)
                                {
                                    TSSEditorUtils.DrawGenericProperty(ref state[i].overrideModes, core);
                                }

                            EditorGUI.EndDisabledGroup();

                            if (GUILayout.Button(delItemButtonContent, TSSEditorUtils.max18pxWidth, TSSEditorUtils.fixedLineHeight))
                            {
                                Undo.RecordObject(core, "[TSS Core] remove item");
                                state.RemoveItem(i);
                                return;
                            }

                        EditorGUILayout.EndHorizontal();

                        EditorGUI.BeginDisabledGroup(!state[i].enabled);

                            EditorGUILayout.BeginHorizontal();
                            if (!state.overrideModes && state[i].overrideModes && state[i].item != null)
                            {
                                TSSEditorUtils.DrawGenericProperty(ref state[i].mode[1], TSSEditorUtils.greenColor, core);
                                TSSEditorUtils.DrawGenericProperty(ref state[i].mode[0], TSSEditorUtils.redColor, core);
                            }
                            EditorGUILayout.EndHorizontal();

                        EditorGUI.EndDisabledGroup();

                    EditorGUILayout.EndVertical();
                }
            EditorGUILayout.EndVertical();

        }

        private bool DrawStateDeleteButton(TSSState state)
        {
            if (!GUILayout.Button(delStateButtonContent, TSSEditorUtils.max18pxWidth, TSSEditorUtils.fixedLineHeight)) return false;
            if (!EditorUtility.DisplayDialog(confirmRemoveTitle.text, string.Format(confirmRemoveMessage.text, state.name), "Yes", "No")) return false;
            Undo.RecordObject(core, "[TSS Core] remove state");
            core.RemoveState(state);
            return true;
        }

        #endregion

        #region Drawing events panel

        private void DrawEventsPanel()
        {
            TSSEditorUtils.DrawGenericProperty(ref core.useInput, "Precess input", core);
            TSSEditorUtils.DrawGenericProperty(ref core.useEvents, "Use events", core);

            if (!core.useEvents) return;

            GUILayout.Space(3);
            EditorGUILayout.PropertyField(serializedCore.FindProperty("OnStateSelected"));
            GUILayout.Space(3);
            EditorGUILayout.PropertyField(serializedCore.FindProperty("OnFirstStateSelected"));
            GUILayout.Space(3);
            EditorGUILayout.PropertyField(serializedCore.FindProperty("OnLastStateSelected"));
            GUILayout.Space(3);
            EditorGUILayout.PropertyField(serializedCore.FindProperty("OnincorrectStateSelected"));
    }

        #endregion
    }
}