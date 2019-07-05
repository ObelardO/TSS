// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru/tss
// MIT License

using System.Collections.Generic;
using UnityEngine;

namespace TSS
{
    #region Enumerations

    public enum ItemEffect
    {
        transform,
        position,
        rotation,
        scale,
        alpha,
        directAlpha,
        color,
        imageFill,
        text,
        number,
        rect,
        volume,
        range,
        light,
        gradient,
        rectDelta,
        time,
        property
    }

    public enum ItemState { closed, opening, opened, closing, slave }

    public enum ItemUpdateType { update, lateUpdate, fixedUpdate }

    public enum ItemKey { closed, opened }

    public enum RotationMode { quaternion, euler, path }

    public enum MaterialMode { instanced, direct }

    public enum ActivationMode
    {
        disabled,
        open, close, openClose,
        openBranch, closeBranch, openCloseBranch,
        openImmediately, closeImmediately, openCloseImmediately,
        openBranchImmediately, closeBranchImmediately, openCloseBranchImmediately
    }

    public enum ChainDirection { first2Last, last2First, middle2End, end2Middle, sync, random }

    public enum ButtonDirection { open2Close, close2Open }

    public enum MaterialPropertyType { single, integer, color, colorHDR, gradient, curve, vector2, vector3, vector4 }

    #endregion
}

namespace TSS.Base
{
    [System.Serializable]
    public class TSSItemActivator
    {
        #region Properties

        public TSSItem item;
        public bool enabled = true;
        public bool overrideModes;
        public ActivationMode[] mode = new ActivationMode[2] { ActivationMode.closeBranch, ActivationMode.openBranch };

        #endregion

        #region Init

        public TSSItemActivator()
        {
            item = null;
        }

        public TSSItemActivator(TSSItem item)
        {
            this.item = item;
            if (item == null) return;
            mode[0] = item.values.activations[0];
            mode[1] = item.values.activations[1];
        }

        #endregion

        #region Activation

        public void Open()
        {
            if (item != null && enabled) TSSItemBase.Activate(item, overrideModes ? mode[1] : item.activationOpen);
        }

        public void Close()
        {
            if (item != null && enabled) TSSItemBase.Activate(item, overrideModes ? mode[0] : item.activationClose);
        }

        public void ActivateManualy(ActivationMode mode)
        {
            if (item != null && enabled) TSSItemBase.Activate(item, mode);
        }

        #endregion
    }

    [System.Serializable]
    public class TSSMaterialProperty
    {
        #region Properties

        public bool enabled;
        public string name;
        public MaterialPropertyType type;

        public float[] singleValues;
        public int[] integerValues;
        public Color[] colorValues;
        public Vector2[] vector2values;
        public Vector3[] vector3values;
        public Vector4[] vector4values;

        public Gradient gradient;
        public AnimationCurve curve;

        #endregion

        #region Init

        public TSSMaterialProperty(MaterialPropertyType propertyType)
        {
            type = propertyType;
            name = "_Property";
            switch (propertyType)
            {
                case MaterialPropertyType.single: singleValues = new float[TSSItemBase.stateCount] { 0, 1 }; break;
                case MaterialPropertyType.integer: integerValues = new int[TSSItemBase.stateCount] { 0, 1 }; break;
                case MaterialPropertyType.color: colorValues = new Color[TSSItemBase.stateCount] { Color.white, Color.white }; break;
                case MaterialPropertyType.colorHDR: colorValues = new Color[TSSItemBase.stateCount] { Color.white, Color.white }; break;
                case MaterialPropertyType.vector2: vector2values = new Vector2[TSSItemBase.stateCount]; break;
                case MaterialPropertyType.vector3: vector3values = new Vector3[TSSItemBase.stateCount]; break;
                case MaterialPropertyType.vector4: vector4values = new Vector4[TSSItemBase.stateCount]; break;
                case MaterialPropertyType.curve: curve = AnimationCurve.EaseInOut(0, 0, 1, 1); break;
                case MaterialPropertyType.gradient:
                    gradient = new Gradient()
                    {
                        colorKeys = new GradientColorKey[] { new GradientColorKey(Color.black, 0), new GradientColorKey(Color.white, 1) }
                    };
                    break;
            }
        }

