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

    public enum PathLerpMode { baked, dynamic }

    public enum PathNormal { up, down, left, right, forward, back }

    #endregion
}

namespace TSS.Base
{
    public static class TSSPathBase
    {
        #region Transforms

        public static List<TSSPath> allPathes = new List<TSSPath>();

        public static Vector3[] normals = new Vector3[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

        public static Vector3 ToLocal(TSSPath path, Vector3 worldPoint)
        {
            return Quaternion.Inverse(path.itemWorldRotation) * (worldPoint - path.itemWorldPosition);
        }

        public static Vector3 ToWorld(TSSPath path, Vector3 localPoint)
        {
            return (path.itemWorldRotation * localPoint) + path.itemWorldPosition;
        }

        public static Quaternion ToLocal(TSSPath path, Quaternion worldRotation)
        {
            return Quaternion.Inverse(path.itemWorldRotation) * worldRotation;
        }

        public static Quaternion ToWorld(TSSPath path, Quaternion localRotation)
        {
            return path.itemWorldRotation * localRotation;
        }

        #endregion

        #region Get points

        public static List<Vector3> GetDefaultPath()
        {
            return GetDefaultPath(null);
        }

        public static List<Vector3> GetDefaultPath(TSSPath path)
        {
            if (path == null || path.item == null || (path.item.values.positions[0] == Vector3.zero && path.item.values.positions[1] == Vector3.zero))
            {
                return new List<Vector3> { Vector3.left * 200, Vector3.left * 150, Vector3.right * 150, Vector3.right * 200 };
            }

            Vector3 length = path.item.values.positions[1] - path.item.values.positions[0];

            return new List<Vector3>
            {
                path.item.values.positions[0],
                path.item.values.positions[0] + length * 0.2f,
                path.item.values.positions[1] - length * 0.2f,
                path.item.values.positions[1]
            };
        }

        public static Vector3[] GetSegmentPoints(Vector3[] path, int segmentID)
        {
            return new Vector3[] { path[segmentID * 3], path[segmentID * 3 + 1], path[segmentID * 3 + 2], path[loopID(segmentID * 3 + 3, path.Length)] };
        }

        public static Vector3[] GetSpacedPoints(Vector3[] path, int pointsCount, float resolution = 1)
        {
            List<Vector3> spacedPoints = new List<Vector3>();
            spacedPoints.Add(path[0]);
            Vector3 previousPoint = path[0];
            float distanceToLastSpecedPoint = 0;

            float spacing = GetPathLength(path) / (path.Length / 3) / pointsCount;

            for (int segmentID = 0; segmentID < path.Length / 3; segmentID++)
            {
                Vector3[] segmentPoints = GetSegmentPoints(path, segmentID);

                int divisions = Mathf.CeilToInt(GetSegmentLegth(segmentPoints[0], segmentPoints[3], segmentPoints[1], segmentPoints[2]) * resolution * 10);
                float t = 0;
                while (t <= 1)
                {
                    t += 1f / divisions;
                    Vector3 pointOnCurve = EvaluateSegment(segmentPoints[0], segmentPoints[3], segmentPoints[1], segmentPoints[2], t);
                    distanceToLastSpecedPoint += Vector3.Distance(previousPoint, pointOnCurve);

                    while (distanceToLastSpecedPoint >= spacing)
                    {
                        float overshootDst = distanceToLastSpecedPoint - spacing;
                        Vector3 newSpacedPoint = pointOnCurve + (previousPoint - pointOnCurve).normalized * overshootDst;
                        spacedPoints.Add(newSpacedPoint);
                        distanceToLastSpecedPoint = overshootDst;
                        previousPoint = newSpacedPoint;
                    }

                    previousPoint = pointOnCurve;
                }
            }

            return spacedPoints.ToArray();
        }

        #endregion

        #region Cubic

        public static Vector3 EvaluateCubicPath(List<Vector3> path, float time)
        {
            return EvaluateCubicPath(path.ToArray(), time);
        }

        public static Vector3 EvaluateCubicPath(Vector3[] path, float time)
        {
            float timeRelativeToSegment;

            int segmentID = GetCubicSegment(path, time, out timeRelativeToSegment);
            if (segmentID == -1)
            {
                segmentID = time < 0.5f ? 0 : path.Length / 3 - 1;
                timeRelativeToSegment = time < 0.5f ? 0 : 1;
            }

            return EvaluateSegment(path, segmentID, timeRelativeToSegment);
        }

        private static int GetCubicSegment(Vector3[] path, float time, out float timeRelativeToSegment)
        {
            timeRelativeToSegment = 0f;

            float subCurvePercent = 0f;
            float totalPercent = 0f;
            float approximateLength = GetPathLength(path);

            int segmentID = -1;

            for (int i = 0; i < path.Length / 3; i++)
            {
                subCurvePercent = GetSegmentLength(GetSegmentPoints(path, i)) / approximateLength;
                if (subCurvePercent + totalPercent > time) { segmentID = i; break; }
                totalPercent += subCurvePercent;
            }

            timeRelativeToSegment = (time - totalPercent) / subCurvePercent;

            return segmentID;
        }

        #endregion

        #region Cubic rotation

        public static Quaternion EvaluateCubicRotation(List<Vector3> path, Vector3 up, float time)
        {
            return EvaluateCubicRotation(path.ToArray(), up, time);
        }

        public static Quaternion EvaluateCubicRotation(Vector3[] path, Vector3 up, float time)
        {
            float timeRelativeToSegment;
            int segmentID = GetCubicSegment(path, time, out timeRelativeToSegment);

            if (segmentID == -1)
            {
                segmentID = time < 0.5f ? 0 : path.Length / 3 - 1;
                timeRelativeToSegment = time < 0.5f ? 0 : 1;
            }

            return EvaluateSegmentRotation(path, up, segmentID, timeRelativeToSegment);
        }

        public static Quaternion EvaluateSegmentRotation(Vector3[] path, Vector3 up, int segmentID, float time)
        {
            Vector3[] segmentPoints = GetSegmentPoints(path, segmentID);
            return EvaluateSegmentRotation(segmentPoints[0], segmentPoints[3], segmentPoints[1], segmentPoints[2], up, time);
        }

        public static Quaternion EvaluateSegmentRotation(Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent, Vector3 up, float time)
        {
            Vector3 tangent = GetTangentOnCubicCurve(startPosition, endPosition, startTangent, endTangent, time);
            return Quaternion.LookRotation(tangent, up);
        }

        public static Vector3 GetTangentOnCubicCurve(Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent, float time)
        {
            float t = time;
            float u = 1f - t;
            float u2 = u * u;
            float t2 = t * t;

            Vector3 tangent =
                (-u2) * startPosition +
                (u * (u - 2f * t)) * startTangent -
                (t * (t - 2f * u)) * endTangent +
                (t2) * endPosition;

            return tangent.normalized;
        }

        public static Vector3 GetBinormalOnCubicCurve(Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent, Vector3 up, float time)
        {
            Vector3 tangent = GetTangentOnCubicCurve(startPosition, endPosition, startTangent, endTangent, time);
            Vector3 binormal = Vector3.Cross(up, tangent);

            return binormal.normalized;
        }

        public static Vector3 GetNormalOnCubicCurve(Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent, Vector3 up, float time)
        {
            Vector3 tangent = GetTangentOnCubicCurve(startPosition, endPosition, startTangent, endTangent, time);
            Vector3 binormal = GetBinormalOnCubicCurve(startPosition, endPosition, startTangent, endTangent, up, time);
            Vector3 normal = Vector3.Cross(tangent, binormal);

            return normal.normalized;
        }

        #endregion

        #region Linear

        public static Vector3 EvaluateSegment(Vector3[] path, int segmentID, float time)
        {
            Vector3[] segmentPoints = GetSegmentPoints(path, segmentID);
            return EvaluateSegment(segmentPoints[0], segmentPoints[3], segmentPoints[1], segmentPoints[2], time);
        }

        public static Vector3 EvaluateSegment(Vector3 startPoint, Vector3 endPoint, Vector3 startTangentPoint, Vector3 endTangentPoint, float time)
        {
            float u = 1f - time;
            float t2 = time * time;
            float u2 = u * u;
            float u3 = u2 * u;
            float t3 = t2 * time;

            Vector3 result =
                (u3) * startPoint +
                (3f * u2 * time) * startTangentPoint +
                (3f * u * t2) * endTangentPoint +
                (t3) * endPoint;

            return result;
        }

        public static Vector3 EvaluateSpacedPath(Vector3[] spacedPoints, float time)
        {
            float segmentLenght = 1.0f / (spacedPoints.Length - 1);
            int segmentID = (int)(time / segmentLenght);

            return Vector3.Lerp(spacedPoints[loopID(segmentID, spacedPoints.Length)], spacedPoints[loopID(segmentID + 1, spacedPoints.Length)], (time - (segmentID * segmentLenght)) / segmentLenght);
        }

        #endregion

        #region Linear rotation

        public static Quaternion EvaluateSpacedRotation(Vector3[] spacedPoints, bool loop, Vector3 up, float time)
        {
            float segmentLenght = 1.0f / (spacedPoints.Length - 1);
            int segmentID = Mathf.FloorToInt(time / segmentLenght);

            Vector3 startPoint = Vector3.zero;
            if (segmentID != spacedPoints.Length - 1)
                startPoint = spacedPoints[segmentID + 1] - spacedPoints[segmentID];
            else if (segmentID > 0)
                startPoint = spacedPoints[segmentID] - spacedPoints[segmentID - 1];

            Vector3 endPoint = startPoint;
            if (segmentID == spacedPoints.Length - 2)
            { if (loop) endPoint = spacedPoints[1] - spacedPoints[0]; }
            else if (segmentID < spacedPoints.Length - 2)
                endPoint = spacedPoints[segmentID + 2] - spacedPoints[segmentID + 1];

            return Quaternion.Lerp(Quaternion.LookRotation(startPoint, up), Quaternion.LookRotation(endPoint, up), (time - (segmentID * segmentLenght)) / segmentLenght);
        }

        #endregion

        #region Get Length

        private static float GetPathLength(Vector3[] path)
        {
            float length = 0;
            for (int i = 0; i < path.Length / 3; i++) length += GetSegmentLength(GetSegmentPoints(path, i));
            return length;
        }

        private static float GetSegmentLength(Vector3[] segmentPoints)
        {
            return GetSegmentLegth(segmentPoints[0], segmentPoints[3], segmentPoints[1], segmentPoints[2]);
        }

        private static float GetSegmentLegth(Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent)
        {
            float length = 0f;
            Vector3 fromPoint = EvaluateSegment(startPosition, endPosition, startTangent, endTangent, 0f);

            for (int i = 0; i < TSSPrefs.dynamicPathSampling; i++)
            {
                float time = (i + 1) / (float)TSSPrefs.dynamicPathSampling;
                Vector3 toPoint = EvaluateSegment(startPosition, endPosition, startTangent, endTangent, time);
                length += Vector3.Distance(fromPoint, toPoint);
                fromPoint = toPoint;
            }

            return length;
        }

        #endregion

        private static int loopID(int pointID, int pathLenght)
        {
            return (pointID + pathLenght) % pathLenght;
        }
    }
}