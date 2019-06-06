// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru/tss
// MIT License

using UnityEngine;

namespace TSS
{
    #region Enumerations

    public enum TweenType
    {
        Linear, NoneZero, NoneOne, Custom,
        InQuad, OutQuad, InOutQuad, OutInQuad,
        InCubic, OutCubic, InOutCubic, OutInCubic,
        InQuart, OutQuart, InOutQuart, OutInQuart,
        InQuint, OutQuint, InOutQuint, OutInQuint,
        InSine, OutSine, InOutSine, OutInSine,
        InExpo, OutExpo, InOutExpo, OutInExpo,
        InCirc, OutCirc, InOutCirc, OutInCirc,
        InElastic, OutElastic, InOutElastic, OutInElastic,
        InBack, OutBack, InOutBack, OutInBack,
        InBounce, OutBounce, InOutBounce, OutInBounce
    }

    public enum TweenMode { Single, Multiple }

    public enum TweenDirection { OpenClose, Open, Close, Button }

    #endregion
}

namespace TSS.Base
{
    public static class TSSTweenBase
    {
        #region Easing

        private static float Linear(float t, float b, float c, float d)
        {
            return c * t / d + b;
        }

        private static float NoneZero(float t, float b, float c, float d)
        {
            return 0;
        }

        private static float NoneOne(float t, float b, float c, float d)
        {
            return 1;
        }

        private static float Custom(float t, float b, float c, float d)
        {
            return 0;
        }

        private static float InQuad(float t, float b, float c, float d)
        {
            return c * (t /= d) * t + b;
        }

        private static float OutQuad(float t, float b, float c, float d)
        {
            return -c * (t /= d) * (t - 2) + b;
        }

        private static float InOutQuad(float t, float b, float c, float d)
        {
            if ((t /= d / 2) < 1) return c / 2 * t * t + b;

            return -c / 2 * ((--t) * (t - 2) - 1) + b;
        }

        private static float OutInQuad(float t, float b, float c, float d)
        {
            if (t < d / 2) return OutQuad(t * 2, b, c / 2, d);
            return InQuad((t * 2) - d, b + c / 2, c / 2, d);
        }

        private static float InCubic(float t, float b, float c, float d)
        {
            return c * (t /= d) * t * t + b;
        }

        private static float OutCubic(float t, float b, float c, float d)
        {
            return c * ((t = t / d - 1) * t * t + 1) + b;
        }

        private static float InOutCubic(float t, float b, float c, float d)
        {
            if ((t /= d / 2) < 1) return c / 2 * t * t * t + b;
            return c / 2 * ((t -= 2) * t * t + 2) + b;
        }

        private static float OutInCubic(float t, float b, float c, float d)
        {
            if (t < d / 2) return OutCubic(t * 2, b, c / 2, d);
            return InCubic((t * 2) - d, b + c / 2, c / 2, d);
        }

        private static float InQuart(float t, float b, float c, float d)
        {
            return c * (t /= d) * t * t * t + b;
        }

        private static float OutQuart(float t, float b, float c, float d)
        {
            return -c * ((t = t / d - 1) * t * t * t - 1) + b;
        }

        private static float InOutQuart(float t, float b, float c, float d)
        {
            if ((t /= d / 2) < 1) return c / 2 * t * t * t * t + b;
            return -c / 2 * ((t -= 2) * t * t * t - 2) + b;
        }

        private static float OutInQuart(float t, float b, float c, float d)
        {
            if (t < d / 2) return OutQuart(t * 2, b, c / 2, d);
            return InQuart((t * 2) - d, b + c / 2, c / 2, d);
        }

        private static float InQuint(float t, float b, float c, float d)
        {
            return c * (t /= d) * t * t * t * t + b;
        }

        private static float OutQuint(float t, float b, float c, float d)
        {
            return c * ((t = t / d - 1) * t * t * t * t + 1) + b;
        }

        private static float InOutQuint(float t, float b, float c, float d)
        {
            if ((t /= d / 2) < 1) return c / 2 * t * t * t * t * t + b;
            return c / 2 * ((t -= 2) * t * t * t * t + 2) + b;
        }

