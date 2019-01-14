// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru
// MIT License

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using TSS.Base;

namespace TSS.Editor
{
    [CustomEditor(typeof(TSSPath))]
    public class TSSPathEditor : UnityEditor.Editor
    {
        #region Properties

        private TSSPath path;
        private SerializedObject serializedPath;
        private static bool syncJoints = true;
        private static List<int> selection = new List<int>();
        private const float segmentSelectTreshold = 500.1f;
        private static int selectedSegmentID = -1;

        private static GUIContent syncJointsContent = new GUIContent("Sync Joints", "Nearest points are affected by the moving point"),
                                  loopModeContent = new GUIContent("Loop Mode", "Path becomes looped or opened"),
                                  autoModeContent = new GUIContent("Auto Mode", "Path is adjusted automatically by anchor points"),
                                  smoothFactorContent = new GUIContent("Smooth Factor", "factor of automatically smoothing adjusted path"),
                                  qualitySpacingContent = new GUIContent("Spacing", "Count of travel points on path\n\nDirectly affects performance!"),
                                  qualityResolutionContent = new GUIContent("Resolution", "Threshold for placing travel points on path\n\nDirectly affects performance!"),
                                  lerpModeContent = new GUIContent("Interpolation", "Linear:\nInterpolation by item position tween (Much faster than cubic)\n\nCubic:\ninterpolation by item position tween and bezier effect (Affecting on performance!)"),
                                  addSegmentButtonContent = new GUIContent("Add Segment", "Add new segment to the path"),
                                  selectAllButtonContent = new GUIContent("Select All", "Select all anchor points of the path"),
                                  deleteSelectedButtonContent = new GUIContent("Delete Selected", "Delete all selected points of the path"),
                                  resetPathButtonContent = new GUIContent("Reset Path", "Reset all path values to default"),
                                  bakeButtonContent = new GUIContent("Bake", "Recalculate baked path"),
                                  editModeButtonContent,
                                  selectSegmentHelpContent = new GUIContent("LMB  -  Select point"),
                                  selectGroupSegmentHelpContent = new GUIContent("Ctrl + LMB  -  Select group of points"),
                                  newSegmentHelpContent = new GUIContent("Shift + LMB  -  New segment or delete point");

        private static bool editMode;

        private static float handle2DScaler = 1.0f;

        private static bool backFocus = false;

        private static AnimBool foldOutAttachPoints;

        #endregion

        #region Unit & Deinit

        private void OnEnable()
        {
            path = (TSSPath)target;
            serializedPath = new SerializedObject(path);
            selection.Clear();
            editModeButtonContent = EditorGUIUtility.IconContent("EditCollider");
            editMode = false;
            path.gizmoSize = 0.033f;

            foldOutAttachPoints = new AnimBool(false);
            foldOutAttachPoints.valueChanged.AddListener(Repaint);
        }

        private void OnDisable()
        {
            selection.Clear();

            editMode = false;

        }

        #endregion

        #region Path points stuff

        private void AddPointToEnd()
        {
            if (selectedSegmentID != -1) { AddSplitPoint(); return; }

            AddPoint(path.last - (path.last - path[path.count - 3]));
        }

        private void AddPoint(Vector3 position)
        {
            Undo.RecordObject(path.item, "[TSS Path] Add segment");
            Undo.RecordObject(path, "[TSS Path] Split segment");

            if (selectedSegmentID != -1)
            {
                Vector3[] segmentPoints = ToWorld(path.GetSegmentPoints(selectedSegmentID));
                position = ToLocal(TSSPathBase.EvaluateSegment(segmentPoints[0], segmentPoints[3], segmentPoints[1], segmentPoints[2], 0.5f));
            }

            path.AddSegment(position);

            selection.Clear();
            selection.Add(path.count - 1);

            backFocus = true;
        }

        private void AddSplitPoint()
        {
            if (selectedSegmentID == -1) AddPointToEnd();
            Vector3[] segmentPoints = ToWorld(path.GetSegmentPoints(selectedSegmentID));
            AddSplitPoint(ToLocal(TSSPathBase.EvaluateSegment(segmentPoints[0], segmentPoints[3], segmentPoints[1], segmentPoints[2], 0.5f)));
        }

