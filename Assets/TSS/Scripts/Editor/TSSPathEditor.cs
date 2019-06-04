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
    [CustomEditor(typeof(TSSPath))]
    public class TSSPathEditor : UnityEditor.Editor
    {
        #region Properties

        private TSSPath path;
        private SerializedObject serializedPath;
        private static bool syncJoints = true;
        private static List<int> selection = new List<int>();
        private const float segmentSelectTreshold = 1000.1f;
        private static int selectedSegmentID = -1;

        private static GUIContent syncJointsContent = new GUIContent("Sync Joints", "Nearest points are affected by the moving point"),
                                  loopModeContent = new GUIContent("Loop Mode", "Path becomes looped or opened"),
                                  autoModeContent = new GUIContent("Auto Mode", "Path is adjusted automatically by anchor points"),
                                  smoothFactorContent = new GUIContent("Smooth Factor", "factor of automatically smoothing adjusted path"),
                                  qualitySpacingContent = new GUIContent("Spacing", "Count of travel points on path\n\nDirectly affects performance!"),
                                  qualityResolutionContent = new GUIContent("Resolution", "Threshold for placing travel points on path\n\nDirectly affects performance!"),
                                  lerpModeContent = new GUIContent("Interpolation", "Linear:\nInterpolation by item position tween (Much faster than cubic)\n\nCubic:\ninterpolation by item position tween and bezier effect (Affecting on performance!)"),
                                  addSegmentStartButtonContent = new GUIContent("Add point to start", "Add new segment to the path start"),
                                  addSegmentEndButtonContent = new GUIContent("Add point to end", "Add new segment to the path end"),
                                  selectAllButtonContent = new GUIContent("Select All", "Select all anchor points of the path"),
                                  selectPathButtonContent = new GUIContent("Select path", "Select all anchor points of the path"),
                                  deleteSelectedButtonContent = new GUIContent("Delete Selected", "Delete all selected points of the path"),
                                  resetPathButtonContent = new GUIContent("Reset Path", "Reset all path values to default"),
                                  bakeButtonContent = new GUIContent("Bake", "Recalculate baked path"),
                                  editModeButtonContent,
                                  selectSegmentHelpContent = new GUIContent("LMB  -  Select point"),
                                  selectGroupSegmentHelpContent = new GUIContent("Ctrl + LMB  -  Select group of points"),
                                  newSegmentHelpContent = new GUIContent("Shift + LMB  -  New segment or delete point"),
                                  projectionMaskContent = new GUIContent("Projection", "Project all points on vector"),
                                  editPointPositionContent = new GUIContent("Selected points position", "Manualy set selected point positions");

        private static bool editMode;

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

            AddPoint(false);
        }

        private void AddPoint(bool toStart = true)
        {
            Undo.RecordObjects(new Object[] { path, path.item }, "[TSS Path] Split segment");


            Vector3 position = toStart ?
                    path[0] - (path[1] - path[0]) * 3 :
                    path.last + (path.last - path[path.count - 2]) * 3;

            path.AddSegment(position, toStart);


            selection.Clear();
            selection.Add(toStart ? 0 : path.count - 1);
        }

        private void AddSplitPoint()
        {
            if (selectedSegmentID == -1) AddPointToEnd();
            Vector3[] segmentPoints = ToWorld(path.GetSegmentPoints(selectedSegmentID));
            AddSplitPoint(ToLocal(TSSPathBase.EvaluateSegment(segmentPoints[0], segmentPoints[3], segmentPoints[1], segmentPoints[2], 0.5f)));
        }

        private void AddSplitPoint(Vector3 position)
        {
            Undo.RecordObjects(new Object[] { path, path.item }, "[TSS Path] Split segment");

            path.SplitSegment(position, selectedSegmentID);

            selection.Clear();
            selection.Add(selectedSegmentID);
        }

        private void SelectAllPoints(int step = 3)
        {
            Undo.RecordObject(this, "[TSS Path] Selection");

            selection.Clear();

            for (int i = 0; i < path.count; i += step) selection.Add(i);
        }



        private void ResetPath()
        {
            Undo.RecordObject(path.item, "[TSS Path] Reset path");

            path.Reset();
            selection.Clear();
        }

        private void DeleteSelectedPoints()
        {
            Undo.RecordObjects(new Object[] { path, path.item }, "[TSS Path] Delete segments");

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

            DrawEditModeButtonGUI();

            EditorGUI.BeginDisabledGroup(!editMode);

            DrawModesGUI();
            DrawLerpGUI();
            DrawControlButtonsGUI();
            /*DrawProjectionToolBarGUI(); <-- !DISABLED (DrawSelectedPointsGUI() functionality cover this feature)*/
            DrawSelectedPointsGUI();
            DrawHelpboxGUI();

            EditorGUI.EndDisabledGroup();

            serializedPath.ApplyModifiedProperties();
        }

        private void DrawEditModeButtonGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(" ");
            editMode = GUILayout.Toggle(editMode, editModeButtonContent, "Button", TSSEditorUtils.fixed35pxWidth, TSSEditorUtils.fixed25pxHeight);
            EditorGUILayout.LabelField("Edit Item Path", EditorStyles.label, TSSEditorUtils.fixed25pxHeight);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawModesGUI()
        {
            EditorGUI.BeginChangeCheck();

            GUILayout.Space(4);

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
        }

        private void DrawLerpGUI()
        {
            bool isLinearLerp = path.item.values.pathLerpMode == PathLerpMode.baked;

            if (isLinearLerp) EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            TSSEditorUtils.DrawGenericProperty(ref path.item.values.pathLerpMode, lerpModeContent, path.item);

            if (isLinearLerp && path.item.values.pathLerpMode == PathLerpMode.dynamic)
            {
                Undo.RecordObjects(new Object[] { path, path.item }, "[TSS Path] Delete segments");

                path.pointsAttach = new List<Transform>();
                path.pointsAttach.AddRange(new Transform[path.segmentsCount + 1]);
            }

            if (!isLinearLerp && path.item.values.pathLerpMode == PathLerpMode.baked)
            {
                Undo.RecordObjects(new Object[] { path, path.item }, "[TSS Path] Split segment");

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
        }

        private void DrawControlButtonsGUI()
        {
            GUILayoutOption buttonOption = GUILayout.Width((EditorGUIUtility.currentViewWidth - 42) / 2);

            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(addSegmentStartButtonContent, buttonOption)) AddPoint(true);
                if (GUILayout.Button(addSegmentEndButtonContent, buttonOption)) AddPoint(false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(selectAllButtonContent, buttonOption)) SelectAllPoints();
                if (GUILayout.Button(selectPathButtonContent, buttonOption)) SelectAllPoints(1);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(deleteSelectedButtonContent, buttonOption)) DeleteSelectedPoints();
                if (GUILayout.Button(resetPathButtonContent, buttonOption)) ResetPath();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawProjectionToolBarGUI()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(projectionMaskContent, GUILayout.MaxWidth(98));

            if (GUILayout.Button("X", EditorStyles.miniButtonLeft, TSSEditorUtils.max18pxWidth))
            {
                Undo.RecordObject(path.item, "[TSS Path] projection");
                path.Project(new Vector3(0, 1, 1));
            }
            if (GUILayout.Button("Y", EditorStyles.miniButtonMid, TSSEditorUtils.max18pxWidth))
            {
                Undo.RecordObject(path.item, "[TSS Path] projection");
                path.Project(new Vector3(1, 0, 1));
            }
            if (GUILayout.Button("Z", EditorStyles.miniButtonRight, TSSEditorUtils.max18pxWidth))
            {
                Undo.RecordObject(path.item, "[TSS Path] projection");
                path.Project(new Vector3(1, 1, 0));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSelectedPointsGUI()
        {
            if (selection.Count == 0) return;

            if (selection.Count == 1)
            {
                Vector3 newPos = path[selection[0]];

                EditorGUI.BeginChangeCheck();

                newPos = EditorGUILayout.Vector3Field(editPointPositionContent, newPos);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(path.item, "[TSS Path] point position");

                    path.SetPointPos(selection[0], newPos, syncJoints);
                    path.UpdateSpacedPoints();
                }
            }
            else if (selection.Count > 0)
            {
                bool xMixed = false;
                bool yMixed = false;
                bool zMixed = false;

                Vector3 oldPos = path[selection[0]];
                Vector3 newPos = Vector3.zero;

                for (int i = 1; i < selection.Count; i++)
                {
                    if (!xMixed) xMixed = !Mathf.Approximately(path[selection[i]].x, oldPos.x);
                    if (!yMixed) yMixed = !Mathf.Approximately(path[selection[i]].y, oldPos.y);
                    if (!zMixed) zMixed = !Mathf.Approximately(path[selection[i]].z, oldPos.z);
                }

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PrefixLabel(editPointPositionContent);

                EditorGUIUtility.labelWidth = 12;

                EditorGUI.showMixedValue = xMixed;
                newPos.x = EditorGUILayout.FloatField("X", oldPos.x);
                EditorGUI.showMixedValue = false;

                EditorGUI.showMixedValue = yMixed;
                newPos.y = EditorGUILayout.FloatField("Y", oldPos.y);
                EditorGUI.showMixedValue = false;

                EditorGUI.showMixedValue = zMixed;
                newPos.z = EditorGUILayout.FloatField("Z", oldPos.z);
                EditorGUI.showMixedValue = false;

                EditorGUIUtility.labelWidth = 0;

                EditorGUILayout.EndHorizontal();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(path.item, "[TSS Path] points position");

                    bool xChg = !Mathf.Approximately(oldPos.x, newPos.x);
                    bool yChg = !Mathf.Approximately(oldPos.y, newPos.y);
                    bool zChg = !Mathf.Approximately(oldPos.z, newPos.z);

                    for (int i = 0; i < selection.Count; i++)
                    {
                        path.SetPointPos(selection[i], new Vector3
                            (
                                xChg ? newPos.x : path[selection[i]].x,
                                yChg ? newPos.y : path[selection[i]].y,
                                zChg ? newPos.z : path[selection[i]].z
                            ),
                        syncJoints);
                    }

                    path.UpdateSpacedPoints();
                }
            }
        }

        private void DrawHelpboxGUI()
        {
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

        }

        #endregion

        #region Scene GUI

        private void OnSceneGUI()
        {
            if (!path.enabled) return;

            Input();
            DrawPath();

            if (editMode && Event.current.type == EventType.Layout) HandleUtility.AddDefaultControl(0);
        }

        private void Input()
        {

            for (int i = 0; i < selection.Count; i++) if (selection[i] < 0 || selection[i] >= path.count) { selection.Clear(); break; }

            if (!editMode) return;

            Event guiEvent = Event.current;

            if (guiEvent.type == EventType.ValidateCommand && guiEvent.commandName == "UndoRedoPerformed")
            {
                path.UpdateSpacedPoints();
            }

            Vector3 newPosition = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition).origin;

            int selectedPointID = -1;

            bool mouseClicked = false;


            for (int i = 0; i < path.count; i++)
            {
                if (path.auto && i % 3 != 0 || (path.auto && i % 3 != 0 && path.smoothFactor == 0)) continue;

                float handleSize = (i % 3 == 0 ? 1 : 0.5f) * GetHandleScale(ToWorld(path[i]));
                Handles.color = i % 3 == 0 ? Color.white : Color.white * 0.75f;

                if (Handles.Button(ToWorld(path[i]), Quaternion.identity, handleSize, handleSize, Handles.SphereHandleCap))
                {
                    selectedPointID = i;
                    mouseClicked = true;
                    Repaint();
                }
            }

            if (guiEvent.keyCode == KeyCode.Escape)
            {
                selection.Clear();
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

        private static float GetHandleScale(Vector3 handlePosition)
        {
            float scale = AnnotationUtiltyWrapper.IconSizeLinear;
            if (!AnnotationUtiltyWrapper.use3dGizmos) scale *= HandleUtility.GetHandleSize(handlePosition) * 0.25f;
            else if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.in2DMode) scale *= 10;
            return scale;
        }

        #endregion

        #region Drawing

        private void DrawPath()
        {
            if (TSSPrefsEditor.showAllPaths)
            {
                for (int i = 0; i < TSSPathBase.allPathes.Count; i++)
                {
                    if (!TSSPathBase.allPathes[i].gameObject.activeInHierarchy ||
                        !TSSPathBase.allPathes[i].enabled ||
                         TSSPathBase.allPathes[i] == path ||
                        !TSSPathBase.allPathes[i].item.enabled) continue;

                    for (int j = 0; j < TSSPathBase.allPathes[i].segmentsCount; j++)
                    {
                        Vector3[] segmentPoints = ToWorld(TSSPathBase.allPathes[i], TSSPathBase.allPathes[i].GetSegmentPoints(j));

                        Handles.DrawBezier(segmentPoints[0], segmentPoints[3], segmentPoints[1], segmentPoints[2], Color.white * 0.85f, null, 2);
                    }
                }
            }

            if (editMode && path.lerpMode == PathLerpMode.baked)
            {
                Handles.color = Color.white * 0.6f;
                Handles.DrawPolyLine(ToWorld(path.spacedPoints));
                Handles.color = Color.white;

                for (int i = 0; i < path.spacedPoints.Length; i++)
                {
                    Vector3 worldPointPos = ToWorld(path.spacedPoints[i]);
                    Handles.DrawWireCube(worldPointPos, Vector3.one * GetHandleScale(worldPointPos) * 0.5f);
                }
            }
           
            for (int i = 0; i < path.segmentsCount; i++)
            {
                Vector3[] segmentPoints = ToWorld(path.GetSegmentPoints(i));

                Handles.color = Color.white;

                Color segmentColor = editMode ? (i == selectedSegmentID ? Color.yellow : Color.green) : Color.white;

                Handles.DrawBezier(segmentPoints[0], segmentPoints[3], segmentPoints[1], segmentPoints[2], segmentColor, null, 2);

                if (path.auto || !editMode) continue;

                Handles.color = Color.white * 0.9f;
                Handles.DrawLine(segmentPoints[0], segmentPoints[1]);
                Handles.DrawLine(segmentPoints[2], segmentPoints[3]);
            }
          

            if (!editMode) return;

            for (int i = 0; i < selection.Count; i++)
            {

                Handles.color = Color.white;
                Vector3 pointPos = ToWorld(path[selection[i]]);
                Vector3 posDelta = Handles.PositionHandle(pointPos, Quaternion.identity) - pointPos;

                if (posDelta == Vector3.zero) continue;

                Undo.RecordObject(path.item, "[TSS Path] Point position");
                for (int j = 0; j < selection.Count; j++) path.SetPointPos(selection[j], ToLocal(ToWorld(path[selection[j]]) + posDelta), syncJoints);
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