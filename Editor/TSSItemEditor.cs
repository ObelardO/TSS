// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru/tss
// MIT License

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.SceneManagement;
using TSS.Base;

namespace TSS.Editor
{
    #region Enumerations

    public enum ItemLoopModePattern
    {
        Yoyo, YoyoBranch, Restart, RestartBranch, None
    }

    #endregion

    [CustomEditor(typeof(TSSItem)), CanEditMultipleObjects]
    public class TSSItemEditor : UnityEditor.Editor
    {
        #region Properties

        private TSSItem item;
        private SerializedObject serializedItem;
        private bool itemConnected;

        private static GUIContent invalidPropertyName = new GUIContent("Invalid property \"{0}\""),
                                    openOnTimeLineButton = new GUIContent("timeLine"),
                                    takeOfTimeLineButton = new GUIContent("no timeline"),
                                    applyProfileButton = new GUIContent("Apply to", "Update profile values by this item"),
                                    revertProfileButton = new GUIContent("Revert from", "Update item values by this profile"),

                                    openedState = new GUIContent("OPENED"),
                                    closedState = new GUIContent("CLOSED"),
                                    openingState = new GUIContent("OPENING"),
                                    closingState = new GUIContent("CLOSING"),
                                    timelineState = new GUIContent("TIMELINE"),
                                    mixedState = new GUIContent("MIXED"),

                                    rotationMaskContent = new GUIContent("Rotation Mask"),

                                    confirmRevertTitle = new GUIContent("Confirm reverting"),
                                    confirmRevertMessage = new GUIContent("Revert values from \"{0}\" profile?");

        private static AnimBool foldOutEvents,
                                foldOutAdvanced,
                                foldOutTweens;

        private string[] toolBarEventsTitle = new string[] { "Closed", "Opening", "Opened", "Closing", "All" };

        private static int toolBarEventsID = 0;

        private bool lastItemState;

        #endregion

        #region Init & Destroy

        private void OnEnable()
        {
            TSSPrefsEditor.Load();

            item = (TSSItem)target;
            serializedItem = new SerializedObject(item);

            EditorUtility.SetDirty(item);
            lastItemState = item.enabled;

            if (!itemConnected)
            {
                EditorApplication.hierarchyChanged += item.Refresh;
                itemConnected = true;
            }

            if (foldOutEvents == null) foldOutEvents = new AnimBool(false);
            foldOutEvents.valueChanged.AddListener(Repaint);

            if (foldOutAdvanced == null) foldOutAdvanced = new AnimBool(false);
            foldOutAdvanced.valueChanged.AddListener(Repaint);

            if (foldOutTweens == null) foldOutTweens = new AnimBool(false);
            foldOutTweens.valueChanged.AddListener(Repaint);
        }

        private void OnDestroy()
        {
            if (itemConnected)
            {
                EditorApplication.hierarchyChanged -= item.Refresh;
                itemConnected = false;
            }
        }

        #endregion

        #region Inspector GUI

