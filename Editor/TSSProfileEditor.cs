// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru/tss
// MIT License

using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using TSS.Base;

namespace TSS.Editor
{
    [CustomEditor(typeof(TSSProfile))]
    public class TSSProfileEditor : UnityEditor.Editor
    {
        #region Properties

        private TSSProfile profile;

        private static GUIContent hlpBoxMessageNoItems = new GUIContent("There are no items attached to this profile."),
                                  applyProfileButton = new GUIContent("Apply to", "Update item values by this profile"),
                                  revertProfileButton = new GUIContent("Revert from", "Update profile values by this item"),
                                  applyAllProfileButton = new GUIContent("Apply to all", "Update all attached items values by this profile"),

                                  confirmApplyAllTitle = new GUIContent("Confirm applying"),
                                  confirmApplyAllMessage = new GUIContent("Apply values from \"{0}\" profile to all attached items? All items overrated values will be reverted to profile values."),

                                  confirmRevertTitle = new GUIContent("Confirm reverting"),
                                  confirmRevertMessage = new GUIContent("Revert values from \"{0}\" item to this profile? All profile values will be overrated by this item values.");

        private static AnimBool foldOutValues,
                        foldOutItems,
                        foldOutTweens;

        private SerializedObject serializedProfile;

        #endregion

        #region Init

        [MenuItem("Assets/Create/TSS Profile")]
        public static TSSProfile CreateProfileAsset()
        {
            return TSSEditorUtils.CreateAsset<TSSProfile>("New TSS Profile");
        }

        public void OnEnable()
        {
            profile = (TSSProfile)target;
            EditorUtility.SetDirty(profile);


            if (foldOutValues == null) foldOutValues = new AnimBool(false);
            foldOutValues.valueChanged.AddListener(Repaint);

            if (foldOutItems == null) foldOutItems = new AnimBool(false);
            foldOutItems.valueChanged.AddListener(Repaint);

            if (foldOutTweens == null) foldOutTweens = new AnimBool(false);
            foldOutTweens.valueChanged.AddListener(Repaint);
        }

        #endregion

        #region Preview Window Stuff

        private static Mesh previewMesh;
        private static Material previewMaterial;
        private static Material previewUIMaterial;
        private static float evulate = 0;

        PreviewRenderUtility previewRenderUtility;

        public override bool HasPreviewGUI()
        {
            if (previewRenderUtility == null)
            {
                previewRenderUtility = new PreviewRenderUtility();
                previewRenderUtility.camera.transform.position = new Vector3(0, 0, -9);
                previewRenderUtility.camera.transform.rotation = Quaternion.Euler(0, 0, 0);
            }

            return true;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type == EventType.Repaint)
            {
                previewRenderUtility.BeginPreview(r, background);

                Material material = profile.isUI ? previewUIMaterial : previewMaterial;

                Vector3 position = profile.values.positions[0];
                Quaternion rotation = Quaternion.identity;
                Vector3 scale = Vector3.one;

                Color closedColorAlpha = profile.values.colors[0];
                closedColorAlpha.a = profile.values.alphas[0];

                Color openedColorAlpha = profile.values.colors[1];
                closedColorAlpha.a = profile.values.alphas[1];

                if (material != null) material.SetColor("_Color", Color.white);

                for (int i = 0; i < profile.tweens.Count; i++)
                {
                    if (!profile.tweens[i].enabled) continue;

                    float tweenValue = TSSTweenBase.Evaluate(evulate, 1, profile.tweens[i].type);

                    switch (profile.tweens[i].effect)
                    {
                        case ItemEffect.transform:
                            position = Vector3.LerpUnclamped(profile.values.positions[0], profile.values.positions[1], tweenValue);
                            rotation = Quaternion.LerpUnclamped(profile.values.rotations[0], profile.values.rotations[1], tweenValue);
                            scale = Vector3.LerpUnclamped(profile.values.scales[0], profile.values.scales[1], tweenValue);
                            break;

                        case ItemEffect.position:
                            position = Vector3.LerpUnclamped(profile.values.positions[0], profile.values.positions[1], tweenValue);
                            break;

                        case ItemEffect.rotation:
                            rotation = Quaternion.LerpUnclamped(profile.values.rotations[0], profile.values.rotations[1], tweenValue);
                            break;

                        case ItemEffect.scale:
                            scale = Vector3.LerpUnclamped(profile.values.scales[0], profile.values.scales[1], tweenValue);
                            break;

                        case ItemEffect.color:
                            material.SetColor("_Color", Color.LerpUnclamped(profile.values.colors[0], profile.values.colors[1], tweenValue));
                            break;
                        case ItemEffect.alpha:

                            material.SetColor("_Color", Color.Lerp(closedColorAlpha, openedColorAlpha, tweenValue));
                            break;
                    }
                }

                position -= profile.values.positions[0];
                if (profile.isUI) position *= 0.01f;
                if (profile.isUI) previewRenderUtility.camera.orthographic = true;

                Matrix4x4 matrix4X4 = Matrix4x4.TRS(position, rotation, scale);

                previewRenderUtility.DrawMesh(previewMesh, matrix4X4, material, 0);
                previewRenderUtility.camera.Render();

                Texture resultRender = previewRenderUtility.EndPreview();
                GUI.DrawTexture(r, resultRender, ScaleMode.StretchToFill, false);
            }
        }