        private void AddSplitPoint(Vector3 position)
        {
            Undo.RecordObject(path.item, "[TSS Path] Split segment");
            Undo.RecordObject(path, "[TSS Path] Split segment");
            path.SplitSegment(position, selectedSegmentID);

            selection.Clear();
            selection.Add(selectedSegmentID);
        }

        private void SelectAllPoints()
        {
            Undo.RecordObject(this, "[TSS Path] Selection");

            selection.Clear();

            for (int i = 0; i < path.count; i += 3) selection.Add(i);
        }

        private void ResetPath()
        {
            Undo.RecordObject(path.item, "[TSS Path] Reset path");

            path.Reset();
            selection.Clear();
        }

        private void DeleteSelectedPoints()
        {
            Undo.RecordObject(path.item, "[TSS Path] Delete segments");
            Undo.RecordObject(path, "[TSS Path] Split segment");

            List<Vector3> positionSelection = new List<Vector3>();

            for (int i = 0; i < selection.Count; i++)
                if (selection[i] % 3 == 0) positionSelection.Add(path.points[selection[i]]);

            selection.Clear();

            for (int i = 0; i < path.count; i++)
                for (int j = 0; j < positionSelection.Count; j++)
                    if (positionSelection[j] == path[i] && path.segmentsCount > 1) path.DeleteSegment(i);

        }

        #endregion

        #region Inspector GUI

        public override void OnInspectorGUI()
        {
            serializedPath.Update();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(" ");
            editMode = GUILayout.Toggle(editMode, editModeButtonContent, "Button", TSSEditorUtils.fixed35pxWidth, TSSEditorUtils.fixed25pxHeight);
            EditorGUILayout.LabelField("Edit Item Path", EditorStyles.label, TSSEditorUtils.fixed25pxHeight);
            path.showGizmos = editMode;
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();

            GUILayout.Space(4);

            EditorGUI.BeginDisabledGroup(!editMode);

            TSSEditorUtils.DrawGenericProperty(ref syncJoints, syncJointsContent);

            bool loopMode = path.loop;
            TSSEditorUtils.DrawGenericProperty(ref loopMode, loopModeContent, path.item);
            if (loopMode != path.loop) { path.loop = loopMode; selection.Clear(); }

            bool autoMode = path.auto;
            TSSEditorUtils.DrawGenericProperty(ref autoMode, autoModeContent, path.item);
            if (autoMode != path.auto) { path.auto = autoMode; selection.Clear(); }

            if (path.auto)
            {
                float pathSmooth = path.smoothFactor;
                TSSEditorUtils.DrawSliderProperty(ref pathSmooth, path.item, smoothFactorContent, 0, 1);
                if (pathSmooth != path.smoothFactor) path.smoothFactor = pathSmooth;
            }

            bool isLinearLerp = path.item.values.pathLerpMode == PathLerpMode.baked;

            if (isLinearLerp) EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            TSSEditorUtils.DrawGenericProperty(ref path.item.values.pathLerpMode, lerpModeContent, path.item);

            if (isLinearLerp && path.item.values.pathLerpMode == PathLerpMode.dynamic)
            {
                Undo.RecordObject(path.item, "[TSS Path] Delete segments");
                Undo.RecordObject(path, "[TSS Path] Split segment");

                path.pointsAttach = new List<Transform>();
                path.pointsAttach.AddRange(new Transform[path.segmentsCount + 1]);
            }

            if (!isLinearLerp && path.item.values.pathLerpMode == PathLerpMode.baked)
            {
                Undo.RecordObject(path.item, "[TSS Path] Delete segments");
                Undo.RecordObject(path, "[TSS Path] Split segment");

                path.pointsAttach = null;
                path.UpdateSpacedPoints();
            }

            if (isLinearLerp && GUILayout.Button(bakeButtonContent, TSSEditorUtils.max80pxWidth, TSSEditorUtils.fixedLineHeight))
            {
                path.UpdateSpacedPoints();
            }

            EditorGUILayout.EndHorizontal();

            if (isLinearLerp)
            {
                GUILayout.Space(4);

                float pathSpacing = path.spacing;
                TSSEditorUtils.DrawSliderProperty(ref pathSpacing, path.item, qualitySpacingContent, 2, 100);
                if (pathSpacing != path.spacing) path.spacing = Mathf.CeilToInt(pathSpacing); ;

                float pathResolution = path.resolution;
                TSSEditorUtils.DrawSliderProperty(ref pathResolution, path.item, qualityResolutionContent, 1, 10);
                if (pathResolution != path.resolution) path.resolution = Mathf.CeilToInt(pathResolution);

            }

            if (isLinearLerp) EditorGUILayout.EndVertical();

            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(addSegmentButtonContent)) AddPointToEnd();