        #endregion
    }

    [System.Serializable]
    public struct TSSItemValues
    {
        #region Properties

        public bool inited;

        public ActivationMode startAction;

        public ActivationMode[] activations;

        public float[] delays;
        public float[] durations;
        public bool[] childBefore;

        public ChainDirection[] chainDirections;
        public float[] chainDelays;
        public float[] firstChildDelay;

        public RotationMode rotationMode;
        public Vector3 rotationMask;
        public MaterialMode materialMode;

        public bool childChainMode;
        public bool brakeChainDelay;
        public bool interactions;
        public bool blockRaycasting;

        public bool soundControl;
        public bool soundRestart;

        public bool videoControl;
        public bool videoRestart;

        public int randomWaveLength;
        public string floatFormat;

        public bool ignoreChilds;
        public bool ignoreParent;

        public float buttonDuration;
        public ButtonDirection buttonDirection;
        [TSSKeyCode] public List<KeyCode> onKeyboard;

        public int loops;
        public ActivationMode loopMode;

        public Vector3[] positions;
        public Quaternion[] rotations;
        public Vector3[] eulerRotations;
        public Vector3[] scales;
        public Color[] colors;
        public float[] alphas;
        public float[] imageFills;
        public float[] numbers;
        public string[] texts;
        public Vector4[] rects;
        public Vector4[] anchors;
        public Vector2[] anchorPositions;
        public float[] intensities;
        public float[] lightRange;
        public float[] soundRange;
        public float[] sphereRange;

        public ItemUpdateType updatingType;
        public bool timeScaled;

        public List<Vector3> path;
        public bool pathIsLooped;
        public bool pathAutoControl;
        public float pathSmoothFactor;
        public int pathResolution;
        public int pathSpacing;
        public PathNormal pathNormal;
        public PathLerpMode pathLerpMode;

        #endregion
    }

    public static class TSSItemBase
    {
        #region Properties

        public const int stateCount = 2;

        #endregion

        #region Init

        public static void InitValues(ref TSSItemValues values)
        {
            if (values.inited) return;

            values.inited = true;
            values.startAction = ActivationMode.closeBranchImmediately;
            values.activations = new ActivationMode[stateCount] { ActivationMode.closeBranch, ActivationMode.openBranch };
            values.delays = new float[stateCount];
            values.durations = new float[stateCount] { 1, 1 };
            values.childBefore = new bool[stateCount];
            values.chainDirections = new ChainDirection[stateCount];
            values.chainDelays = new float[stateCount] { 0.2f, 0.2f };
            values.brakeChainDelay = true;
            values.firstChildDelay = new float[stateCount] { 0.2f, 0.2f };
            values.buttonDuration = 0.5f;
            values.onKeyboard = new List<KeyCode>();
            values.positions = new Vector3[stateCount] { Vector3.zero, Vector3.zero };
            values.rotations = new Quaternion[stateCount] { Quaternion.identity, Quaternion.identity };
            values.eulerRotations = new Vector3[stateCount] { Vector3.zero, Vector3.zero };
            values.scales = new Vector3[stateCount] { Vector3.one, Vector3.one };
            values.colors = new Color[stateCount] { Color.white, Color.white };
            values.alphas = new float[stateCount] { 0, 1 };
            values.imageFills = new float[stateCount] { 0, 1 };
            values.numbers = new float[stateCount] { 0, 100 };
            values.texts = new string[stateCount];
            values.rects = new Vector4[stateCount];
            values.anchors = new Vector4[stateCount];
            values.anchorPositions = new Vector2[stateCount] { Vector3.zero, Vector3.zero };
            values.intensities = new float[stateCount] { 0, 1 };
            values.lightRange = new float[stateCount] { 0, 1 };
            values.soundRange = new float[stateCount] { 0, 1 };
            values.sphereRange = new float[stateCount] { 0, 1 };
            values.floatFormat = "0";

            values.path = TSSPathBase.GetDefaultPath();
            values.pathSmoothFactor = 0.5f;
            values.pathResolution = 1;
            values.pathSpacing = 10;
            values.rotationMask = Vector3.one;

            values.timeScaled = true;
        }