        private static float OutInQuint(float t, float b, float c, float d)
        {
            if (t < d / 2) return OutQuint(t * 2, b, c / 2, d);
            return InQuint((t * 2) - d, b + c / 2, c / 2, d);
        }

        private static float InSine(float t, float b, float c, float d)
        {
            return -c * Mathf.Cos(t / d * (Mathf.PI / 2)) + c + b;
        }

        private static float OutSine(float t, float b, float c, float d)
        {
            return c * Mathf.Sin(t / d * (Mathf.PI / 2)) + b;
        }

        private static float InOutSine(float t, float b, float c, float d)
        {
            return -c / 2 * (Mathf.Cos(Mathf.PI * t / d) - 1) + b;
        }

        private static float OutInSine(float t, float b, float c, float d)
        {
            if (t < d / 2) return OutSine(t * 2, b, c / 2, d);
            return InSine((t * 2) - d, b + c / 2, c / 2, d);
        }

        private static float InExpo(float t, float b, float c, float d)
        {
            return (t == 0) ? b : c * Mathf.Pow(2, 10 * (t / d - 1)) + b - c * 0.001f;
        }

        private static float OutExpo(float t, float b, float c, float d)
        {
            return (t == d) ? b + c : c * 1.001f * (-Mathf.Pow(2, -10 * t / d) + 1) + b;
        }

        private static float InOutExpo(float t, float b, float c, float d)
        {
            if (t == 0) return b;
            if (t == d) return b + c;
            if ((t /= d / 2) < 1) return c / 2 * Mathf.Pow(2, 10 * (t - 1)) + b - c * 0.0005f;
            return c / 2 * 1.0005f * (-Mathf.Pow(2, -10 * --t) + 2) + b;
        }

        private static float OutInExpo(float t, float b, float c, float d)
        {
            if (t < d / 2) return OutExpo(t * 2, b, c / 2, d);
            return InExpo((t * 2) - d, b + c / 2, c / 2, d);
        }

        private static float InCirc(float t, float b, float c, float d)
        {
            return -c * (Mathf.Sqrt(1 - (t /= d) * t) - 1) + b;
        }

        private static float OutCirc(float t, float b, float c, float d)
        {
            return c * Mathf.Sqrt(1 - (t = t / d - 1) * t) + b;
        }

        private static float InOutCirc(float t, float b, float c, float d)
        {
            if ((t /= d / 2) < 1) return -c / 2 * (Mathf.Sqrt(1 - t * t) - 1) + b;
            return c / 2 * (Mathf.Sqrt(1 - (t -= 2) * t) + 1) + b;
        }

        private static float OutInCirc(float t, float b, float c, float d)
        {
            if (t < d / 2) return OutCirc(t * 2, b, c / 2, d);
            return InCirc((t * 2) - d, b + c / 2, c / 2, d);
        }

        private static float InElastic(float t, float b, float c, float d)
        {
            if (t == 0) return b;
            if ((t /= d) == 1) return b + c;
            float p = d * .3f;
            float s = 0;
            float a = 0;
            if (a == 0f || a < Mathf.Abs(c))
            {
                a = c;
                s = p / 4;
            }
            else
            {
                s = p / (2 * Mathf.PI) * Mathf.Asin(c / a);
            }
            return -(a * Mathf.Pow(2, 10 * (t -= 1)) * Mathf.Sin((t * d - s) * (2 * Mathf.PI) / p)) + b;
        }

        private static float OutElastic(float t, float b, float c, float d)
        {
            if (t == 0) return b;
            if ((t /= d) == 1) return b + c;
            float p = d * .3f;
            float s = 0;
            float a = 0;
            if (a == 0f || a < Mathf.Abs(c))
            {
                a = c;
                s = p / 4;
            }
            else
            {
                s = p / (2 * Mathf.PI) * Mathf.Asin(c / a);
            }
            return (a * Mathf.Pow(2, -10 * t) * Mathf.Sin((t * d - s) * (2 * Mathf.PI) / p) + c + b);
        }

