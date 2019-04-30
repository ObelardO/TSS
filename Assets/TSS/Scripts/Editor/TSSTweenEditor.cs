// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru/tss
// MIT License

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using TSS.Base;

namespace TSS.Editor
{
    #region Enumerations

    public enum TweenTypeMode
    {
        In, Out, InOut, OutIn
    }

    public enum TweenTypeBase
    {
        Linear, NoneZero, NoneOne, Custom,
        Quad, Cubic, Quart, Quint, Sine, Expo, Circ, Elastic, Back, Bounce
    }

    #endregion
       
    [CustomEditor(typeof(TSSTween))]
    public class TSSTweenEditor : UnityEditor.Editor
    {
        #region Properties

        private static GUIContent blndTweenLabelContent = new GUIContent("Blend Time (sec)", "Time in seconds while tween blending between eases"),
                                  delTweenButtonContent = new GUIContent("-", "Remove this tween"),
                                  addTweenButtonContent = new GUIContent("+", "Add a new tween"),
                                  addTweenBigButtonContent = new GUIContent("Add new tween", "Add a new tween"),
                                  hlpBoxMessageNewTween = new GUIContent("Click[+] to add first tween to this item");

        #endregion

        #region Drawing

        public static void DrawTweensPanel(List<TSSTween> holder, Object parent, AnimBool foldOutTweens, TSSItemValues values)
        {
            GUILayout.Space(3);

            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            foldOutTweens.target = EditorGUILayout.Foldout(foldOutTweens.target, "   Tweens (" + holder.Count + ")", true, GUI.skin.label);

            if (GUILayout.Button(addTweenButtonContent, TSSEditorUtils.max18pxWidth))
            {
                AddTween(holder, parent);
                foldOutTweens.target = true;
            }
            EditorGUILayout.EndHorizontal();

            if (foldOutTweens.faded > 0)
            {
                EditorGUILayout.BeginFadeGroup(foldOutTweens.faded);

                if (holder.Count == 0)
                {
                    EditorGUILayout.HelpBox(hlpBoxMessageNewTween.text, MessageType.Info);
                }
                else
                {
                    for (int i = 0; i < holder.Count; i++)
                    {
                        if (!DrawTween(holder[i], holder, parent, values)) break;
                    }
                }
                if (GUILayout.Button(addTweenBigButtonContent, TSSEditorUtils.fixedLineHeight))
                {
                    AddTween(holder, parent, true);
                    foldOutTweens.target = true;
                }

                EditorGUILayout.EndFadeGroup();
            }

            EditorGUILayout.EndVertical();
        }