        #endregion

        #region Capturing values

        public static void Capture(this TSSItem item, ItemKey key)
        {
            CaptureItemPosition(item, key);
            CaptureItemRotation(item, key);
            CaptureItemScales(item, key);
            CaptureItemRectTransform(item, key);
            CaptureItemUI(item, key);
            CaptureItemLight(item, key);
            CaptureItemRanges(item, key);
            CaptureItemMaterialColor(item, key);
            CaptureItemPath(item, key);
        }

        public static void CaptureItemTransform(TSSItem item, ItemKey key)
        {
            CaptureItemPosition(item, key);
            CaptureItemRotation(item, key);
            CaptureItemScales(item, key);
        }

        public static void CaptureItemPosition(TSSItem item, ItemKey key)
        {
            item.values.positions[(int)key] = item.transform.localPosition;
            CaptureItemPath(item, key);
        }

        public static void CaptureItemRotation(TSSItem item, ItemKey key)
        {
            item.values.rotations[(int)key] = item.transform.localRotation;
            item.values.eulerRotations[(int)key] = item.transform.GetInspectorRotation();
        }

        public static void CaptureItemScales(TSSItem item, ItemKey key)
        {
            item.values.scales[(int)key] = item.transform.localScale;
        }

        public static void CaptureItemRectTransform(TSSItem item, ItemKey key)
        {
            if (item.rect != null)
            {
                item.values.rects[(int)key] = new Vector4
                (
                    item.rect.offsetMin.x, item.rect.offsetMin.y,
                    item.rect.offsetMax.x, item.rect.offsetMax.y
                );
                item.values.anchors[(int)key] = new Vector4
                (
                    item.rect.anchorMin.x, item.rect.anchorMin.y,
                    item.rect.anchorMax.x, item.rect.anchorMax.y
                );
                item.values.anchorPositions[(int)key] = item.rect.anchoredPosition;
            }
        }

        public static void CaptureItemUI(TSSItem item, ItemKey key)
        {
            if (item.canvasGroup != null)
            {
                item.values.alphas[(int)key] = item.canvasGroup.alpha;
            }
            else if (item.image != null)
            {
                item.values.alphas[(int)key] = item.image.color.a;
                item.values.colors[(int)key] = item.image.color;
                item.values.imageFills[(int)key] = item.image.fillAmount;
            }
            else if (item.rawImage != null)
            {
                item.values.alphas[(int)key] = item.rawImage.color.a;
                item.values.colors[(int)key] = item.rawImage.color;
            }
            else if (item.text != null)
            {
                item.values.alphas[(int)key] = item.text.color.a;
                item.values.colors[(int)key] = item.text.color;
                item.values.texts[(int)key] = item.text.text;

                TSSText.Parse(item.text.text, out item.values.numbers[(int)key], out item.stringPart);
            }
        }

        public static void CaptureItemLight(TSSItem item, ItemKey key)
        {
            if (item.itemLight != null)
            {
                item.values.colors[(int)key] = item.itemLight.color;
                item.values.lightRange[(int)key] = item.itemLight.range;
                item.values.intensities[(int)key] = item.itemLight.intensity;
            }
        }

        public static void CaptureItemRanges(TSSItem item, ItemKey key)
        {
            if (item.sphereCollider != null)
            {
                item.values.sphereRange[(int)key] = item.sphereCollider.radius;
            }

            if (item.audioPlayer != null)
            {
                item.values.soundRange[(int)key] = item.audioPlayer.maxDistance;
            }
        }

        public static void CaptureItemMaterialColor(TSSItem item, ItemKey key)
        {
            if (item.material != null && item.material.HasProperty("_Color"))
            {
                item.values.colors[(int)key] = item.material.GetColor("_Color");
            }
        }