        private static float InOutElastic(float t, float b, float c, float d)
        {
            if (t == 0) return b;
            if ((t /= d / 2) == 2) return b + c;
            float p = d * (.3f * 1.5f);
            float s = 0;
            float a = 0;
            if (a == 0f || a < Mathf.Abs(c))
            {
                a = c;
                s = p / 4;
            }
            else
            {
                s = p / (2 * Mathf.PI) * Mathf.Asin(c / a);
            }
            if (t < 1) return -.5f * (a * Mathf.Pow(2, 10 * (t -= 1)) * Mathf.Sin((t * d - s) * (2 * Mathf.PI) / p)) + b;
            return a * Mathf.Pow(2, -10 * (t -= 1)) * Mathf.Sin((t * d - s) * (2 * Mathf.PI) / p) * .5f + c + b;
        }

        private static float OutInElastic(float t, float b, float c, float d)
        {
            if (t < d / 2) return OutElastic(t * 2, b, c / 2, d);
            return InElastic((t * 2) - d, b + c / 2, c / 2, d);
        }

        private static float InBack(float t, float b, float c, float d)
        {
            float s = 1.70158f;
            return c * (t /= d) * t * ((s + 1) * t - s) + b;
        }

        private static float OutBack(float t, float b, float c, float d)
        {
            float s = 1.70158f;
            return c * ((t = t / d - 1) * t * ((s + 1) * t + s) + 1) + b;
        }

        private static float InOutBack(float t, float b, float c, float d)
        {
            float s = 1.70158f;
            if ((t /= d / 2) < 1) return c / 2 * (t * t * (((s *= (1.525f)) + 1) * t - s)) + b;
            return c / 2 * ((t -= 2) * t * (((s *= (1.525f)) + 1) * t + s) + 2) + b;
        }

        private static float OutInBack(float t, float b, float c, float d)
        {
            if (t < d / 2) return OutBack(t * 2, b, c / 2, d);
            return InBack((t * 2) - d, b + c / 2, c / 2, d);
        }

        private static float InBounce(float t, float b, float c, float d)
        {
            return c - OutBounce(d - t, 0, c, d) + b;
        }

        private static float OutBounce(float t, float b, float c, float d)
        {
            if ((t /= d) < (1 / 2.75f))
            {
                return c * (7.5625f * t * t) + b;
            }
            else if (t < (2 / 2.75f))
            {
                return c * (7.5625f * (t -= (1.5f / 2.75f)) * t + .75f) + b;
            }
            else if (t < (2.5f / 2.75f))
            {
                return c * (7.5625f * (t -= (2.25f / 2.75f)) * t + .9375f) + b;
            }
            else
            {
                return c * (7.5625f * (t -= (2.625f / 2.75f)) * t + .984375f) + b;
            }
        }

        private static float InOutBounce(float t, float b, float c, float d)
        {
            if (t < d / 2) return InBounce(t * 2, 0, c, d) * .5f + b;
            else return OutBounce(t * 2 - d, 0, c, d) * .5f + c * .5f + b;
        }

        private static float OutInBounce(float t, float b, float c, float d)
        {
            if (t < d / 2) return OutBounce(t * 2, b, c / 2, d);
            return InBounce((t * 2) - d, b + c / 2, c / 2, d);
        }

        private delegate float EaseDelegate(float t, float b, float c, float d);

        private static EaseDelegate[] tweens = new EaseDelegate[]
        {
            Linear, NoneZero, NoneOne, Custom,
            InQuad, OutQuad, InOutQuad, OutInQuad,
            InCubic, OutCubic, InOutCubic, OutInCubic,
            InQuart, OutQuart,  InOutQuart, OutInQuart,
            InQuint, OutQuint, InOutQuint, OutInQuint,
            InSine, OutSine, InOutSine, OutInSine,
            InExpo, OutExpo, InOutExpo, OutInExpo,
            InCirc, OutCirc, InOutCirc, OutInCirc,
            InElastic, OutElastic, InOutElastic, OutInElastic,
            InBack, OutBack, InOutBack, OutInBack,
            InBounce, OutBounce,  InOutBounce, OutInBounce
        };

        public static float Evaluate(float time, float duration, TweenType tweenType) { return tweens[(int)tweenType](time, 0, 1, duration); }

        #endregion
    }
}