        public override void OnInspectorGUI()
        {
            serializedItem.Update();

            DrawControlPanel();
            if (Selection.transforms.Length == 1) TSSTweenEditor.DrawTweensPanel(item.tweens, item, foldOutTweens, item.values);
            DrawEventsPanel();
            DrawAdvancedPanel();

            serializedItem.ApplyModifiedProperties();

            GUILayout.Space(3);

            if (lastItemState != item.enabled)
            {
                if (item.parent != null) item.parent.Refresh();
                lastItemState = item.enabled;
            }

            if (GUI.changed && !Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        #endregion

        #region Drawing & Update

        private void DrawEventsPanel()
        {
            if (Selection.transforms.Length > 1) return;

            EditorGUI.BeginDisabledGroup(Selection.transforms.Length > 1);
            GUILayout.Space(3);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            foldOutEvents.target = EditorGUILayout.Foldout(foldOutEvents.target, "   Events", true, GUI.skin.label);
            GUILayout.Space(3);

            if (foldOutEvents.faded > 0)
            {
                EditorGUILayout.BeginFadeGroup(foldOutEvents.faded);

                EditorGUILayout.BeginVertical();

                toolBarEventsID = GUILayout.Toolbar(toolBarEventsID, toolBarEventsTitle);
                EditorGUILayout.BeginVertical();

                if (toolBarEventsID == 0 || toolBarEventsID == 4) TSSEditorUtils.DrawEventProperty(serializedItem.FindProperty("OnClosed"), item.OnClosed.GetPersistentEventCount());
                if (toolBarEventsID == 1 || toolBarEventsID == 4) TSSEditorUtils.DrawEventProperty(serializedItem.FindProperty("OnOpening"), item.OnOpening.GetPersistentEventCount());
                if (toolBarEventsID == 2 || toolBarEventsID == 4) TSSEditorUtils.DrawEventProperty(serializedItem.FindProperty("OnOpened"), item.OnOpened.GetPersistentEventCount());
                if (toolBarEventsID == 3 || toolBarEventsID == 4) TSSEditorUtils.DrawEventProperty(serializedItem.FindProperty("OnClosing"), item.OnClosing.GetPersistentEventCount());

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndFadeGroup();
            }

            EditorGUILayout.EndVertical();
            EditorGUI.EndDisabledGroup();
        }

        private void DrawControlPanel()
        {
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            if (SelectedItemsStateIsIdentical(item.state))
            {
                switch (item.state)
                {
                    case ItemState.closed:
                        GUI.color = Color.red;
                        EditorGUILayout.LabelField(closedState, EditorStyles.boldLabel, TSSEditorUtils.max80pxWidth);
                        break;

                    case ItemState.opening:
                        GUI.color = Color.yellow;
                        EditorGUILayout.LabelField(openingState, EditorStyles.boldLabel, TSSEditorUtils.max80pxWidth);
                        break;

                    case ItemState.opened:
                        GUI.color = Color.green;
                        EditorGUILayout.LabelField(openedState, EditorStyles.boldLabel, TSSEditorUtils.max80pxWidth);
                        break;

                    case ItemState.closing:
                        GUI.color = Color.yellow;
                        EditorGUILayout.LabelField(closingState, EditorStyles.boldLabel, TSSEditorUtils.max80pxWidth);
                        break;

                    case ItemState.slave:
                        GUI.color = Color.cyan;
                        EditorGUILayout.LabelField(timelineState, EditorStyles.boldLabel, TSSEditorUtils.max80pxWidth);
                        break;
                }
            }
            else
            {
                GUI.color = Color.grey;
                EditorGUILayout.LabelField(mixedState, EditorStyles.boldLabel, TSSEditorUtils.max80pxWidth);
            }

            GUI.color = TSSEditorUtils.redColor;
            if (GUILayout.Button(TSSEditorTextures.itemRecordClose, EditorStyles.miniButtonLeft, TSSEditorUtils.fixedLineHeight, TSSEditorUtils.max40pxWidth))
            {
                Undo.RecordObject(item, "[TSS Item] recording closed state");
                item.Capture(ItemKey.closed); InvokeItemMethod("CloseImmediately");
            }

            GUI.color = TSSEditorUtils.greenColor;
            if (GUILayout.Button(TSSEditorTextures.itemRecordOpen, EditorStyles.miniButtonRight, TSSEditorUtils.fixedLineHeight, TSSEditorUtils.max40pxWidth))
            {
                Undo.RecordObject(item, "[TSS Item] recording closed state");
                item.Capture(ItemKey.opened); InvokeItemMethod("OpenImmediately");
            }

            GUI.color = TSSEditorUtils.redColor;
            if (GUILayout.Button(TSSEditorTextures.itemClose, EditorStyles.miniButtonLeft, TSSEditorUtils.fixedLineHeight, TSSEditorUtils.max40pxWidth))
            {
                Undo.RecordObject(item, "[TSS Item] to closed state");
                if (Application.isPlaying) InvokeItemMethod("CloseBranch"); else InvokeItemMethod("CloseBranchImmediately");
            }

            GUI.color = TSSEditorUtils.greenColor;
            if (GUILayout.Button(TSSEditorTextures.itemOpen, EditorStyles.miniButtonRight, TSSEditorUtils.fixedLineHeight, TSSEditorUtils.max40pxWidth))
            {
                Undo.RecordObject(item, "[TSS Item] to opened state");
                if (Application.isPlaying) InvokeItemMethod("OpenBranch"); else InvokeItemMethod("OpenBranchImmediately");
            }

            GUI.color = Color.white;

            if (item != TSSTimeLineEditor.item && GUILayout.Button(openOnTimeLineButton, TSSEditorUtils.fixedLineHeight, TSSEditorUtils.max80pxWidth))
            {
                EditorWindow window = EditorWindow.GetWindow(typeof(TSSTimeLineEditor), false);
                if (TSSTimeLineEditor.item == null) TSSTimeLineEditor.mode = TSSTimeLineEditor.Mode.open;
                TSSTimeLineEditor.item = item;
                window.titleContent.text = "TSS TimeLine";
                item.state = ItemState.slave;

            }

            if (item == TSSTimeLineEditor.item && GUILayout.Button(takeOfTimeLineButton, TSSEditorUtils.fixedLineHeight, TSSEditorUtils.max80pxWidth))
            {
                TSSTimeLineEditor.item = null;
                TSSItemBase.Activate(item, ActivationMode.closeBranchImmediately);
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(3);

            EditorGUILayout.BeginVertical();

            GUILayout.Space(3);

            EditorGUI.BeginDisabledGroup(item.parentChainMode);

            EditorGUILayout.BeginHorizontal();
            DrawItemProperty(item, "openDelay");
            DrawItemProperty(item, "closeDelay");
            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginHorizontal();
            DrawItemProperty(item, "openDuration");
            DrawItemProperty(item, "closeDuration");
            EditorGUILayout.EndHorizontal();

            bool ignoreChilds = item.ignoreChilds && ValuesIsIdentical(GetSelectedItemsValues<bool>("ignoreChilds"));

            //if (ignoreChilds || item.childItems.Count == 0) { EditorGUILayout.EndVertical(); return; }

            EditorGUILayout.BeginHorizontal();
            DrawItemProperty(item, "openChildBefore");
            DrawItemProperty(item, "closeChildBefore");
            EditorGUILayout.EndHorizontal();

            bool chainMode = false;

            EditorGUILayout.BeginHorizontal();
            DrawItemProperty(item, "childChainMode");
            chainMode = (item.childChainMode && ValuesIsIdentical(GetSelectedItemsValues<bool>("childChainMode")));
            if (chainMode) DrawItemProperty(item, "brakeChainDelay");
            EditorGUILayout.EndHorizontal();

            if (chainMode)
            {
                EditorGUILayout.BeginHorizontal();
                DrawItemProperty(item, "chainOpenDelay");
                DrawItemProperty(item, "chainCloseDelay");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                DrawItemProperty(item, "firstChildOpenDelay");
                DrawItemProperty(item, "firstChildCloseDelay");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                DrawItemProperty(item, "chainOpenDirection");
                DrawItemProperty(item, "chainCloseDirection");
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawAdvancedPanel()
        {
            GUILayout.Space(3);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            foldOutAdvanced.target = EditorGUILayout.Foldout(foldOutAdvanced.target, "   Advanced", true, GUI.skin.label);

            if (foldOutAdvanced.faded > 0)
            {
                EditorGUILayout.BeginFadeGroup(foldOutAdvanced.faded);

                GUILayout.Space(3);

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;

                bool profilePropertyIdentical = ValuesIsIdentical(GetSelectedItemsValues<TSSProfile>("profile"));

                EditorGUILayout.BeginHorizontal();
                DrawItemProperty(item, "profile");
                if (item.profile == null && profilePropertyIdentical && GUILayout.Button(TSSEditorUtils.addKeyButtonContent, TSSEditorUtils.max18pxWidth))
                {
                    item.profile = TSSProfileEditor.CreateProfileAsset();
                    TSSProfile.ProfileApply(item, item.profile);
                    Selection.SetActiveObjectWithContext(item.gameObject, item);
                }
                if (item.profile != null && profilePropertyIdentical && GUILayout.Button(TSSEditorUtils.delKeyButtonContent, TSSEditorUtils.max18pxWidth)) item.profile = null;
                EditorGUILayout.EndHorizontal();

                if (item.profile != null && profilePropertyIdentical)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(applyProfileButton))
                    {
                        Undo.RecordObject(item.profile, "[TSS Item] applying profile");
                        TSSProfile.ProfileApply(item, item.profile);
                    }

                    if (GUILayout.Button(revertProfileButton))
                    {
                        foreach (Transform itemTransform in Selection.transforms)
                        {
                            TSSItem item = itemTransform.GetComponent<TSSItem>();
                            if (item == null) continue;
                            Undo.RecordObject(item, "[TSS Item] revert profile");
                            TSSProfile.ProfileRevert(item, item.profile);
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                }

                EditorGUILayout.EndVertical();

                TSSEditorUtils.BeginBlackVertical();

                EditorGUILayout.BeginHorizontal();
                DrawItemProperty(item, "activationOpen");
                DrawItemProperty(item, "activationClose");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                DrawItemProperty(item, "activationStart");
                EditorGUILayout.BeginVertical();
                DrawItemProperty(item, "loops");
                if (item.loops != 0 && ValuesIsIdentical(GetSelectedItemsValues<int>("loops")))
                    DrawItemProperty(item, "loopMode", null, null, false, true);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

                TSSEditorUtils.BeginBlackVertical();
                EditorGUILayout.BeginHorizontal();
                DrawItemProperty(item, "ignoreChilds");
                DrawItemProperty(item, "ignoreParent");
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                TSSEditorUtils.BeginBlackVertical();
                EditorGUILayout.BeginHorizontal();
                DrawItemProperty(item, "rotationMode");
                DrawItemProperty(item, "materialMode");
                EditorGUILayout.EndHorizontal();

                if (ValuesIsIdentical(GetSelectedItemsValues<RotationMode>("rotationMode")) && item.rotationMode == RotationMode.path)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField(rotationMaskContent, GUILayout.MaxWidth(98));

                    DrawItemProperty(item, "rotationMaskX", "X", TSSEditorUtils.max18pxWidth);
                    DrawItemProperty(item, "rotationMaskY", "Y", TSSEditorUtils.max18pxWidth);
                    DrawItemProperty(item, "rotationMaskZ", "Z", TSSEditorUtils.max18pxWidth);

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    DrawItemProperty(item, "pathNormal");
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();

                TSSEditorUtils.BeginBlackVertical();
                EditorGUILayout.BeginHorizontal();
                DrawItemProperty(item, "interactions");
                DrawItemProperty(item, "blockRaycasting");
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                TSSEditorUtils.BeginBlackVertical();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                DrawItemProperty(item, "soundControl");
                if (item.soundControl && ValuesIsIdentical(GetSelectedItemsValues<bool>("soundControl"))) DrawItemProperty(item, "soundRestart");
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();
                DrawItemProperty(item, "videoControl");
                if (item.videoControl && ValuesIsIdentical(GetSelectedItemsValues<bool>("videoControl"))) DrawItemProperty(item, "videoRestart");
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                if (item.text != null)
                {
                    TSSEditorUtils.BeginBlackVertical();
                    EditorGUILayout.BeginHorizontal();
                    DrawItemProperty(item, "randomWave");
                    string floatFormat = item.floatFormat;
                    DrawItemProperty(item, "floatFormat");
                    if (floatFormat != item.floatFormat)
                    {
                        try { 0f.ToString(item.floatFormat); }
                        catch { Debug.LogWarningFormat("TSS Item \"{0}({1})\" has uncorrect float format!", item.name, item.GetInstanceID()); item.floatFormat = floatFormat; }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }

                if (item.button != null || (item.parent != null && item.parent.button != null && item.tweens.Where(t => t.enabled && t.direction == TweenDirection.Button).ToArray().Length > 0))
                {
                    TSSEditorUtils.BeginBlackVertical();

                    EditorGUILayout.BeginHorizontal();
                    DrawItemProperty(item, "buttonDuration");
                    DrawItemProperty(item, "buttonDirection");
                    EditorGUILayout.EndHorizontal();


                    if (item.button != null && Selection.transforms.Length == 1) TSSEditorUtils.DrawKeyCodeListProperty(item.values.onKeyboard, item, serializedItem.FindProperty("values").FindPropertyRelative("onKeyboard"), false);

                    EditorGUILayout.EndVertical();
                }

                TSSEditorUtils.BeginBlackVertical();
                EditorGUILayout.BeginHorizontal();
                DrawItemProperty(item, "updatingType");
                DrawItemProperty(item, "timeScaled", null, GUILayout.MaxWidth(80), true);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndFadeGroup();
            }
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Item property stuff

        private delegate void SetValue<T>(T value);
        private delegate T GetValue<T>();

        public static void DrawItemProperty(TSSItem item, string propertyName, string displayPropertyName = null, GUILayoutOption displayPropertyOption = null, bool shortCheckBox = false, bool alternative = false)
        {
            GUI.color = Color.white;
            GUI.backgroundColor = Color.white;

            if (!ItemPropertyIsValid(propertyName))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(string.Format(invalidPropertyName.text, propertyName), MessageType.Warning);
                EditorGUILayout.EndHorizontal();
                return;
            }

            if (string.IsNullOrEmpty(displayPropertyName)) displayPropertyName = propertyName;
            if (displayPropertyOption == null) displayPropertyOption = TSSEditorUtils.max100pxWidth;

            Type propertyType = GetItemPropertyType(propertyName);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(TSSText.GetHumanReadableString(displayPropertyName), displayPropertyOption);

            if (propertyType == typeof(float))
            {
                float displayedValue = GetItemValue<float>(item, propertyName);
                bool itemValuesIdentical = ValuesIsIdentical(GetSelectedItemsValues<float>(propertyName));
                if (!itemValuesIdentical) { EditorGUI.showMixedValue = true; }
                float enteredValue = Mathf.Clamp(EditorGUILayout.FloatField(displayedValue), 0, float.MaxValue);
                if (EditorGUI.EndChangeCheck()) SelectedItemsSetValue(propertyName, enteredValue);
            }
            else if (propertyType == typeof(bool))
            {
                bool displayedValue = GetItemValue<bool>(item, propertyName);
                bool itemValuesIdentical = ValuesIsIdentical(GetSelectedItemsValues<bool>(propertyName));
                if (!itemValuesIdentical) { EditorGUI.showMixedValue = true; }
                bool enteredValue = EditorGUILayout.Toggle(displayedValue, shortCheckBox ? TSSEditorUtils.max18pxWidth : TSSEditorUtils.max120pxWidth);
                if (EditorGUI.EndChangeCheck()) SelectedItemsSetValue(propertyName, enteredValue);
            }
            else if (propertyType == typeof(string))
            {
                string displayedValue = GetItemValue<string>(item, propertyName);
                bool itemValuesIdentical = ValuesIsIdentical(GetSelectedItemsValues<string>(propertyName));
                if (!itemValuesIdentical) { EditorGUI.showMixedValue = true; }
                string enteredValue = EditorGUILayout.TextField(displayedValue);
                if (EditorGUI.EndChangeCheck()) SelectedItemsSetValue(propertyName, enteredValue);
            }
            else if (propertyType == typeof(int))
            {
                int displayedValue = GetItemValue<int>(item, propertyName);
                bool itemValuesIdentical = ValuesIsIdentical(GetSelectedItemsValues<int>(propertyName));
                if (!itemValuesIdentical) { EditorGUI.showMixedValue = true; }
                int enteredValue = EditorGUILayout.IntField(displayedValue);
                if (EditorGUI.EndChangeCheck()) SelectedItemsSetValue(propertyName, enteredValue);
            }
            else if (propertyType == typeof(ChainDirection))
            {
                ChainDirection displayedValue = GetItemValue<ChainDirection>(item, propertyName);
                bool itemValuesIdentical = ValuesIsIdentical(GetSelectedItemsValues<ChainDirection>(propertyName));
                if (!itemValuesIdentical) { EditorGUI.showMixedValue = true; }
                ChainDirection enteredValue = (ChainDirection)EditorGUILayout.EnumPopup(displayedValue);
                if (EditorGUI.EndChangeCheck()) SelectedItemsSetValue(propertyName, enteredValue);
            }
            else if (propertyType == typeof(RotationMode))
            {
                RotationMode displayedValue = GetItemValue<RotationMode>(item, propertyName);
                bool itemValuesIdentical = ValuesIsIdentical(GetSelectedItemsValues<RotationMode>(propertyName));
                if (!itemValuesIdentical) { EditorGUI.showMixedValue = true; }
                RotationMode enteredValue = (RotationMode)EditorGUILayout.EnumPopup(displayedValue);
                if (EditorGUI.EndChangeCheck()) SelectedItemsSetValue(propertyName, enteredValue);
            }
            else if (propertyType == typeof(MaterialMode))
            {
                MaterialMode displayedValue = GetItemValue<MaterialMode>(item, propertyName);
                bool itemValuesIdentical = ValuesIsIdentical(GetSelectedItemsValues<MaterialMode>(propertyName));
                if (!itemValuesIdentical) { EditorGUI.showMixedValue = true; }
                MaterialMode enteredValue = (MaterialMode)EditorGUILayout.EnumPopup(displayedValue);
                if (EditorGUI.EndChangeCheck()) SelectedItemsSetValue(propertyName, enteredValue);
            }
            else if (propertyType == typeof(AnimationCurve))
            {
                AnimationCurve displayedValue = GetItemValue<AnimationCurve>(item, propertyName);
                bool itemValuesIdentical = ValuesIsIdentical(GetSelectedItemsValues<AnimationCurve>(propertyName));
                if (!itemValuesIdentical) { EditorGUI.showMixedValue = true; }
                AnimationCurve enteredValue = (AnimationCurve)EditorGUILayout.CurveField(displayedValue);
                if (EditorGUI.EndChangeCheck()) SelectedItemsSetValue(propertyName, enteredValue);

            }
            else if (propertyType == typeof(TSSProfile))
            {
                TSSProfile displayedValue = GetItemValue<TSSProfile>(item, propertyName);
                bool itemValuesIdentical = ValuesIsIdentical(GetSelectedItemsValues<TSSProfile>(propertyName));
                if (!itemValuesIdentical) { EditorGUI.showMixedValue = true; }
                TSSProfile enteredValue = (TSSProfile)EditorGUILayout.ObjectField(displayedValue, typeof(TSSProfile), false);
                if (EditorGUI.EndChangeCheck())
                {
                    SelectedItemsSetValue(propertyName, enteredValue);
                    if (enteredValue != null && EditorUtility.DisplayDialog(confirmRevertTitle.text, string.Format(confirmRevertMessage.text, item.profile.name), "Yes", "No"))
                        TSSProfile.ProfileRevert(item, item.profile);
                }
            }
            else if (propertyType == typeof(ActivationMode))
            {
                if (alternative)
                {
                    ItemLoopModePattern displayedValue = ActivationModeToLoopPattern(GetItemValue<ActivationMode>(item, propertyName));
                    bool itemValuesIdentical = ValuesIsIdentical(GetSelectedItemsValues<ActivationMode>(propertyName));
                    if (!itemValuesIdentical) { EditorGUI.showMixedValue = true; }
                    ItemLoopModePattern enteredValue = (ItemLoopModePattern)EditorGUILayout.EnumPopup(displayedValue);
                    if (EditorGUI.EndChangeCheck()) SelectedItemsSetValue(propertyName, LoopPatternToActivationMode(enteredValue));
                }
                else
                {
                    ActivationMode displayedValue = GetItemValue<ActivationMode>(item, propertyName);
                    bool itemValuesIdentical = ValuesIsIdentical(GetSelectedItemsValues<ActivationMode>(propertyName));
                    if (!itemValuesIdentical) { EditorGUI.showMixedValue = true; }
                    ActivationMode enteredValue = (ActivationMode)EditorGUILayout.EnumPopup(displayedValue);
                    if (EditorGUI.EndChangeCheck()) SelectedItemsSetValue(propertyName, enteredValue);
                }
            }
            else if (propertyType == typeof(ButtonDirection))
            {
                ButtonDirection displayedValue = GetItemValue<ButtonDirection>(item, propertyName);
                bool itemValuesIdentical = ValuesIsIdentical(GetSelectedItemsValues<ButtonDirection>(propertyName));
                if (!itemValuesIdentical) { EditorGUI.showMixedValue = true; }
                ButtonDirection enteredValue = (ButtonDirection)EditorGUILayout.EnumPopup(displayedValue);
                if (EditorGUI.EndChangeCheck()) SelectedItemsSetValue(propertyName, enteredValue);
            }
            else if (propertyType == typeof(ItemUpdateType))
            {
                ItemUpdateType displayedValue = GetItemValue<ItemUpdateType>(item, propertyName);
                bool itemValuesIdentical = ValuesIsIdentical(GetSelectedItemsValues<ItemUpdateType>(propertyName));
                if (!itemValuesIdentical) { EditorGUI.showMixedValue = true; }
                ItemUpdateType enteredValue = (ItemUpdateType)EditorGUILayout.EnumPopup(displayedValue);
                if (EditorGUI.EndChangeCheck()) SelectedItemsSetValue(propertyName, enteredValue);
            }
            else if (propertyType == typeof(PathNormal))
            {
                PathNormal displayedValue = GetItemValue<PathNormal>(item, propertyName);
                bool itemValuesIdentical = ValuesIsIdentical(GetSelectedItemsValues<PathNormal>(propertyName));
                if (!itemValuesIdentical) { EditorGUI.showMixedValue = true; }
                PathNormal enteredValue = (PathNormal)EditorGUILayout.EnumPopup(displayedValue);
                if (EditorGUI.EndChangeCheck()) SelectedItemsSetValue(propertyName, enteredValue);
            }


            EditorGUILayout.EndHorizontal();

            EditorGUI.showMixedValue = false;
        }

        private static ActivationMode LoopPatternToActivationMode(ItemLoopModePattern loopPattern)
        {
            switch (loopPattern)
            {
                case ItemLoopModePattern.None: return ActivationMode.disabled;
                case ItemLoopModePattern.Yoyo: return ActivationMode.close;
                case ItemLoopModePattern.YoyoBranch: return ActivationMode.closeBranch;
                case ItemLoopModePattern.Restart: return ActivationMode.closeImmediately;
                case ItemLoopModePattern.RestartBranch: return ActivationMode.closeBranchImmediately;
                default: return ActivationMode.disabled;
            }
        }

        private static ItemLoopModePattern ActivationModeToLoopPattern(ActivationMode activationMode)
        {
            switch (activationMode)
            {
                case ActivationMode.disabled: return ItemLoopModePattern.None;
                case ActivationMode.close: return ItemLoopModePattern.Yoyo;
                case ActivationMode.closeBranch: return ItemLoopModePattern.YoyoBranch;
                case ActivationMode.closeImmediately: return ItemLoopModePattern.Restart;
                case ActivationMode.closeBranchImmediately: return ItemLoopModePattern.RestartBranch;
                default: return ItemLoopModePattern.None;
            }
        }

        private static bool ValuesIsIdentical<T>(T[] values)
        {
            if (values.Length == 1) return true;
            for (int i = 0; i < values.Length; i++) if (!EqualityComparer<T>.Default.Equals(values[i], values[0])) return false;
            return true;
        }

        private static T[] GetSelectedItemsValues<T>(string propertyName)
        {
            List<T> results = new List<T>();
            foreach (Transform itemTransform in Selection.transforms)
            {
                TSSItem item = itemTransform.GetComponent<TSSItem>();
                if (item == null) continue;
                results.Add(GetItemValue<T>(item, propertyName));
            }

            return results.ToArray();
        }

        private static void SelectedItemsSetValue<T>(string propertyName, T value)
        {
            foreach (Transform itemTransform in Selection.transforms)
            {
                TSSItem item = itemTransform.GetComponent<TSSItem>();
                if (item == null) continue;


                Undo.RecordObject(item, "[TSS item] " + item.name + ":" + propertyName);

                SetItemValue(item, propertyName, value);
            }
        }

        private static bool ItemPropertyIsValid(string propertyName)
        {
            Type type = typeof(TSSItem);
            PropertyInfo property = type.GetProperty(propertyName);

            return property != null;
        }

        private static Type GetItemPropertyType(string propertyName)
        {
            return typeof(TSSItem).GetProperty(propertyName).PropertyType;
        }

        private static void SetItemValue<T>(TSSItem item, string propertyName, T value)
        {
            Type type = typeof(TSSItem);
            PropertyInfo property = type.GetProperty(propertyName);
            MethodInfo methodInfo = property.GetSetMethod();
            SetValue<T> setValue = (SetValue<T>)Delegate.CreateDelegate(typeof(SetValue<T>), item, methodInfo);
            setValue(value);
        }

        private static T GetItemValue<T>(TSSItem item, string propertyName)
        {
            Type type = typeof(TSSItem);
            PropertyInfo property = type.GetProperty(propertyName);
            MethodInfo methodInfo = property.GetGetMethod();
            GetValue<T> getValue = (GetValue<T>)Delegate.CreateDelegate(typeof(GetValue<T>), item, methodInfo);
            T value = getValue();

            return value;
        }

        private static void InvokeItemMethod(string methodName)
        {
            MethodInfo method = typeof(TSSItemBase).GetMethod(methodName);
            if (method == null) return;

            foreach (Transform itemTransform in Selection.transforms)
            {
                TSSItem item = itemTransform.GetComponent<TSSItem>();
                if (item == null) continue;
                method.Invoke(item, new object[1] { item });
            }
        }

        private static bool SelectedItemsStateIsIdentical(ItemState state)
        {
            if (Selection.transforms.Length == 1) return true;

            foreach (Transform itemTransform in Selection.transforms)
            {
                TSSItem item = itemTransform.GetComponent<TSSItem>();
                if (item == null) continue;
                if (item.state != state) return false;
            }

            return true;
        }

        #endregion
    }
}