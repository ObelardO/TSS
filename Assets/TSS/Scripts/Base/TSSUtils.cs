// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru/tss
// MIT License

using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using TSS.Base;

namespace TSS
{
    public static class TSSUtils
    {
        #region Inspector

        public static Vector3 GetInspectorRotation(this Transform transform)
        {
            Vector3 result = Vector3.zero;
            MethodInfo mth = typeof(Transform).GetMethod("GetLocalEulerAngles", BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo pi = typeof(Transform).GetProperty("rotationOrder", BindingFlags.Instance | BindingFlags.NonPublic);
            object rotationOrder = null;
            if (pi != null)
            {
                rotationOrder = pi.GetValue(transform, null);
            }
            if (mth != null)
            {
                object retVector3 = mth.Invoke(transform, new object[] { rotationOrder });
                result = (Vector3)retVector3;
            }
            return result;
        }

        #endregion

        #region Clone utils

        public static AnimationCurve Clone(this AnimationCurve sourceCurve)
        {
            return new AnimationCurve()
            {
                keys = sourceCurve.keys,
                preWrapMode = sourceCurve.preWrapMode,
                postWrapMode = sourceCurve.postWrapMode
            };
        }
        
        public static List<Vector3> Clone(this List<Vector3> sourceList)
        {
            List<Vector3> resultList = new List<Vector3>();
            for (int i = 0; i < sourceList.Count; i++) resultList.Add(sourceList[i]);
            return resultList;
        }

        public static List<TSSTween> Clone(this List<TSSTween> sourceList, TSSItem parent)
        {
            List<TSSTween> resultList = new List<TSSTween>();

            for (int i = 0; i < sourceList.Count; i++)
            {
                resultList.Add(new TSSTween(parent)
                {
                    customEase = sourceList[i].customEase.Clone(),
                    enabled = sourceList[i].enabled,
                    startPoint = sourceList[i].startPoint,
                    endPoint = sourceList[i].endPoint,
                    mode = sourceList[i].mode,
                    type = sourceList[i].type,
                    closingType = sourceList[i].closingType,
                    direction = sourceList[i].direction,
                    effect = sourceList[i].effect,
                    matPropertyType = sourceList[i].matPropertyType,
                    matProperty = sourceList[i].matProperty.Clone(),
                    blendFactor = sourceList[i].blendFactor

                });
            }

            return resultList;
        }

        public static TSSMaterialProperty Clone(this TSSMaterialProperty sourceProperty)
        {
            Color[] newColorValues = (sourceProperty.colorValues == null || sourceProperty.colorValues.Length == 0) ? sourceProperty.colorValues : new Color[TSSItemBase.stateCount];
            if (sourceProperty.colorValues != null) for (int i = 0; i < sourceProperty.colorValues.Length; i++)
                    newColorValues[i] = new Color(sourceProperty.colorValues[i].r, sourceProperty.colorValues[i].g, sourceProperty.colorValues[i].b, sourceProperty.colorValues[i].a);

            AnimationCurve newAnimationCurve = (sourceProperty.curve == null ? null : sourceProperty.curve.Clone());

            return new TSSMaterialProperty(sourceProperty.type)
            {
                name = sourceProperty.name,
                singleValues = sourceProperty.singleValues,
                integerValues = sourceProperty.integerValues,
                colorValues = newColorValues,
                curve = newAnimationCurve,
                gradient = sourceProperty.gradient,
                vector2values = sourceProperty.vector2values,
                vector3values = sourceProperty.vector3values,
                vector4values = sourceProperty.vector4values
            };
        }

        #endregion
    }

    public class TSSText
    {
        #region Text Utils

