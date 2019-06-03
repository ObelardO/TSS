// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru/tss
// MIT License

using System.Collections.Generic;
using UnityEngine;
using TSS.Base;

namespace TSS
{
    [System.Serializable, DisallowMultipleComponent, AddComponentMenu("TSS/Path"), RequireComponent(typeof(TSSItem))]
    #if UNITY_2018_3_OR_NEWER
        [ExecuteAlways]
    #else
        [ExecuteInEditMode]
    #endif
    public class TSSPath : MonoBehaviour
    {
        #region Properties

        [HideInInspector] public TSSItem item;
        public Vector3 itemWorldPosition { get { return item.transform.parent == null ? Vector3.zero : item.transform.parent.position; } }
        public Quaternion itemWorldRotation { get { return item.transform.parent == null ? Quaternion.identity : item.transform.parent.rotation; } }
        public Vector3 itemScale { get { return item.transform.parent == null ? Vector3.one : item.transform.parent.localScale; } }

        public PathLerpMode lerpMode { get { return item.values.pathLerpMode; } set { item.values.pathLerpMode = value; } }
        public float smoothFactor { get { return item.values.pathSmoothFactor; } set { item.values.pathSmoothFactor = value; AutoSetAllControls(); } }

        public int count { get { return points.Count; } }
        public Vector3 last { get { return points[count - 1]; } set { points[count - 1] = value; } }
        public Vector3 first { get { return points[0]; } set { points[0] = value; } }
        public int segmentsCount { get { return count / 3; } }
        public List<Vector3> points { get { return item.values.path; } set { item.values.path = value; } }
        public Vector3 this[int i] { get { return points[i]; } set { points[i] = value; } }

        public List<Transform> pointsAttach;

        [HideInInspector] public Vector3[] spacedPoints = null;
        public int spacing
        {
            get { return item.values.pathSpacing; }
            set { item.values.pathSpacing = value; item.values.pathSpacing = Mathf.Clamp(item.values.pathSpacing, 1, 100); UpdateSpacedPoints(); }
        }
        public int resolution
        {
            get { return item.values.pathResolution; }
            set { item.values.pathResolution = value; item.values.pathResolution = Mathf.Clamp(item.values.pathResolution, 1, 10); UpdateSpacedPoints(); }
        }

        public bool loop { get { return item.values.pathIsLooped; }
            set
            {
                if (item.values.pathIsLooped == value) return;

                item.values.pathIsLooped = value;

                if (!item.values.pathIsLooped)
                {
                    points.RemoveRange(count - 2, 2);
                    if (auto) AutoSetLoopEnds();
                    UpdateSpacedPoints();
                    return;
                }

                points.Add(last * 2 - points[count - 2]);
                points.Add(points[0] * 2 - points[1]);

                if (auto) { AutoSetControl(0); AutoSetControl(count - 3); }
                UpdateSpacedPoints();
            }
        }

        public bool auto { get { return item.values.pathAutoControl; }
            set
            {
                if (item.values.pathAutoControl == value) return;
                item.values.pathAutoControl = value;

                if (item.values.pathAutoControl && ((loop && segmentsCount > 2) || (!loop && segmentsCount > 1)))
                    AutoSetAllControls(); 
            }
        }

        #endregion

        #region Evaluate methods

        public Vector3 EvaluatePosition(float time)
        {
            if (item.values.pathLerpMode == PathLerpMode.baked)
                return ToWorld(TSSPathBase.EvaluateSpacedPath(spacedPoints, Mathf.Clamp01(time)));
            
            return ToWorld(TSSPathBase.EvaluateCubicPath(points, Mathf.Clamp01(time)));
        }

        public Quaternion EvaluateRotation(float time)
        {
            return EvaluateRotation(time, TSSPathBase.normals[(int)item.values.pathNormal]);
        }

        public Quaternion EvaluateRotation(float time, Vector3 aligmentVector)
        {
            Quaternion evaluatedRotation = Quaternion.identity;

            if (item.values.pathLerpMode == PathLerpMode.baked)
                evaluatedRotation = TSSPathBase.EvaluateSpacedRotation(spacedPoints, loop, aligmentVector.normalized, Mathf.Clamp01(time));
            else
                evaluatedRotation = TSSPathBase.EvaluateCubicRotation(points, aligmentVector.normalized, Mathf.Clamp01(time));

            return ToWorld(Quaternion.Euler(Mathf.Lerp(item.transform.localRotation.eulerAngles.x, evaluatedRotation.eulerAngles.x, item.rotationMask.x),
                                            Mathf.Lerp(item.transform.localRotation.eulerAngles.y, evaluatedRotation.eulerAngles.y, item.rotationMask.y),
                                            Mathf.Lerp(item.transform.localRotation.eulerAngles.z, evaluatedRotation.eulerAngles.z, item.rotationMask.z)));
        }