        void OnDestroy()
        {
            if (previewRenderUtility != null) previewRenderUtility.Cleanup();
        }

        private void OnDisable()
        {
            if (previewRenderUtility != null) previewRenderUtility.Cleanup();
        }

        private void Awake()
        {
            previewMesh = TSSEditorUtils.LoadAssetFromUniqueAssetPath<Mesh>("Library/unity default resources::Cube");
            previewMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
            previewUIMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        }

        #endregion

        #region Inspector GUI

        public override void OnInspectorGUI()
        {
            profile = (TSSProfile)target;
            serializedProfile = new SerializedObject(profile);
            serializedProfile.Update();

            evulate = EditorGUILayout.Slider(evulate, 0, 1);

            TSSTweenEditor.DrawTweensPanel(profile.tweens, profile, foldOutTweens, profile.values);
            DrawItemsPanel(TSSBehaviour.GetItems().Where(i => i.profile == profile).OrderBy(i => i.name).ToArray());
            DrawValuesPanel();

            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Drawing

        private void DrawItemsPanel(TSSItem[] items)
        {
            GUILayout.Space(3);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            foldOutItems.target = EditorGUILayout.Foldout(foldOutItems.target, "   Items (" + items.Length +")", true, GUI.skin.label);
            EditorGUILayout.EndHorizontal();

            if (foldOutItems.faded > 0)
            {
                EditorGUILayout.BeginFadeGroup(foldOutItems.faded);

                if (items.Length == 0)
                {
                    EditorGUILayout.HelpBox(hlpBoxMessageNoItems.text, MessageType.Info);
                }
                else
                {
                    for (int i = 0; i < items.Length; i++) DrawItem(items[i]);

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(applyAllProfileButton, TSSEditorUtils.fixedLineHeight))
                    {
                        if (EditorUtility.DisplayDialog(confirmApplyAllTitle.text, string.Format(confirmApplyAllMessage.text, profile.name), "Yes", "No"))
                        {
                            for (int i = 0; i < items.Length; i++)
                            {
                                Undo.RecordObject(items[i], "[TSS Profile] applying profile");
                                TSSProfile.ProfileRevert(items[i], profile);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndFadeGroup();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawItem(TSSItem item)
        {
            GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.ObjectField(item, typeof(TSSItem), true);

            if (GUILayout.Button(applyProfileButton, TSSEditorUtils.fixedLineHeight))
            {
                Undo.RecordObject(item, "[TSS Profile] applying profile");
                TSSProfile.ProfileRevert(item, item.profile);
            }
            if (GUILayout.Button(revertProfileButton, TSSEditorUtils.fixedLineHeight))
            {
                if (EditorUtility.DisplayDialog(confirmRevertTitle.text, string.Format(confirmRevertMessage.text, item.name), "Yes", "No"))
                {
                    Undo.RecordObject(item.profile, "[TSS Profile] reverting profile");
                    TSSProfile.ProfileApply(item, item.profile);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawValuesPanel()
        {
            GUILayout.Space(3);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            foldOutValues.target = EditorGUILayout.Foldout(foldOutValues.target, "   Values", true, GUI.skin.label);
            EditorGUILayout.EndHorizontal();

            if (foldOutValues.faded > 0)
            {
                EditorGUILayout.BeginFadeGroup(foldOutValues.faded);

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Start activation", TSSEditorUtils.max120pxWidth);
                profile.values.startAction = (ActivationMode)EditorGUILayout.EnumPopup(profile.values.startAction);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Activation modes", TSSEditorUtils.max120pxWidth);
                profile.values.activations[0] = (ActivationMode)EditorGUILayout.EnumPopup(profile.values.activations[0]);
                profile.values.activations[1] = (ActivationMode)EditorGUILayout.EnumPopup(profile.values.activations[1]);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Delays", TSSEditorUtils.max120pxWidth);
                profile.values.delays[0] = EditorGUILayout.FloatField(profile.values.delays[0]);
                profile.values.delays[1] = EditorGUILayout.FloatField(profile.values.delays[1]);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Durations", TSSEditorUtils.max120pxWidth);
                profile.values.durations[0] = EditorGUILayout.FloatField(profile.values.durations[0]);
                profile.values.durations[1] = EditorGUILayout.FloatField(profile.values.durations[1]);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Childs before", TSSEditorUtils.max120pxWidth);
                profile.values.childBefore[0] = EditorGUILayout.Toggle(profile.values.childBefore[0]);
                profile.values.childBefore[1] = EditorGUILayout.Toggle(profile.values.childBefore[1]);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Chain delays", TSSEditorUtils.max120pxWidth);
                profile.values.chainDelays[0] = EditorGUILayout.FloatField(profile.values.chainDelays[0]);
                profile.values.chainDelays[1] = EditorGUILayout.FloatField(profile.values.chainDelays[1]);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Chain first child delays", TSSEditorUtils.max120pxWidth);
                profile.values.firstChildDelay[0] = EditorGUILayout.FloatField(profile.values.firstChildDelay[0]);
                profile.values.firstChildDelay[1] = EditorGUILayout.FloatField(profile.values.firstChildDelay[1]);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Chain directions", TSSEditorUtils.max120pxWidth);
                profile.values.chainDirections[0] = (ChainDirection)EditorGUILayout.EnumPopup(profile.values.chainDirections[0]);
                profile.values.chainDirections[1] = (ChainDirection)EditorGUILayout.EnumPopup(profile.values.chainDirections[1]);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Rotation mode", TSSEditorUtils.max120pxWidth);
                profile.values.rotationMode = (RotationMode)EditorGUILayout.EnumPopup(profile.values.rotationMode);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Rotation mask", TSSEditorUtils.max120pxWidth);
                profile.values.rotationMask = EditorGUILayout.Vector3Field(string.Empty, profile.values.rotationMask);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Material mode", TSSEditorUtils.max120pxWidth);
                profile.values.materialMode = (MaterialMode)EditorGUILayout.EnumPopup(profile.values.materialMode);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Chain mode", TSSEditorUtils.max120pxWidth);
                profile.values.childChainMode = EditorGUILayout.Toggle(profile.values.childChainMode);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Ignore childs", TSSEditorUtils.max120pxWidth);
                profile.values.ignoreChilds = EditorGUILayout.Toggle(profile.values.ignoreChilds);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Ignore parent", TSSEditorUtils.max120pxWidth);
                profile.values.ignoreParent = EditorGUILayout.Toggle(profile.values.ignoreParent);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Break chain delays", TSSEditorUtils.max120pxWidth);
                profile.values.brakeChainDelay = EditorGUILayout.Toggle(profile.values.brakeChainDelay);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Interactions", TSSEditorUtils.max120pxWidth);
                profile.values.interactions = EditorGUILayout.Toggle(profile.values.interactions);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Block raycastring", TSSEditorUtils.max120pxWidth);
                profile.values.blockRaycasting = EditorGUILayout.Toggle(profile.values.blockRaycasting);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Sound control", TSSEditorUtils.max120pxWidth);
                profile.values.soundControl = EditorGUILayout.Toggle(profile.values.soundControl);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Sound restart", TSSEditorUtils.max120pxWidth);
                profile.values.soundRestart = EditorGUILayout.Toggle(profile.values.soundRestart);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Video control", TSSEditorUtils.max120pxWidth);
                profile.values.videoControl = EditorGUILayout.Toggle(profile.values.videoControl);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Video restart", TSSEditorUtils.max120pxWidth);
                profile.values.videoRestart = EditorGUILayout.Toggle(profile.values.videoRestart);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Float format", TSSEditorUtils.max120pxWidth);
                profile.values.floatFormat = EditorGUILayout.TextField(profile.values.floatFormat);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Text typing wave", TSSEditorUtils.max120pxWidth);
                profile.values.randomWaveLength = EditorGUILayout.IntField(profile.values.randomWaveLength);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Button duration", TSSEditorUtils.max120pxWidth);
                profile.values.buttonDuration = EditorGUILayout.FloatField(profile.values.buttonDuration);
                EditorGUILayout.EndHorizontal();

                TSSEditorUtils.DrawKeyCodeListProperty(profile.values.onKeyboard, profile, true);

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Loops", TSSEditorUtils.max120pxWidth);
                profile.values.loops = EditorGUILayout.IntField(profile.values.loops);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Loop mode", TSSEditorUtils.max120pxWidth);
                profile.values.loopMode = (ActivationMode)EditorGUILayout.EnumPopup(profile.values.loopMode);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Positions", TSSEditorUtils.max120pxWidth);
                profile.values.positions[0] = EditorGUILayout.Vector3Field(string.Empty, profile.values.positions[0]);
                profile.values.positions[1] = EditorGUILayout.Vector3Field(string.Empty, profile.values.positions[1]);
                EditorGUILayout.EndVertical();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Quaternion", TSSEditorUtils.max120pxWidth);
                profile.values.rotations[0] = Quaternion.Euler(EditorGUILayout.Vector3Field("", profile.values.rotations[0].eulerAngles));
                profile.values.rotations[1] = Quaternion.Euler(EditorGUILayout.Vector3Field("", profile.values.rotations[1].eulerAngles));
                EditorGUILayout.EndVertical();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Euler angles", TSSEditorUtils.max120pxWidth);
                profile.values.eulerRotations[0] = EditorGUILayout.Vector3Field(string.Empty, profile.values.eulerRotations[0]);
                profile.values.eulerRotations[1] = EditorGUILayout.Vector3Field(string.Empty, profile.values.eulerRotations[1]);
                EditorGUILayout.EndVertical();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Scales", TSSEditorUtils.max120pxWidth);
                profile.values.scales[0] = EditorGUILayout.Vector3Field(string.Empty, profile.values.scales[0]);
                profile.values.scales[1] = EditorGUILayout.Vector3Field(string.Empty, profile.values.scales[1]);
                EditorGUILayout.EndVertical();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Colors", TSSEditorUtils.max120pxWidth);
                profile.values.colors[0] = EditorGUILayout.ColorField("", profile.values.colors[0]);
                profile.values.colors[1] = EditorGUILayout.ColorField("", profile.values.colors[1]);
                EditorGUILayout.EndVertical();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Alphas", TSSEditorUtils.max120pxWidth);
                profile.values.alphas[0] = EditorGUILayout.FloatField(profile.values.alphas[0]);
                profile.values.alphas[1] = EditorGUILayout.FloatField(profile.values.alphas[1]);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Image fillings", TSSEditorUtils.max120pxWidth);
                profile.values.imageFills[0] = EditorGUILayout.FloatField(profile.values.imageFills[0]);
                profile.values.imageFills[1] = EditorGUILayout.FloatField(profile.values.imageFills[1]);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Numbers", TSSEditorUtils.max120pxWidth);
                profile.values.numbers[0] = EditorGUILayout.FloatField(profile.values.numbers[0]);
                profile.values.numbers[1] = EditorGUILayout.FloatField(profile.values.numbers[1]);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Texts", TSSEditorUtils.max120pxWidth);
                profile.values.texts[0] = EditorGUILayout.TextArea(profile.values.texts[0]);
                profile.values.texts[1] = EditorGUILayout.TextArea(profile.values.texts[1]);
                EditorGUILayout.EndVertical();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Rects", TSSEditorUtils.max120pxWidth);
                profile.values.rects[0] = EditorGUILayout.Vector4Field("", profile.values.rects[0]);
                profile.values.rects[1] = EditorGUILayout.Vector4Field("", profile.values.rects[1]);
                EditorGUILayout.EndVertical();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Anchors", TSSEditorUtils.max120pxWidth);
                profile.values.anchors[0] = EditorGUILayout.Vector4Field("", profile.values.anchors[0]);
                profile.values.anchors[1] = EditorGUILayout.Vector4Field("", profile.values.anchors[1]);
                EditorGUILayout.EndVertical();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Anchors positions", TSSEditorUtils.max120pxWidth);
                profile.values.anchorPositions[0] = EditorGUILayout.Vector2Field("", profile.values.anchorPositions[0]);
                profile.values.anchorPositions[1] = EditorGUILayout.Vector2Field("", profile.values.anchorPositions[1]);
                EditorGUILayout.EndVertical();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Intensities", TSSEditorUtils.max120pxWidth);
                profile.values.intensities[0] = EditorGUILayout.FloatField(profile.values.intensities[0]);
                profile.values.intensities[1] = EditorGUILayout.FloatField(profile.values.intensities[1]);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Light ranges", TSSEditorUtils.max120pxWidth);
                profile.values.lightRange[0] = EditorGUILayout.FloatField(profile.values.lightRange[0]);
                profile.values.lightRange[1] = EditorGUILayout.FloatField(profile.values.lightRange[1]);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Sound ranges", TSSEditorUtils.max120pxWidth);
                profile.values.soundRange[0] = EditorGUILayout.FloatField(profile.values.soundRange[0]);
                profile.values.soundRange[1] = EditorGUILayout.FloatField(profile.values.soundRange[1]);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Collider range", TSSEditorUtils.max120pxWidth);
                profile.values.sphereRange[0] = EditorGUILayout.FloatField(profile.values.sphereRange[0]);
                profile.values.sphereRange[1] = EditorGUILayout.FloatField(profile.values.sphereRange[1]);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Updating type", TSSEditorUtils.max120pxWidth);
                profile.values.updatingType = (ItemUpdateType)EditorGUILayout.EnumPopup(profile.values.updatingType);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Time scaling", TSSEditorUtils.max120pxWidth);
                profile.values.timeScaled = EditorGUILayout.Toggle(profile.values.timeScaled);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Path loop mode", TSSEditorUtils.max120pxWidth);
                profile.values.pathIsLooped = EditorGUILayout.Toggle(profile.values.pathIsLooped);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Path auto control", TSSEditorUtils.max120pxWidth);
                profile.values.pathAutoControl = EditorGUILayout.Toggle(profile.values.pathAutoControl);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Path smoothing", TSSEditorUtils.max120pxWidth);
                profile.values.pathSmoothFactor = EditorGUILayout.FloatField(profile.values.pathSmoothFactor);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Path resolution", TSSEditorUtils.max120pxWidth);
                profile.values.pathResolution = EditorGUILayout.IntField(profile.values.pathResolution);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Path spacing", TSSEditorUtils.max120pxWidth);
                profile.values.pathSpacing = EditorGUILayout.IntField(profile.values.pathSpacing);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Path normal", TSSEditorUtils.max120pxWidth);
                profile.values.pathNormal = (PathNormal)EditorGUILayout.EnumPopup(profile.values.pathNormal);
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField("Path lerping", TSSEditorUtils.max120pxWidth);
                profile.values.pathLerpMode = (PathLerpMode)EditorGUILayout.EnumPopup(profile.values.pathLerpMode);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndFadeGroup();
            }

            EditorGUILayout.EndVertical();
        }

        #endregion
    }
}