        public static void CaptureItemPath(TSSItem item, ItemKey key)
        {
            if (item.path != null && item.path.enabled)
            {
                if (item.path.loop)
                {
                    item.path.SetPointPos(0, TSSPathBase.ToLocal(item.path, item.transform.position), true);
                    item.path.SetPointPos(0, TSSPathBase.ToLocal(item.path, item.transform.position), true);
                    item.values.positions[0] = item.transform.localPosition;
                    item.values.positions[1] = item.transform.localPosition;
                    return;
                }
                item.path.SetPointPos(key == 0 ? 0 : item.path.count - 1, TSSPathBase.ToLocal(item.path, item.transform.position));
                item.path.UpdateSpacedPoints();
            }
        }

        #endregion

        #region Extention methods

        public static float GetItemDuration(this TSSItem item)
        {
            switch (item.state)
            {
                case ItemState.opening: return item.openDuration;
                case ItemState.closing: return item.closeDuration;
                case ItemState.opened: return 1;
                case ItemState.slave: return 1;
            }
            return 0;
        }

        public static float GetItemDelayInChain(this TSSItem item, bool childBefore, ChainDirection direction, float delay, float firstChildDelay)
        {

            float offset = -firstChildDelay + delay;

            switch (direction)
            {
                case ChainDirection.first2Last: return (item.ID) * delay - offset;
                case ChainDirection.last2First: return (item.parent.childItems.Count - item.ID + 1) * delay - offset;
                case ChainDirection.middle2End: return Mathf.CeilToInt(item.parent.childItems.Count % 2 + Mathf.Abs((item.parent.childItems.Count + 1) * 0.5f - item.ID)) * delay - offset;
                case ChainDirection.end2Middle: return ((item.parent.childItems.Count + 1) * 0.5f - Mathf.Abs((item.parent.childItems.Count + 1) * 0.5f - item.ID)) * delay - offset;
                case ChainDirection.sync: return delay - offset;
                case ChainDirection.random: return UnityEngine.Random.Range(1, item.parent.childItems.Count - 1) * delay - offset;
                default: return 0;
            }
        }

        public static Transform GetItemParentTransform(TSSItem item)
        {
            Transform parent = item.transform.parent;
            while (parent != null && (parent.GetComponent<TSSItem>() == null || (parent.GetComponent<TSSItem>() != null && !parent.GetComponent<TSSItem>().enabled))) parent = parent.parent;
            return parent;
        }

        public static void UpdateItemDelaysInChain(this TSSItem item, int key)
        {
            if (!item.childChainMode) return;

            for (int i = 0; i < item.childItems.Count; i++)
            {
                item.childItems[i].values.delays[key] = item.childItems[i].GetItemDelayInChain(
                    item.values.childBefore[key],
                    item.values.chainDirections[key],
                    item.values.chainDelays[key],
                    item.values.firstChildDelay[key]);
            }
        }

        #endregion

        #region Effects methods

        delegate void EffectDelegate(TSSItem item, float time);

        static EffectDelegate[] effects = new EffectDelegate[]
        {
            DoTransform,
            DoPosition,
            DoRotation,
            DoScale,
            DoAlpha,
            DoDirectAlpha,
            DoColor,
            DoImageFill,
            DoText,
            DoNumber,
            DoRect,
            DoVolume,
            DoRange,
            DoLight,
            DoGradient,
            DoRectDelta,
            DoValue
        };

        public static void Evaluate(this TSSItem item, float time, ItemKey direction)
        {

            float effectValue = 0;

            if (item.state != ItemState.slave) item.state = ItemState.slave;
            for (int i = 0; i < item.tweens.Count; i++)
            {
                if (!item.tweens[i].enabled ||
                    (direction == ItemKey.closed && item.tweens[i].direction != TweenDirection.Close && item.tweens[i].direction != TweenDirection.OpenClose) ||
                    (direction == ItemKey.opened && item.tweens[i].direction != TweenDirection.Open && item.tweens[i].direction != TweenDirection.OpenClose)) continue;

                TweenType type = item.tweens[i].mode == TweenMode.Single ? item.tweens[i].type :
                    (direction == ItemKey.closed ? item.tweens[i].closingType : item.tweens[i].type);

                effectValue = item.tweens[i].Evaluate(time, type);

                if (item.tweens[i].effect == ItemEffect.property) DoProperty(item, item.tweens[i].matProperty, effectValue);
                else effects[(int)item.tweens[i].effect](item, effectValue);
            }
        }