        public void Evaluate(float time)
        {
            EvaluatePosition(time);
            EvaluateRotation(time);
        }

        #endregion

        #region Segments

        /// <summary>
        /// Add a new segment to path
        /// </summary>
        /// <param name="newPos">new path's end position</param>
        /// <param name="toStart">if true new segment will be added to start</param>
        public void AddSegment(Vector3 newPos, bool toStart = false)
        {
            if (toStart)
            {
                points.Insert(0,points[0] * 2 - points[1]);
                points.Insert(0,(points[0] + newPos) * 0.5f);
                points.Insert(0,newPos);


                SetPointPos(0, newPos, true);
            }
            else
            {
                points.Add(last * 2 - points[count - 2]);
                points.Add((last + newPos) * 0.5f);
                points.Add(newPos);

                SetPointPos(count - 1, newPos, true);
            }

            UpdateSpacedPoints();

            if (item.values.pathLerpMode == PathLerpMode.dynamic)
            pointsAttach.Add(null);

            if (!auto) return;

            AutoSetAllEffectedControls(count -1);
            UpdateSpacedPoints();
        }

        /// <summary>
        /// Split specified path segment to 2 new segments
        /// </summary>
        /// <param name="newPos">split point</param>
        /// <param name="anchorID">segment ID</param>
        public void SplitSegment(Vector3 newPos, int anchorID)
        {
            points.InsertRange(anchorID * 3 + 2, new Vector3[] { Vector3.zero, newPos, Vector3.zero });

            if (item.values.pathLerpMode == PathLerpMode.dynamic)
                pointsAttach.Insert(anchorID + 1, null);

            if (auto)
            {
                AutoSetAllEffectedControls(anchorID * 3 + 3);
                UpdateSpacedPoints();
                return;
            }

            AutoSetControl(anchorID * 3 + 3);
            UpdateSpacedPoints();
        }

        /// <summary>
        /// Project all path to vector
        /// </summary>
        /// <param name="projectionMask">project on vector</param>
        public void Project(Vector3 projectionMask)
        {
            for (int i = 0; i < count; i++)
                points[i] = new Vector3(points[i].x * projectionMask.x, points[i].y * projectionMask.y, points[i].z * projectionMask.z);

            UpdateSpacedPoints();
        }

        /// <summary>
        /// Delete path segment
        /// </summary>
        /// <param name="anchorID">segment ID</param>
        public void DeleteSegment(int anchorID)
        {
            if (segmentsCount == 1 || (loop && segmentsCount == 2)) return;

            if (anchorID == 0)
            {
                if (loop) last = points[2];

                points.RemoveRange(0, 3);

            }
            else if (anchorID == count - 1 && !loop)
            {
                points.RemoveRange(anchorID - 2, 3);
            }
            else
            {
                points.RemoveRange(anchorID - 1, 3);
            }

            if (auto) AutoSetAllEffectedControls(anchorID);

            if (item.values.pathLerpMode == PathLerpMode.dynamic)
                pointsAttach.RemoveAt(anchorID / 3);

            UpdateSpacedPoints();
        }

        /// <summary>
        /// Get segment points (start, start tangent, end tangent, end)
        /// </summary>
        /// <param name="i">segment ID</param>
        /// <returns>4 segment points: start, start tangent, end tangent, end</returns>
        public Vector3[] GetSegmentPoints(int i)
        {
            return new Vector3[] { points[i * 3], points[i * 3 + 1], points[i * 3 + 2], points[loopID(i * 3 + 3)] };
        }

        /// <summary>
        /// Set path specified point position
        /// </summary>
        /// <param name="pointID">point ID</param>
        /// <param name="newPos">new point position (must be local, use ToLocal())</param>
        /// <param name="updateOthers">update connected points positions</param>
        public void SetPointPos(int pointID, Vector3 newPos, bool updateOthers = false)
        {
            if (auto && pointID % 3 != 0) return;

            Vector3 deltaPos = newPos - points[pointID];

            points[pointID] = newPos;

            if (pointID == 0 && item != null) item.values.positions[0] = points[0];
            if (pointID == count - 1 && item != null) item.values.positions[1] = last;

            if (!updateOthers) return;

            if (pointID % 3 == 0)
            {
                if (pointID + 1 < count || loop) points[loopID(pointID + 1)] += deltaPos;
                if (pointID - 1 >= 0 || loop) points[loopID(pointID - 1)] += deltaPos;

                if (auto && ((loop && segmentsCount > 2) || (!loop && segmentsCount > 1)))
                {
                    AutoSetAllEffectedControls(pointID);
                }

                return;
            }

            bool nextIsAnchor = (pointID + 1) % 3 == 0;
            int opositeID = nextIsAnchor ? pointID + 2 : pointID - 2;
            int anchorID = nextIsAnchor ? pointID + 1 : pointID - 1;

            if (opositeID >= 0 && opositeID < count || loop)
            {
                opositeID = loopID(opositeID);
                anchorID = loopID(anchorID);

                float distance = (ToWorld(points[anchorID]) - ToWorld(points[opositeID])).magnitude;
                Vector3 direction = (ToWorld(points[anchorID]) - ToWorld(newPos)).normalized;
                points[opositeID] = points[anchorID] + direction * distance;
            }
        }