            if (GUILayout.Button(selectAllButtonContent)) SelectAllPoints();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(deleteSelectedButtonContent)) DeleteSelectedPoints();

            if (GUILayout.Button(resetPathButtonContent)) ResetPath();

            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();

            if (editMode)
            {
                if (path.item.values.pathLerpMode == PathLerpMode.dynamic) DrawAttachPointsPanel();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUI.color = Color.white * 0.9f;
                EditorGUILayout.LabelField(selectSegmentHelpContent, EditorStyles.miniLabel);
                EditorGUILayout.LabelField(selectGroupSegmentHelpContent, EditorStyles.miniLabel);
                EditorGUILayout.LabelField(newSegmentHelpContent, EditorStyles.miniLabel);

                EditorGUILayout.EndVertical();
            }

            EditorGUI.EndDisabledGroup();

            serializedPath.ApplyModifiedProperties();
        }

        #endregion

        #region Scene GUI

        private void OnSceneGUI()
        {
            if (!path.enabled) return;

            if (backFocus)
            {
                backFocus = false;
                Selection.objects = new Object[0] { };
                Selection.SetActiveObjectWithContext(path.gameObject, path);
            }

            Input();
            DrawPath();
        }

        private void Input()
        {
            if (SceneView.lastActiveSceneView != null)
            {
                handle2DScaler = (SceneView.lastActiveSceneView.in2DMode ? 1.0f : 0.165f);
                path.gizmoSize = (SceneView.lastActiveSceneView.in2DMode ? 1.0f : 0.33f);
            }

            for (int i = 0; i < selection.Count; i++) if (selection[i] < 0 || selection[i] >= path.count) { selection.Clear(); break; }

            if (!editMode) return;

            Event guiEvent = Event.current;

            Vector3 newPosition = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition).origin;

            int selectedPointID = -1;

            bool mouseClicked = false;

            for (int i = 0; i < path.count; i++)
            {
                if (path.auto && i % 3 != 0 || (path.auto && i % 3 != 0 && path.smoothFactor == 0)) continue;

                if (Handles.Button(ToWorld(path[i]), Quaternion.identity, 15f * handle2DScaler, 15f * handle2DScaler, Handles.SphereCap))
                {
                    selectedPointID = i;
                    mouseClicked = true;
                    Repaint();
                }
            }

            if (!mouseClicked)
            {
                float minDistane = segmentSelectTreshold;
                int newSelectedSegmentID = -1;

                if (guiEvent.type == EventType.MouseMove)
                {
                    for (int i = 0; i < path.segmentsCount; i++)
                    {
                        Vector3[] points = ToWorld(path.GetSegmentPoints(i));
                        float distance = HandleUtility.DistancePointBezier(newPosition, points[0], points[3], points[1], points[2]);

                        if (distance < minDistane)
                        {
                            minDistane = distance;
                            newSelectedSegmentID = i;
                        }
                    }

                    if (newSelectedSegmentID != selectedSegmentID)
                    {
                        selectedSegmentID = newSelectedSegmentID;
                        HandleUtility.Repaint();
                    }

                    return;
                }

                if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0)
                {
                    path.UpdateSpacedPoints();
                    return;
                }

                if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.shift)
                {
                    if (selectedSegmentID != -1)
                    {
                        AddSplitPoint();
                        return;
                    }

                    if (path.loop) return;

                    newPosition.z = path.last.z + (path.last.z - path[path.count - 1].z) * 0.5f;
                    AddPoint(ToLocal(newPosition));
                }

                return;
            }

            if (guiEvent.shift)
            {
                Undo.RecordObject(path.item, "[TSS Path] Delete segment");
                if (selectedPointID % 3 == 0) path.DeleteSegment(selectedPointID);
                selection.Clear();
                return;
            }

            if (guiEvent.control)
            {
                if (!selection.Contains(selectedPointID)) selection.Add(selectedPointID);
                else selection.Remove(selectedPointID);
                return;
            }

            selection.Clear();
            selection.Add(selectedPointID);
        }

        #endregion

        #region Drawing

        private void DrawPath()
        {
            for (int i = 0; i < path.segmentsCount; i++)
            {
                Vector3[] segmentPoints = ToWorld(path.GetSegmentPoints(i));

                Handles.color = Color.white;

                Color segmentColor = editMode ? (i == selectedSegmentID ? Color.yellow : Color.green) : Color.white;

                Handles.DrawBezier(segmentPoints[0], segmentPoints[3], segmentPoints[1], segmentPoints[2], segmentColor, null, 2);

                if (path.auto || !editMode) continue;

                Handles.color = Color.gray;
                Handles.DrawLine(segmentPoints[0], segmentPoints[1]);
                Handles.DrawLine(segmentPoints[2], segmentPoints[3]);
            }

            if (!editMode) return;

            for (int i = 0; i < selection.Count; i++)
            {
                Handles.color = Color.white;
                Vector3 pointPos = ToWorld(path[selection[i]]);
                Vector3 newPos = Handles.PositionHandle(pointPos, Quaternion.identity);
                Vector3 posDelta = newPos - pointPos;

                if (posDelta == Vector3.zero) continue;

                Undo.RecordObject(path.item, "[TSS Path] Point position");
                for (int j = 0; j < selection.Count; j++) path.SetPointPos(selection[j], ToLocal(newPos), syncJoints);
            }

        }

        public void DrawAttachPointsPanel()
        {
            GUILayout.Space(3);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            foldOutAttachPoints.target = EditorGUILayout.Foldout(foldOutAttachPoints.target, "   Attach points", true, GUI.skin.label);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(3);

            if (foldOutAttachPoints.faded > 0)
            {
                EditorGUILayout.BeginFadeGroup(foldOutAttachPoints.faded);

                for (int i = 0; i < path.pointsAttach.Count; i++)
                {
                    EditorGUI.BeginDisabledGroup(!selection.Contains(i * 3));

                    Transform editingTransform = path.pointsAttach[i];
                    TSSEditorUtils.DrawGenericProperty(ref editingTransform, path);
                    if (editingTransform != path.pointsAttach[i]) path.pointsAttach[i] = editingTransform;

                    EditorGUI.EndDisabledGroup();
                }

                EditorGUILayout.EndFadeGroup();
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(3);
        }

        #endregion

        #region Transforms

        private static Vector3[] ToWorld(TSSPath path, Vector3[] localPoints)
        {
            Vector3[] worldPoints = new Vector3[localPoints.Length];
            for (int i = 0; i < worldPoints.Length; i++) worldPoints[i] = TSSPathBase.ToWorld(path, localPoints[i]);
            return worldPoints;
        }

        private Vector3[] ToWorld(Vector3[] localPoints)
        {
            Vector3[] worldPoints = new Vector3[localPoints.Length];
            for (int i = 0; i < worldPoints.Length; i++) worldPoints[i] = ToWorld(localPoints[i]);
            return worldPoints;
        }

        private Vector3[] ToLocal(Vector3[] worldPoints)
        {
            Vector3[] localPoints = new Vector3[worldPoints.Length];
            for (int i = 0; i < localPoints.Length; i++) localPoints[i] = ToLocal(worldPoints[i]);
            return localPoints;
        }

        private Vector3 ToLocal(Vector3 worldPoint)
        {
            return TSSPathBase.ToLocal(path, worldPoint);
        }

        private Vector3 ToWorld(Vector3 localPoint)
        {
            return TSSPathBase.ToWorld(path, localPoint);
        }

        #endregion
    }
}