        public static void Evaluate(this TSSItem item, float time)
        {
            float effectValue = 0.0f;

            if (item.state != ItemState.slave) item.state = ItemState.slave;
            for (int i = 0; i < item.tweens.Count; i++)
            {
                if (!item.tweens[i].enabled) continue;

                effectValue = item.tweens[i].Evaluate(time, item.tweens[i].type);

                if (item.tweens[i].effect == ItemEffect.property) DoProperty(item, item.tweens[i].matProperty, effectValue);
                else effects[(int)item.tweens[i].effect](item, effectValue);
            }
        }

        public static void EvaluateBranch(this TSSItem item, float time)
        {
            Evaluate(item, time);
            for (int i = 0; i < item.childItems.Count; i++) EvaluateBranch(item.childItems[i], time);
        }

        public static void EvaluateBranch(this TSSItem item, float time, ItemKey direction)
        {
            Evaluate(item, time, direction);
            for (int i = 0; i < item.childItems.Count; i++) EvaluateBranch(item.childItems[i], time, direction);
        }

        public static void DoAllEffects(TSSItem item, float time)
        {
            float effectValue = 0.0f;

            for (int i = 0; i < item.tweens.Count; i++)
            {
                if (!item.tweens[i].enabled) continue;

                if (item.tweens[i].mode == TweenMode.Multiple)
                {
                    effectValue = item.tweens[i].Evaluate(time, (item.IsClosed || item.isClosing) ?
                        item.tweens[i].closingType :
                        item.tweens[i].type);
                }
                else effectValue = item.tweens[i].Evaluate(time, item.tweens[i].type);

                if (item.tweens[i].effect == ItemEffect.property) DoProperty(item, item.tweens[i].matProperty, effectValue);
                else effects[(int)item.tweens[i].effect](item, effectValue);
            }
        }

        public static void DoEffect(TSSItem item, float time, ItemEffect itemEffect)
        {
            effects[(int)itemEffect](item, time);
        }

        public static void DoTransform(TSSItem item, float time)
        {
            DoPosition(item, time);
            DoRotation(item, time);
            DoScale(item, time);
        }

        public static void DoPosition(TSSItem item, float time)
        {
            if (item.path != null && item.path.enabled) item.transform.position = item.path.EvaluatePosition(time);
            else item.transform.localPosition = Vector3.LerpUnclamped(item.values.positions[0], item.values.positions[1], time);

        }

        public static void DoRotation(TSSItem item, float time)
        {
            if (item.path != null && item.path.enabled && item.rotationMode == RotationMode.path) item.transform.rotation = item.path.EvaluateRotation(time, TSSPathBase.normals[(int)item.values.pathNormal]);
            if (item.rotationMode == RotationMode.quaternion) item.transform.localRotation = Quaternion.LerpUnclamped(item.values.rotations[0], item.values.rotations[1], time);
            else if (item.rotationMode == RotationMode.euler) item.transform.localEulerAngles = Vector3.LerpUnclamped(item.values.eulerRotations[0], item.values.eulerRotations[1], time);
        }

        public static void DoScale(TSSItem item, float time)
        {
            item.transform.localScale = Vector3.LerpUnclamped(item.values.scales[0], item.values.scales[1], time);
        }

        public static void DoAlpha(TSSItem item, float time)
        {
            if (item.canvasGroup != null) item.canvasGroup.alpha = Mathf.Lerp(item.values.alphas[0], item.values.alphas[1], time);
            else if (item.image != null) item.image.color = new Color(item.image.color.r, item.image.color.g, item.image.color.b, Mathf.Lerp(item.values.alphas[0], item.values.alphas[1], time));
            else if (item.rawImage != null) item.rawImage.color = new Color(item.rawImage.color.r, item.rawImage.color.g, item.rawImage.color.b, Mathf.Lerp(item.values.alphas[0], item.values.alphas[1], time));
            else if (item.text != null) item.text.color = new Color(item.text.color.r, item.text.color.g, item.text.color.b, Mathf.Lerp(item.values.alphas[0], item.values.alphas[1], time));
        }