        /// <summary>
        /// Calculate baked path points
        /// </summary>
        public void UpdateSpacedPoints()
        {
            if (item.values.pathLerpMode != PathLerpMode.baked) return;
            spacedPoints = TSSPathBase.GetSpacedPoints(points.ToArray(), spacing, resolution);
        }

        private void AutoSetAllEffectedControls(int anchorID)
        {
            for (int i = anchorID - 3; i <= anchorID + 3; i+=3)
            {
                if (i >= 0 && i < count || loop) AutoSetControl(loopID(i));
            }

            AutoSetLoopEnds();
        }

        private void AutoSetAllControls()
        {
            for (int i = 0; i < count; i+=3) AutoSetControl(i);

            AutoSetLoopEnds();
            UpdateSpacedPoints();
        }

        private void AutoSetControl(int anchorID)
        {
            Vector3 anchorPos = points[anchorID];
            Vector3 direction = Vector3.zero;
            float[] distances = new float[2];
            
            if (anchorID - 3 >= 0 || loop)
            {
                Vector3 offset = points[loopID(anchorID - 3)] - anchorPos;
                direction += offset.normalized;
                distances[0] = offset.magnitude;
            }

            if (anchorID + 3 >= 0 || loop)
            {
                Vector3 offset = points[loopID(anchorID + 3)] - anchorPos;
                direction -= offset.normalized;
                distances[1] =-offset.magnitude;
            }

            direction.Normalize();

            for (int i = 0; i < 2; i++)
            {
                int pointID = anchorID + i * 2 - 1;
                if (pointID >= 0 && pointID < count || loop) points[loopID(pointID)] = anchorPos + direction * distances[i] * smoothFactor;
            }

        }

        private void AutoSetLoopEnds()
        {
            if (loop) return;

            points[1] = (points[0] + points[2]) * 0.5f;
            points[count - 2] = (last + points[count - 3]) * 0.5f;

        }

        private int loopID(int pointID)
        {
            return (pointID + count) % count;
        }

        private void OnDrawGizmos()
        {
            
        }

        #endregion

        #region Inity Update

        private void OnEnable()
        {
            TSSPathBase.allPathes.Add(this);
        }

        private void OnDisable()
        {
            TSSPathBase.allPathes.Remove(this);
        }

        private void Start()
        {
            Init();
        }

        public void Refresh()
        {
            item = GetComponent<TSSItem>();
        }

        private void Init()
        {
            Refresh();
        }

        public void Reset()
        {
            Init();
            points = TSSPathBase.GetDefaultPath(this);
            resolution = 1;
            spacing = 10;
            loop = false;
            auto = true;
            UpdateSpacedPoints();

            pointsAttach = new List<Transform>();

            if (item.values.pathLerpMode == PathLerpMode.dynamic)
            {
                pointsAttach.Add(null);
                pointsAttach.Add(null);
            }
        }

        /// <summary>
        /// Path points can be dynamically move if path are dynamic (affected on performance)
        /// </summary>
        public void UpdatePath()
        {
            if (item.values.pathLerpMode == PathLerpMode.baked || pointsAttach == null || pointsAttach.Count == 0) return;

            for (int i = 0; i < pointsAttach.Count; i++)
            {
                if (pointsAttach[i] == null) continue;

                Vector3 newPointPosition = ToLocal(pointsAttach[i].position);

                if (points[i * 3] == newPointPosition) continue;

                points[i * 3] = newPointPosition;
                if (auto) AutoSetAllEffectedControls(i * 3);
            }
        }

        #endregion

        #region Transforms

        private Vector3 ToLocal(Vector3 worldPoint)
        {
            return TSSPathBase.ToLocal(this, worldPoint);
        }

        private Vector3 ToWorld(Vector3 localPoint)
        {
            return TSSPathBase.ToWorld(this, localPoint);
        }

        private Quaternion ToWorld(Quaternion localRotation)
        {
            return TSSPathBase.ToWorld(this, localRotation);
        }

        private Quaternion ToLocal(Quaternion worldRotation)
        {
            return TSSPathBase.ToLocal(this, worldRotation);
        }

        #endregion
    }
}