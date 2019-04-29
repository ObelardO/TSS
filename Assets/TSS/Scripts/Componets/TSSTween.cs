// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru/tss
// MIT License

using UnityEngine;
using TSS.Base;

namespace TSS
{
    [System.Serializable]
    public class TSSTween
    {
        #region Properties

        /// <summary>Tweens activated and affecting on parent item</summary>
        public bool enabled = true;

        /// <summary>Relative start time</summary>
        public float startPoint = 0;

        /// <summary>Relative end time</summary>
        public float endPoint = 1;

        /// <summary>Parent item pointer</summary>
        public TSSItem item;

        /// <summary>Custom ease</summary>
        public AnimationCurve customEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

        /// <summary>Tween mode. When tween mode is single, there is a one ease to close and open
        /// And different eases on multiple mode</summary>
        public TweenMode mode;

        /// <summary>Tween ease</summary>
        public TweenType type = TweenType.InOutQuad;

        /// <summary>Tween ease for closing (using only at multiple mode)</summary>
        public TweenType closingType;
        public TweenDirection direction;
        public ItemEffect effect;

        /// <summary>
        /// If tween effect is Material property, this var specify property type (float, int, Vector3, Color ... etc.)
        /// </summary>
        [SerializeField] public MaterialPropertyType matPropertyType;
        /// <summary>Container for Material propert effect values</summary>
        [SerializeField] public TSSMaterialProperty matProperty;

        /// <summary>Time in seconds which tween eases are blending on a halfway</summary>
        public float blendFactor = 0.25f;
        public float blendTime = 0;
        private float effectValue, openValue, closeValue;

        #endregion

        #region Init

        public TSSTween(TSSItem item)
        {
            this.item = item;
            this.matProperty = null;
        }

        #endregion

        #region Update

        public float Evaluate(float value, TweenType type)
        {
            if (type == TweenType.Custom || direction == TweenDirection.Button) return customEase.Evaluate(((value - startPoint)) / (endPoint - startPoint));
            float duration = item.GetItemDuration();
            return TSSTweenBase.Evaluate(((Mathf.Clamp(value, startPoint, endPoint) - startPoint)) * duration, duration == 0 ? 1 : (endPoint - startPoint) * duration, type);
        }

        public void Update()
        {
            if (!enabled || direction == TweenDirection.Button) return;

            if ((item.state == ItemState.closing || item.state == ItemState.closed) && direction == TweenDirection.Open ||
                (item.state == ItemState.opening || item.state == ItemState.opened) && direction == TweenDirection.Close) return;

            if (item.time < startPoint && item.time > endPoint) return;

            if (mode == TweenMode.Single) effectValue = Evaluate(item.time, type);
            else
            {
                openValue = Evaluate(item.time, type);
                closeValue = Evaluate(item.time, closingType);
                if ((item.state == ItemState.closing && blendTime < blendFactor) ||
                    (item.state == ItemState.opening && blendTime > 0)) blendTime += item.deltaTime;
                effectValue = Mathf.SmoothStep(openValue, closeValue, Mathf.Clamp01(blendTime / blendFactor));
            }

            if (matProperty != null && effect == ItemEffect.property) TSSItemBase.DoProperty(item, matProperty, effectValue);
            else TSSItemBase.DoEffect(item, effectValue, effect);
        }

        #endregion
    }
}