        public static void DoDirectAlpha(TSSItem item, float time)
        {
            if (item.canvasGroup != null) item.canvasGroup.alpha = time;
            else if (item.image != null) item.image.color = new Color(item.image.color.r, item.image.color.g, item.image.color.b, time);
            else if (item.rawImage != null) item.rawImage.color = new Color(item.rawImage.color.r, item.rawImage.color.g, item.rawImage.color.b, time);
            else if (item.text != null) item.text.color = new Color(item.text.color.r, item.text.color.g, item.text.color.b, time);
        }

        public static void DoColor(TSSItem item, float time)
        {
            if (item.image != null) item.image.color = Color.LerpUnclamped(item.values.colors[0], item.values.colors[1], time);
            else if (item.rawImage != null) item.rawImage.color = Color.LerpUnclamped(item.values.colors[0], item.values.colors[1], time);
            else if (item.text != null) item.text.color = Color.LerpUnclamped(item.values.colors[0], item.values.colors[1], time);
            else if (item.itemLight != null) item.itemLight.color = Color.LerpUnclamped(item.values.colors[0], item.values.colors[1], time);

            if (item.material == null || !item.material.HasProperty("_Color")) return;
            item.material.SetColor("_Color", Color.LerpUnclamped(item.values.colors[0], item.values.colors[1], time));
        }

        public static void DoImageFill(TSSItem item, float time)
        {
            if (item.image != null) item.image.fillAmount = Mathf.Lerp(item.values.imageFills[0], item.values.imageFills[1], time);
        }

        public static void DoText(TSSItem item, float time)
        {
            if (item.text != null) item.text.text = TSSText.Lerp(item.values.texts[0], item.values.texts[1], time, item.values.randomWaveLength);
        }

        public static void DoGradient(TSSItem item, float time)
        {
            if (item.gradient == null) return;
            item.gradient.Offset = time;
            if (time != 0 && time != 1) return;
            if (item.text != null) item.text.color = item.gradient.EffectGradient.Evaluate(1 - time);
            if (item.image != null) item.image.color = item.gradient.EffectGradient.Evaluate(1 - time);
            if (item.rawImage != null) item.rawImage.color = item.gradient.EffectGradient.Evaluate(1 - time);
        }

        public static void DoNumber(TSSItem item, float time)
        {
            if (item.text == null) return;
            item.text.text = string.Format("{0}{1}", Mathf.LerpUnclamped(item.values.numbers[0], item.values.numbers[1], time)
                            .ToString(item.values.floatFormat, System.Globalization.CultureInfo.InvariantCulture)
                            .Replace(string.Format("<{0}>", TSSPrefs.Symbols.dot), string.Empty).Replace("<>", string.Empty), item.stringPart);
        }

        public static void DoRect(TSSItem item, float time)
        {
            if (item.rect == null) return;
            item.rect.offsetMin = Vector2.LerpUnclamped(new Vector2(item.values.rects[0].x, item.values.rects[0].y), new Vector2(item.values.rects[1].x, item.values.rects[1].y), time);
            item.rect.offsetMax = Vector2.LerpUnclamped(new Vector2(item.values.rects[0].z, item.values.rects[0].w), new Vector2(item.values.rects[1].z, item.values.rects[1].w), time);
            item.rect.anchorMin = Vector2.LerpUnclamped(new Vector2(item.values.anchors[0].x, item.values.anchors[0].y), new Vector2(item.values.anchors[1].x, item.values.anchors[1].y), time);
            item.rect.anchorMax = Vector2.LerpUnclamped(new Vector2(item.values.anchors[0].z, item.values.anchors[0].w), new Vector2(item.values.anchors[1].z, item.values.anchors[1].w), time);
            item.rect.anchoredPosition = Vector2.LerpUnclamped(item.values.anchorPositions[0], item.values.anchorPositions[1], time);
        }