        public static void Parse(string text, out float numberPart, out string stringPart)
        {
            if (text.Contains(TSSPrefs.Symbols.percent.ToString()))
            {
                float.TryParse(text.Split(TSSPrefs.Symbols.percent)[0].Replace(" ", string.Empty), out numberPart);
                stringPart = TSSPrefs.Symbols.percent.ToString();
            }
            else if (text.Contains(TSSPrefs.Symbols.space.ToString()))
            {
                float.TryParse(text.Split(TSSPrefs.Symbols.space)[0], out numberPart);
                stringPart = text.Replace(numberPart.ToString(), string.Empty);
            }
            else
            {
                float.TryParse(text, out numberPart);
                stringPart = string.Empty;
            }
        }

        public static string GetHumanReadableString(string sourceStrirng)
        {
            string result = string.Empty;

            for (int i = 0; i < sourceStrirng.Length; i++)
            {
                char sourceChar = sourceStrirng[i];
                if (char.IsUpper(sourceChar) && i > 0) result += ' ';
                if (i == 0) sourceChar = char.ToUpper(sourceChar);
                result += sourceChar;
            }

            return result;
        }

        private static string GetRundomString(string sourceString, int length = 1)
        {
            sourceString = sourceString.Replace(" ", string.Empty);
            string result = string.Empty;
            for (int i = 0; i < length; i++) result += sourceString.Substring(Random.Range(0, sourceString.Length), 1);
            return result;
        }

        private static string GetStringByChar(char sourceChar, int length = 1)
        {
            string result = string.Empty;
            for (int i = 0; i < length; i++) result += sourceChar;
            return result;
        }

        private static string ProjectString(string projectOn, string projector)
        {
            if (projector.Length >= projectOn.Length) return projector;
            return projector + "" + projectOn.Substring(projector.Length, projectOn.Length - projector.Length);
        }

        private static string Blend(string a, string b, float t, int randomChars)
        {
            if (a == b) return a;
            if (t == 0) return a;
            if (t == 1) return b;

            int aLength = a.Length;
            int bLength = b.Length;
            int maxLength = Mathf.Max(aLength, bLength);

            int curCharPos = (int)System.Math.Ceiling(maxLength * t);
            string result = string.Empty;

            if (randomChars > maxLength) randomChars = maxLength;
            if (aLength < bLength) a += GetStringByChar(' ', bLength - aLength);
            if (bLength < aLength) b += GetStringByChar(' ', aLength - bLength);
            if (curCharPos <= randomChars) result = GetRundomString(b, curCharPos);
            else if (curCharPos <= maxLength)
            {
                int curRandomChars = (maxLength - curCharPos > randomChars ? randomChars : maxLength - curCharPos);
                result = b.Substring(0, curCharPos - curRandomChars) + GetRundomString(b, curRandomChars);
            }

            return ProjectString(a, result).TrimEnd();
        }

        public static string Lerp(string a, string b, float t, int randomChars = 2)
        {
            if (a == b) return a;
            if (t == 0) return a;
            if (t == 1) return b;

            string[] aLines = a.Split('\n');
            string[] bLines = b.Split('\n');
            int linesCount = Mathf.Max(aLines.Length, bLines.Length);
            string[] resultLines = new string[linesCount];
            string result = string.Empty;
            float lineDuration = 1.0f / linesCount;
            float lineTime = 0;

            for (int i = 0; i < linesCount; i++)
            {
                lineTime = 1 - Mathf.Clamp01(((lineDuration * (i + 1)) - t) / lineDuration);
                if (aLines.Length <= i) resultLines[i] = Blend(string.Empty, bLines[i], lineTime, randomChars);
                else if (bLines.Length <= i) resultLines[i] = Blend(aLines[i], string.Empty, lineTime, randomChars);
                else resultLines[i] = Blend(aLines[i], bLines[i], lineTime, randomChars);
                result += resultLines[i] + (i == linesCount - 1 ? string.Empty : "\n");
            }

            return result.TrimEnd('\n');
        }

        #endregion
    }

    public class TSSKeyCodeAttribute : PropertyAttribute
    {
        public TSSKeyCodeAttribute() { }
    }
}