        private static bool DrawTween(TSSTween tween, List<TSSTween> holder, Object parent, TSSItemValues values)
        {
            GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            TSSEditorUtils.DrawGenericProperty(ref tween.enabled, parent);
            EditorGUI.BeginDisabledGroup(!tween.enabled);

            ItemEffect tweenEffect = tween.effect;
            TSSEditorUtils.DrawGenericProperty(ref tween.effect, parent);
            if (tween.effect != tweenEffect)
            {
                if (tween.effect == ItemEffect.property) { tween.matProperty = new TSSMaterialProperty(MaterialPropertyType.single); }
                if (tween.effect != ItemEffect.property) { tween.matProperty = null; tween.matPropertyType = MaterialPropertyType.single; }
            }

            EditorGUI.BeginDisabledGroup(tween.direction == TweenDirection.Button);

            if (tween.mode == TweenMode.Multiple)
            {
                EditorGUI.BeginDisabledGroup(tween.direction == TweenDirection.Close);
                TSSEditorUtils.DrawGenericProperty(ref tween.type, TSSEditorUtils.greenColor, null, parent);
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(tween.direction == TweenDirection.Open);
                TSSEditorUtils.DrawGenericProperty(ref tween.closingType, TSSEditorUtils.redColor, null, parent);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                TSSEditorUtils.DrawGenericProperty(ref tween.type, parent);
            }

            TSSEditorUtils.DrawGenericProperty(ref tween.mode, parent);

            EditorGUI.EndDisabledGroup();

            TweenDirection tweenDirection = tween.direction;
            TSSEditorUtils.DrawGenericProperty(ref tween.direction, parent);
            if (tween.direction != tweenDirection)
            {
                if (tween.direction == TweenDirection.Button)
                {
                    tween.type = TweenType.Custom;
                    tween.mode = TweenMode.Single;
                }
            }

            EditorGUI.EndDisabledGroup();

            if (DrawTweenDeleteButton(tween, holder, parent)) return false;

            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(!tween.enabled);

            GUILayout.Space(3);

            TSSEditorUtils.DrawMinMaxSliderProperty(ref tween.startPoint, ref tween.endPoint, parent);

            if (tween.mode == TweenMode.Multiple && tween.direction == TweenDirection.OpenClose)
            {
                TSSEditorUtils.DrawSliderProperty(ref tween.blendFactor, parent, blndTweenLabelContent);
            }

            if ((tween.type == TweenType.Custom && tween.direction != TweenDirection.Close)
                || (tween.mode == TweenMode.Multiple && tween.closingType == TweenType.Custom
                                                        && tween.direction != TweenDirection.Open))
            {
                EditorGUILayout.BeginHorizontal();
                TSSEditorUtils.DrawGenericProperty(ref tween.customEase, parent);
                EditorGUILayout.EndHorizontal();
            }

            if (tween.effect == ItemEffect.property)
            {

                EditorGUILayout.BeginVertical();

                EditorGUILayout.BeginHorizontal();

                MaterialPropertyType tssMatPropType = tween.matPropertyType;
                string tssMatPropName = tween.matProperty.name;
                TSSEditorUtils.DrawGenericProperty(ref tween.matPropertyType, parent);
                if (tssMatPropType != tween.matPropertyType) tween.matProperty = new TSSMaterialProperty(tween.matPropertyType);
                tween.matProperty.name = tssMatPropName;

                TSSEditorUtils.DrawGenericProperty(ref tween.matProperty.name, parent);

                EditorGUILayout.EndHorizontal();

                switch (tween.matPropertyType)
                {
                    case MaterialPropertyType.single:
                        EditorGUILayout.BeginHorizontal();
                        TSSEditorUtils.DrawGenericProperty(ref tween.matProperty.singleValues[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref tween.matProperty.singleValues[1], TSSEditorUtils.greenColor, parent);
                        EditorGUILayout.EndHorizontal();
                        break;
                    case MaterialPropertyType.integer:
                        EditorGUILayout.BeginHorizontal();
                        TSSEditorUtils.DrawGenericProperty(ref tween.matProperty.integerValues[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref tween.matProperty.integerValues[1], TSSEditorUtils.greenColor, parent);
                        EditorGUILayout.EndHorizontal();
                        break;
                    case MaterialPropertyType.color:
                        TSSEditorUtils.useHDRcolors = false;
                        EditorGUILayout.BeginHorizontal();
                        TSSEditorUtils.DrawGenericProperty(ref tween.matProperty.colorValues[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref tween.matProperty.colorValues[1], TSSEditorUtils.greenColor, parent);
                        EditorGUILayout.EndHorizontal();
                        break;
                    case MaterialPropertyType.colorHDR:
                        TSSEditorUtils.useHDRcolors = true;
                        EditorGUILayout.BeginHorizontal();
                        TSSEditorUtils.DrawGenericProperty(ref tween.matProperty.colorValues[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref tween.matProperty.colorValues[1], TSSEditorUtils.greenColor, parent);
                        EditorGUILayout.EndHorizontal();
                        break;
                    case MaterialPropertyType.vector2:
                        TSSEditorUtils.DrawGenericProperty(ref tween.matProperty.vector2values[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref tween.matProperty.vector2values[1], TSSEditorUtils.greenColor, parent);
                        break;
                    case MaterialPropertyType.vector3:
                        TSSEditorUtils.DrawGenericProperty(ref tween.matProperty.vector3values[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref tween.matProperty.vector3values[1], TSSEditorUtils.greenColor, parent);
                        break;
                    case MaterialPropertyType.vector4:
                        TSSEditorUtils.DrawGenericProperty(ref tween.matProperty.vector4values[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref tween.matProperty.vector4values[1], TSSEditorUtils.greenColor, parent);
                        break;
                    case MaterialPropertyType.curve:
                        TSSEditorUtils.DrawGenericProperty(ref tween.matProperty.curve, parent);
                        break;
                    case MaterialPropertyType.gradient:
                        TSSEditorUtils.DrawGenericProperty(ref tween.matProperty.gradient, parent);
                        break;
                }

                EditorGUILayout.EndVertical();
            }


            if (TSSPrefsEditor.showTweenProperties)
            {
                EditorGUILayout.BeginVertical();

                switch (tween.effect)
                {
                    case ItemEffect.transform:

                        
                        EditorGUILayout.LabelField("Position");
                        EditorGUILayout.BeginVertical();
                        TSSEditorUtils.DrawGenericProperty(ref values.positions[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref values.positions[1], TSSEditorUtils.greenColor, parent);
                        EditorGUILayout.EndVertical();

                        GUILayout.Space(4);

                        EditorGUILayout.LabelField("Rotation");
                        EditorGUILayout.BeginVertical();
                        TSSEditorUtils.DrawGenericProperty(ref values.eulerRotations[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref values.eulerRotations[1], TSSEditorUtils.greenColor, parent);
                        EditorGUILayout.EndVertical();

                        GUILayout.Space(4);

                        EditorGUILayout.LabelField("Scale");
                        EditorGUILayout.BeginVertical();
                        TSSEditorUtils.DrawGenericProperty(ref values.scales[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref values.scales[1], TSSEditorUtils.greenColor, parent);
                        EditorGUILayout.EndVertical();

                        break;

                    case ItemEffect.position:

                        TSSEditorUtils.DrawGenericProperty(ref values.positions[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref values.positions[1], TSSEditorUtils.greenColor, parent);

                        break;
                    case ItemEffect.rotation:

                        TSSEditorUtils.DrawGenericProperty(ref values.eulerRotations[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref values.eulerRotations[1], TSSEditorUtils.greenColor, parent);

                        break;
                    case ItemEffect.scale:

                        TSSEditorUtils.DrawGenericProperty(ref values.scales[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref values.scales[1], TSSEditorUtils.greenColor, parent);

                        break;
                    case ItemEffect.directAlpha:

                        EditorGUILayout.BeginHorizontal();
                        TSSEditorUtils.DrawGenericProperty(ref values.alphas[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref values.alphas[1], TSSEditorUtils.greenColor, parent);
                        EditorGUILayout.EndHorizontal();

                        break;
                    case ItemEffect.alpha:

                        EditorGUILayout.BeginHorizontal();
                        TSSEditorUtils.DrawGenericProperty(ref values.alphas[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref values.alphas[1], TSSEditorUtils.greenColor, parent);
                        EditorGUILayout.EndHorizontal();

                        break;
                    case ItemEffect.color:

                        EditorGUILayout.BeginHorizontal();
                        TSSEditorUtils.useHDRcolors = false;
                        TSSEditorUtils.DrawGenericProperty(ref values.colors[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref values.colors[1], TSSEditorUtils.greenColor, parent);
                        EditorGUILayout.EndHorizontal();

                        break;
                    case ItemEffect.imageFill:

                        EditorGUILayout.BeginHorizontal();
                        TSSEditorUtils.DrawGenericProperty(ref values.imageFills[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref values.imageFills[1], TSSEditorUtils.greenColor, parent);
                        EditorGUILayout.EndHorizontal();

                        break;
                    case ItemEffect.text:

                        TSSEditorUtils.DrawGenericProperty(ref values.texts[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref values.texts[1], TSSEditorUtils.greenColor, parent);

                        break;
                    case ItemEffect.number:

                        EditorGUILayout.BeginHorizontal();
                        TSSEditorUtils.DrawGenericProperty(ref values.numbers[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref values.numbers[1], TSSEditorUtils.greenColor, parent);
                        EditorGUILayout.EndHorizontal();

                        break;
                    case ItemEffect.light:
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Color", TSSEditorUtils.max80pxWidth);
                        TSSEditorUtils.DrawGenericProperty(ref values.colors[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref values.colors[1], TSSEditorUtils.greenColor, parent);
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(4);

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Intensity", TSSEditorUtils.max80pxWidth);
                        TSSEditorUtils.DrawGenericProperty(ref values.intensities[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref values.intensities[1], TSSEditorUtils.greenColor, parent);
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(4);

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Range", TSSEditorUtils.max80pxWidth);
                        TSSEditorUtils.DrawGenericProperty(ref values.lightRange[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref values.lightRange[1], TSSEditorUtils.greenColor, parent);
                        EditorGUILayout.EndHorizontal();

                        break;
                   
                    case ItemEffect.range:

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Light", TSSEditorUtils.max80pxWidth);
                        TSSEditorUtils.DrawGenericProperty(ref values.lightRange[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref values.lightRange[1], TSSEditorUtils.greenColor, parent);
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(4);

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Collider", TSSEditorUtils.max80pxWidth);
                        TSSEditorUtils.DrawGenericProperty(ref values.sphereRange[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref values.sphereRange[1], TSSEditorUtils.greenColor, parent);
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(4);

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Sound", TSSEditorUtils.max80pxWidth);
                        TSSEditorUtils.DrawGenericProperty(ref values.soundRange[0], TSSEditorUtils.redColor, parent);
                        TSSEditorUtils.DrawGenericProperty(ref values.soundRange[1], TSSEditorUtils.greenColor, parent);
                        EditorGUILayout.EndHorizontal();

                        break;
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(3);

            return true;
        }

        private static bool DrawTweenDeleteButton(TSSTween tween, List<TSSTween> holder, Object parent)
        {
            if (!GUILayout.Button(delTweenButtonContent, TSSEditorUtils.max18pxWidth, TSSEditorUtils.fixedLineHeight)) return false;
            if (holder.Contains(tween))
            {
                if (parent != null) Undo.RecordObject(parent, "[TSS Tween] delete tween");
                holder.Remove(tween);
            }
            return true;
        }

        private static void AddTween(List<TSSTween> holder, Object parent, bool isLast = false)
        {
            TSSItem item = null;

            foreach (Transform itemTransform in Selection.transforms)
            {
                item = itemTransform.GetComponent<TSSItem>();
                if (item == null) continue;
                break;
            }

            if (parent != null) Undo.RecordObject(parent, "[TSS Tween] add tween");

            holder.Insert(isLast ? holder.Count : 0, new TSSTween(item));
        }

        #endregion 
    }
}