        public static void DoRectDelta(TSSItem item, float time)
        {
            if (item.rect == null) return;
            item.rect.sizeDelta =
            Vector2.LerpUnclamped(
                new Vector2(item.values.rects[0].z, item.values.rects[0].w),
                new Vector2(item.values.rects[1].z, item.values.rects[1].w), time) -
            Vector2.LerpUnclamped(
                new Vector2(item.values.rects[0].x, item.values.rects[0].y),
                new Vector2(item.values.rects[1].x, item.values.rects[1].y), time);
        }

        public static void DoVolume(TSSItem item, float time)
        {
            if (item.audioPlayer != null) { item.audioPlayer.volume = Mathf.Clamp01(time); return; }
            if (item.videoPlayer == null) return;
            ushort audioTracksCount = item.videoPlayer.controlledAudioTrackCount;

            for (ushort i = 0; i < audioTracksCount; i++)
            {
                if (item.videoPlayer.IsAudioTrackEnabled(i))
                {
                    if (item.videoPlayer.canSetDirectAudioVolume) item.videoPlayer.SetDirectAudioVolume(i, Mathf.Clamp01(time));
                    AudioSource audioPlayer = item.videoPlayer.GetTargetAudioSource(i);
                    if (audioPlayer != null) audioPlayer.volume = Mathf.Clamp01(time);
                }
            }
        }

        public static void DoRange(TSSItem item, float time)
        {
            if (item.itemLight != null) item.itemLight.range = Mathf.Clamp(Mathf.LerpUnclamped(item.values.lightRange[0], item.values.lightRange[1], time), 0, 1000);
            if (item.audioPlayer != null) item.audioPlayer.maxDistance = Mathf.Clamp(Mathf.LerpUnclamped(item.values.soundRange[0], item.values.soundRange[1], time), 0, 1000);
            if (item.sphereCollider != null) item.sphereCollider.radius = Mathf.Clamp(Mathf.LerpUnclamped(item.values.sphereRange[0], item.values.sphereRange[1], time), 0, 1000);
        }

        public static void DoLight(TSSItem item, float time)
        {
            if (item.itemLight == null) return;
            item.itemLight.intensity = Mathf.Clamp(Mathf.LerpUnclamped(item.values.intensities[0], item.values.intensities[1], time), 0, 1000);
            item.itemLight.range = Mathf.Clamp(Mathf.LerpUnclamped(item.values.lightRange[0], item.values.lightRange[1], time), 0, 1000);
            item.itemLight.color = Color.LerpUnclamped(item.values.colors[0], item.values.colors[1], time);
        }

        public static void DoValue(TSSItem item, float time)
        {
            item.evaluation = time;
        }

        public static void DoProperty(TSSItem item, TSSMaterialProperty property, float time)
        {
            if (item.material == null || !item.material.HasProperty(property.name)) return;

            switch (property.type)
            {
                case MaterialPropertyType.color: item.material.SetColor(property.name, Color.LerpUnclamped(property.colorValues[0], property.colorValues[1], time)); break;
                case MaterialPropertyType.single: item.material.SetFloat(property.name, Mathf.LerpUnclamped(property.singleValues[0], property.singleValues[1], time)); break;
                case MaterialPropertyType.integer: item.material.SetInt(property.name, Mathf.RoundToInt(Mathf.LerpUnclamped(property.integerValues[0], property.integerValues[1], time))); break;
                case MaterialPropertyType.vector2: item.material.SetVector(property.name, Vector2.LerpUnclamped(property.vector2values[0], property.vector2values[1], time)); break;
                case MaterialPropertyType.vector3: item.material.SetVector(property.name, Vector3.LerpUnclamped(property.vector3values[0], property.vector3values[1], time)); break;
                case MaterialPropertyType.vector4: item.material.SetVector(property.name, Vector4.LerpUnclamped(property.vector4values[0], property.vector4values[1], time)); break;
                case MaterialPropertyType.curve: item.material.SetFloat(property.name, property.curve.Evaluate(time)); break;
                case MaterialPropertyType.gradient: item.material.SetColor(property.name, property.gradient.Evaluate(time)); break;
                case MaterialPropertyType.colorHDR: item.material.SetColor(property.name, Color.LerpUnclamped(property.colorValues[0], property.colorValues[1], time)); break;
            }
        }

        #endregion

        #region Activation methods

        delegate void ActivationDelegate(TSSItem item);

        private static ActivationDelegate[] activators = new ActivationDelegate[]
        {
            Open, Close, OpenClose,
            OpenBranch, CloseBranch, OpenCloseBranch,
            OpenImmediately, CloseImmediately, OpenCloseImmediately,
            OpenBranchImmediately, CloseBranchImmediately, OpenCloseBranchImmediately
        };

        public static void Activate(TSSItem item, ActivationMode mode)
        {
            item.loopActivated = false;
            if (mode == ActivationMode.disabled) return;
            activators[(int)mode - 1](item);
        }

        public static void Open(TSSItem item)
        {
            if (item.openDelay == 0 && item.openDuration == 0) { OpenImmediately(item); return; }
            if (item.state == ItemState.opening || item.state == ItemState.opened) return;
            item.stateChgTime = item.openDelay;
            item.stateChgBranchMode = false;
            if (item.parentChainMode && item.parent.brakeChainDelay) item.stateChgTime = item.openDelay * (1 - item.time);
            item.state = ItemState.opening;
            TSSBehaviour.AddItem(item);
        }

        public static void Close(TSSItem item)
        {
            item.loopActivated = false;
            if (item.closeDelay == 0 && item.closeDuration == 0) { CloseImmediately(item); return; }
            if (item.state == ItemState.closing || item.state == ItemState.closed) return;
            item.stateChgTime = item.closeDelay;
            item.stateChgBranchMode = false;
            if (item.parentChainMode && item.parent.brakeChainDelay) item.stateChgTime = item.closeDelay * (item.time);
            item.state = ItemState.closing;
            TSSBehaviour.AddItem(item);
        }

        public static void OpenClose(TSSItem item)
        {
            if (item.state == ItemState.closed || item.state == ItemState.closing) Open(item); else Close(item);
        }


        public static void OpenBranch(TSSItem item)
        {
            Open(item);
            item.stateChgBranchMode = true;
            if (item.openChildBefore) OpenChilds(item);
        }

        public static void CloseBranch(TSSItem item)
        {
            Close(item);
            item.stateChgBranchMode = true;
            if (item.closeChildBefore) CloseChilds(item);
        }

        public static void OpenCloseBranch(TSSItem item)
        {
            OpenClose(item);
            for (int i = 0; i < item.childItems.Count; i++) OpenCloseBranch(item.childItems[i]);
        }


        public static void OpenImmediately(TSSItem item)
        {
            TSSBehaviour.RemoveItem(item);
            item.state = ItemState.opened;
            item.time = 1;
            DoAllEffects(item, item.time);
            item.stateChgTime = 0;
        }

        public static void CloseImmediately(TSSItem item)
        {
            TSSBehaviour.RemoveItem(item);
            item.state = ItemState.closed;
            item.time = 0;
            DoAllEffects(item, item.time);
            item.stateChgTime = 0;
        }

        public static void OpenCloseImmediately(TSSItem item)
        {
            if (item.state == ItemState.closed || item.state == ItemState.closing) OpenImmediately(item); else CloseImmediately(item);
        }


        public static void OpenBranchImmediately(TSSItem item)
        {
            OpenImmediately(item);
            for (int i = 0; i < item.childItems.Count; i++) OpenBranchImmediately(item.childItems[i]);
        }

        public static void CloseBranchImmediately(TSSItem item)
        {
            CloseImmediately(item);
            for (int i = 0; i < item.childItems.Count; i++) CloseBranchImmediately(item.childItems[i]);
        }

        public static void OpenCloseBranchImmediately(TSSItem item)
        {
            OpenCloseImmediately(item);
            for (int i = 0; i < item.childItems.Count; i++) OpenCloseBranchImmediately(item.childItems[i]);
        }


        public static void OpenChilds(TSSItem item)
        {
            for (int i = 0; i < item.childItems.Count; i++) Activate(item.childItems[i], item.childItems[i].activationOpen);
        }

        public static void CloseChilds(TSSItem item)
        {
            for (int i = 0; i < item.childItems.Count; i++) Activate(item.childItems[i], item.childItems[i].activationClose);
        }

        #endregion
    }
}