// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru/tss
// MIT License

using System;
using UnityEngine;
using UnityEditor;
using TSS.Base;

namespace TSS.Editor
{
    public class TSSTimeLineEditor : EditorWindow
    {
        #region Enumerations

        public enum Mode { open, close, openClode}

        public static ItemKey direction;
        public static Mode mode;

        #endregion

        #region Properties

        public static TSSItem item;
        public static int lastItemCount;
        public static int itemCount;

        private static float timeLineOpenPeriod, timeLineClosePeriod;

        private static float timeLinePrecision = 0.1f;
        private static bool timeLinePlaying;
        private static float timeLinePlayingLastTime;

        private static GUIStyle itemStyle, itemSelectedStyle, itemChainStyle;
        private static GUIStyle itemTitleStyle;
        private static GUIStyle centeredGreyStyle;
        private static GUIStyle rightWhiteStyle;
        private static GUIStyle centeredWhiteStyle;

        private static TSSItem selectedItem;
        private static Rect selectedItemRect;
        private static Rect timeLineRect;

        private static float itemHeight = 18;
        private static float itemHeightScale = 1.0f;
        private static int itemHandleSize = 10;
        private static int itemLineSpacing = 2;
        private static bool lastBackColor;

        private static float controlLineHeight = 50;
        private static float controlLineOffset = 23;
        private static int controlLineHandlerSize = 12;
        private static float controlLineHandlerPosition;
        private static float controlLineSelectionPosition;
        private static float controlLineSelectionSize;
        private static bool controlLineHandlerSelected;

        private static GUIStyle controlLineHeaderStyle;
        private static GUIStyle controlLineStyle;
        private static Vector2 controlButtonSize = new Vector2(30, 18);

        private static bool boolMousePressed;
        private static Vector2 mousePosition;

        #endregion

        #region Init & Deinit

        [MenuItem("Window/TSS/TimeLine", false, 10)]
        static void OpenTimeLineWindow(MenuCommand menuCommand)
        {
            EditorWindow window = GetWindow(typeof(TSSTimeLineEditor), false);
            window.titleContent.text = "TSS TimeLine";

            if (Selection.transforms.Length == 0) return;

            TSSItem selectedItem = Selection.transforms[0].gameObject.GetComponent<TSSItem>();

            if (selectedItem == null) return;

            item = selectedItem;
            selectedItem.state = ItemState.slave;
            mode = Mode.open;
        }
        
        private void OnEnable()
        {
            itemStyle = new GUIStyle();
            itemStyle.normal.background = TSSEditorTextures.timeLineItem;
            itemStyle.border = new RectOffset(12, 12, 1, 1);

            centeredGreyStyle = new GUIStyle();
            centeredGreyStyle.alignment = TextAnchor.UpperCenter;
            centeredGreyStyle.normal.textColor = Color.grey;

            rightWhiteStyle = new GUIStyle();
            rightWhiteStyle.alignment = TextAnchor.UpperLeft;
            rightWhiteStyle.normal.textColor = Color.white;

            centeredWhiteStyle = new GUIStyle();
            centeredWhiteStyle.alignment = TextAnchor.UpperCenter;
            centeredWhiteStyle.normal.textColor = Color.white;

            controlLineHeaderStyle = new GUIStyle();
            controlLineHeaderStyle.normal.background = TSSEditorTextures.timeLineHeader;
            controlLineHeaderStyle.border = new RectOffset(12, 12, 1, 1);

            controlLineStyle = new GUIStyle();
            controlLineStyle.normal.background = TSSEditorTextures.timeLineControlLine;

            itemSelectedStyle = new GUIStyle();
            itemSelectedStyle.normal.background = TSSEditorTextures.timeLineSelectedItem;
            itemSelectedStyle.border = itemStyle.border;

            itemChainStyle = new GUIStyle();
            itemChainStyle.normal.background = TSSEditorTextures.timeLineChainItem;
            itemChainStyle.border = itemStyle.border;

            itemTitleStyle = new GUIStyle();
            itemTitleStyle.normal.textColor = Color.white;
            itemTitleStyle.fontSize = 9;

            EditorApplication.playModeStateChanged += EditorPlayModeChanged;

            controlLineSelectionPosition = 0;
            controlLineSelectionSize = 0;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= EditorPlayModeChanged;

            item = null;
        }

        private void EditorPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode && item != null) item = null;
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            if (item == null) return;

            Handles.BeginGUI();

            boolMousePressed = false;

            itemHeight = Mathf.Clamp((position.height - controlLineHeight) / (itemCount + 2), 16, 30) * itemHeightScale;
            timeLineRect = new Rect(0, controlLineHeight, position.width, position.height - controlLineHeight);

            SetPeriods();
            
            DrawTimeLines();
            DrawSelection();
            UpdatePlaying();
            DrawControls();
        }

        #endregion

        #region Drawing

        private void DrawTimeLines()
        {
            switch (mode)
            {
                case Mode.open:
                    UpdateInput(); DrawTimeLine(timeLineRect, ItemKey.opened); break;

                case Mode.close:
                    UpdateInput(); DrawTimeLine(timeLineRect, ItemKey.closed); break;

                case Mode.openClode:
                    timeLineRect.width = position.width * 0.5f;

                    UpdateInput();

                    timeLineRect.x = 0;
                    DrawTimeLine(timeLineRect, ItemKey.opened);
                    timeLineRect.x = position.width * 0.5f;
                    DrawTimeLine(timeLineRect, ItemKey.closed);

                    EditorGUI.DrawRect(new Rect(timeLineRect.width - 1, controlLineOffset, 2, position.height), Color.white * 0.75f);

                    break;
            }
        }

        private void DrawTimeLine(Rect timeLineRect, ItemKey timeLineDirection)
        {
            direction = timeLineDirection;
            itemCount = 0;
            lastBackColor = false;

            DrawControlLine(new Rect(timeLineRect.x, timeLineRect.y, timeLineRect.width, controlLineHeight));
            DrawGrid(timeLineRect.width / GetPeriod() * timeLinePrecision, timeLineRect, 0.25f, Color.grey);
            DrawGrid(timeLineRect.width / GetPeriod(), timeLineRect, 0.5f, Color.grey);


            DrawItemOnTimeLine(item, new Vector2(timeLineRect.x + GetItemBeforeOffset(item), timeLineRect.y));
        }

        private void DrawControlLine(Rect controlRect)
        {
            Color controlLineColor = Color.black;
            controlLineColor.a = 0.5f;

            Rect controlLineRect = new Rect(controlRect.x, controlLineOffset, controlRect.width, controlLineHeight - controlLineOffset);
            GUI.Box(controlLineRect, string.Empty, controlLineStyle);

            Rect gridRect = new Rect(controlRect.x, controlRect.y - 8, controlRect.width, 8);
            DrawGrid(controlRect.width / GetPeriod() * timeLinePrecision, gridRect, 0.4f, Color.white);
            gridRect = new Rect(controlRect.x, controlRect.y - 16, controlRect.width, 16);
            DrawGrid(controlRect.width / GetPeriod(), gridRect, 0.7f, Color.white);

            EditorGUI.DrawRect(new Rect(controlRect.x, controlLineHeight - 1, controlRect.width, 1),  direction == ItemKey.opened ? TSSEditorUtils.greenColor : TSSEditorUtils.redColor);

            Rect controlLineHandlerRect = new Rect(controlLineHandlerPosition - controlLineHandlerSize / 2, controlLineHeight - controlLineHandlerSize, controlLineHandlerSize, controlLineHandlerSize);
            GUI.DrawTexture(controlLineHandlerRect, TSSEditorTextures.handlerIcon, ScaleMode.StretchToFill);
        }

        private void DrawControls()
        {
            Mode tmpMode = mode;
            mode = (Mode)GUI.Toolbar(new Rect(10, 3, 140, 17), (int)mode, new string[] { "open", "close", "split" });
            if (mode != tmpMode)
            {
                controlLineSelectionPosition = 0;
                controlLineSelectionSize = 0;
            }

            itemHeightScale = GUI.Slider(new Rect(position.width - 200, 3, 190, 17), itemHeightScale, 0, 0.0f, 1.0f, GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb, true, 0);

            Handles.color = new Color(1, 1, 1, 1);
            Handles.DrawLine(new Vector3(controlLineHandlerPosition, controlLineHeight, 0), new Vector3(controlLineHandlerPosition, position.height, 0));

            Handles.EndGUI();

            Repaint();
        }

        private void UpdatePlaying()
        {
            if (mode == Mode.open) direction = ItemKey.opened;
            else if (mode == Mode.close) direction = ItemKey.closed;
            else
            {
                if (controlLineHandlerPosition < position.width * 0.5f) direction = ItemKey.opened;
                else direction = ItemKey.closed;
            }

            if (timeLinePlaying)
            {
                if (GUI.Button(new Rect((position.width - controlButtonSize.x) * 0.5f, 3, controlButtonSize.x, controlButtonSize.y), TSSEditorTextures.stopIcon)) timeLinePlaying = false;
                if (!controlLineHandlerSelected) controlLineHandlerPosition += timeLineRect.width * (((float)EditorApplication.timeSinceStartup - timeLinePlayingLastTime) / GetPeriod());

                if (controlLineSelectionSize != 0)
                {
                    if (controlLineHandlerPosition < controlLineSelectionPosition
                        || controlLineHandlerPosition > controlLineSelectionPosition + controlLineSelectionSize)
                        controlLineHandlerPosition = controlLineSelectionPosition;
                }
                else if (controlLineHandlerPosition >= position.width)
                controlLineHandlerPosition = 0;

                timeLinePlayingLastTime = (float)EditorApplication.timeSinceStartup;
            }
            else
            {
                if (GUI.Button(new Rect((position.width - controlButtonSize.x) * 0.5f, 3, controlButtonSize.x, controlButtonSize.y), TSSEditorTextures.playIcon))
                {
                    timeLinePlaying = true;
                    timeLinePlayingLastTime = (float)EditorApplication.timeSinceStartup;
                }
            }
        }

        private void DrawSelection()
        {
            if (boolMousePressed && mousePosition.y > controlLineHeight)
            {
                selectedItem = null;
                controlLineSelectionPosition = mousePosition.x;
                controlLineSelectionSize = 0;
            }

            if (controlLineSelectionSize == 0) return;

            if (mode == Mode.open)
            {
                direction = ItemKey.opened;
            }
            else if (mode == Mode.close)
            {
                direction = ItemKey.closed;
            }
            else
            {
                float width = position.width * 0.5f;

                if (controlLineSelectionPosition < width)
                {
                    direction = ItemKey.opened;
                    if (controlLineSelectionPosition + controlLineSelectionSize > width) controlLineSelectionSize = width - controlLineSelectionPosition;
                }
                else
                {
                    direction = ItemKey.closed;
                    if (controlLineSelectionPosition + controlLineSelectionSize < width) controlLineSelectionSize = (controlLineSelectionPosition - width) *-1; 
                }
            }

            if (controlLineSelectionPosition + controlLineSelectionSize < 0) controlLineSelectionSize = -controlLineSelectionPosition;
            if (controlLineSelectionPosition + controlLineSelectionSize > position.width) controlLineSelectionSize = position.width - controlLineSelectionPosition;

            float duration = Mathf.Abs(controlLineSelectionSize) / timeLineRect.width * GetPeriod();
            float delayOffset = mode == Mode.openClode && direction == ItemKey.closed ? timeLineRect.x : 0;
            float delay = ((controlLineSelectionSize > 0 ? controlLineSelectionPosition : controlLineSelectionPosition + controlLineSelectionSize) - delayOffset) / timeLineRect.width * GetPeriod();

            Handles.Label(new Vector2(controlLineSelectionPosition + controlLineSelectionSize * 0.5f - 6, 3 + controlLineOffset), string.Format("{0:f2}", duration), centeredWhiteStyle);

            float delayLabelPos = 0;
            if (mode == Mode.openClode && direction == ItemKey.closed) delayLabelPos = position.width * 0.5f;
            float delayLabelSize = (controlLineSelectionSize > 0 ? controlLineSelectionPosition - delayLabelPos : controlLineSelectionPosition - delayLabelPos + controlLineSelectionSize) * 0.5f;
            Handles.Label(new Vector2(delayLabelPos + delayLabelSize - 6, 3 + controlLineOffset), string.Format("{0:f2}", delay), centeredGreyStyle);

            EditorGUI.DrawRect(new Rect(controlLineSelectionPosition, controlLineHeight - (controlLineHeight - controlLineOffset), controlLineSelectionSize, controlLineHeight - controlLineOffset), new Color(0.45f, 0.66f, 1f, 0.2f));
            Handles.color = new Color(1, 1, 1, 0.25f);
            Handles.DrawLine(new Vector2(controlLineSelectionPosition + controlLineSelectionSize, controlLineOffset), new Vector2(controlLineSelectionPosition + controlLineSelectionSize, 1000));
            Handles.DrawLine(new Vector2(controlLineSelectionPosition, controlLineOffset), new Vector2(controlLineSelectionPosition, 1000));

            if (controlLineSelectionSize > 0)
            {
                EditorGUI.DrawRect(new Rect(0, controlLineHeight, controlLineSelectionPosition - 1, position.height), new Color(0, 0, 0, 0.15f));
                EditorGUI.DrawRect(new Rect(controlLineSelectionPosition + 1 + controlLineSelectionSize, controlLineHeight, position.width - controlLineSelectionPosition - 2, position.height), new Color(0, 0, 0, 0.15f));
            }
            else
            {
                EditorGUI.DrawRect(new Rect(0, controlLineHeight, controlLineSelectionPosition + controlLineSelectionSize - 1, position.height), new Color(0, 0, 0, 0.15f));
                EditorGUI.DrawRect(new Rect(controlLineSelectionPosition + 1, controlLineHeight, position.width - controlLineSelectionPosition - 1, position.height), new Color(0, 0, 0, 0.15f));
            }
        }

        private void DrawItemOnTimeLine(TSSItem item, Vector2 position)
        {
            position.y = controlLineHeight + itemCount * (itemHeight + itemLineSpacing);

            if (item.parent != null && ItemChildBefore(item.parent))
                position.x -= (ItemDelay(item.parent) - ItemDelay(item)) / GetPeriod() * timeLineRect.width;
            else
                position.x += ItemDelay(item) / GetPeriod() * timeLineRect.width;

            bool itemChained = (item.parent != null && item.parent.childChainMode);
            Rect itemRect = new Rect(position.x, position.y, timeLineRect.width * ItemDuration(item) / GetPeriod(), itemHeight);
            Rect itemTitleRect = new Rect(itemRect.x + (itemChained ? 24 : 12), itemRect.y + itemRect.height * 0.5f - 6, itemRect.width - 24, 24);


            if (selectedItem == item)
            {
                if (!itemChained)
                {
                    EditorGUIUtility.AddCursorRect(new Rect(itemRect.x - itemHandleSize, itemRect.y, itemHandleSize * 2, itemHeight), MouseCursor.ResizeHorizontal);
                }
                EditorGUIUtility.AddCursorRect(new Rect(itemRect.x + itemHandleSize, itemRect.y, itemRect.width - itemHandleSize * 2, itemHeight), MouseCursor.SlideArrow);

                EditorGUIUtility.AddCursorRect(new Rect(itemRect.x + itemRect.width - itemHandleSize, itemRect.y, itemHandleSize * 2, itemHeight), MouseCursor.ResizeHorizontal);
            } else
            {
                EditorGUIUtility.AddCursorRect(itemRect, MouseCursor.Link);
            }

            if (boolMousePressed && new Rect(itemRect.x - itemHandleSize, itemRect.y, itemRect.width + itemHandleSize * 2, itemRect.height).Contains(mousePosition))
            {
                selectedItem = item;
                selectedItemRect = itemRect;
                boolMousePressed = false;

                controlLineSelectionPosition = itemRect.x;
                controlLineSelectionSize = itemRect.width;

                Selection.SetActiveObjectWithContext(item.gameObject, item);
                Undo.RegisterCompleteObjectUndo(item, "[TSS TimeLine]");
            }

            lastBackColor = !lastBackColor;
            if (!lastBackColor) EditorGUI.DrawRect(new Rect(timeLineRect.x, itemRect.y - itemLineSpacing, timeLineRect.width, itemHeight + itemLineSpacing), new Color(0, 0, 0, 0.125f));

            float itemEffectsValue = 0;

            if (itemRect.x > controlLineHandlerPosition)
                itemEffectsValue = 0;
            else if (itemRect.x + itemRect.width < controlLineHandlerPosition)
                itemEffectsValue = 1;
            else
                itemEffectsValue = (controlLineHandlerPosition - itemRect.x) / itemRect.width;

            if (direction == ItemKey.closed) itemEffectsValue = 1 - itemEffectsValue;

            
            if ((controlLineHandlerSelected || timeLinePlaying) && item.tweens.Count > 0)
            {
                item.state = ItemState.slave;

                if (mode == Mode.openClode)
                {
                    if (direction == ItemKey.closed && controlLineHandlerPosition > timeLineRect.width) TSSItemBase.Evaluate(item, Mathf.Clamp01(itemEffectsValue), ItemKey.closed);
                    if (direction == ItemKey.opened && controlLineHandlerPosition < timeLineRect.width) TSSItemBase.Evaluate(item, Mathf.Clamp01(itemEffectsValue), ItemKey.opened);
                }
                else
                    TSSItemBase.Evaluate(item, Mathf.Clamp01(itemEffectsValue), direction); 
            } 
            

            GUI.Box(itemRect, string.Empty, item == selectedItem ? itemSelectedStyle : (itemChained ? itemChainStyle : itemStyle) );
            GUI.Label(itemTitleRect, TSSText.GetHumanReadableString(item.name), itemTitleStyle);

            if (itemChained)
            {
                Rect chainIconRect = new Rect(itemRect.x + 7, itemRect.y + itemRect.height * 0.5f - 7, 14, 14);
                GUI.DrawTexture(chainIconRect, TSSEditorTextures.itemChainIcon, ScaleMode.StretchToFill);
            }

            if (item == selectedItem)
            {
                if (!itemChained)
                {
                    Handles.color = new Color(1, 1, 1, 0.5f);
                    Handles.color = Color.white;
                    Handles.DrawLine(new Vector2 (itemRect.x, itemRect.y), new Vector2(itemRect.x, itemRect.y + itemRect.height));
                }

                Handles.color = Color.white;
                Handles.DrawLine( new Vector2(itemRect.x + itemRect.width, itemRect.y), new Vector2(itemRect.x + itemRect.width, itemRect.y + itemRect.height));
            }

            if (item.parent == selectedItem && selectedItem != null)
            {
                EditorGUI.DrawRect(itemRect, new Color(0.45f, 0.66f, 1f, 0.15f));
            }
            else if (selectedItem != null)
            {
                EditorGUI.DrawRect(itemRect, new Color(0, 0, 0, 0.15f));
            }

            itemCount++;

            for (int i = 0; i < item.childItems.Count; i++) DrawItemOnTimeLine(item.childItems[i], position);            
        }

        private void DrawGrid(float gridSpacing, Rect gridRect, float gridOpacity, Color gridColor)
        {
            int linesCount = Mathf.CeilToInt(gridRect.width / gridSpacing);
            Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

            for (int i = 0; i < linesCount; i++)
            {
                Handles.DrawLine(new Vector2(gridSpacing * i + gridRect.x, gridRect.y), new Vector2(gridSpacing * i + gridRect.x, gridRect.y + gridRect.height));
            }

            Handles.color = Color.white;
        }

        #endregion

        #region Input

        private void UpdateInput()
        {
            Event e = Event.current;

            if (e.button != 0) return;

            if (mode == Mode.openClode)
            {
                if (e.mousePosition.x < position.width * 0.5f) direction = ItemKey.opened;
                else direction = ItemKey.closed;
            }

            switch (e.type)
            {
                case EventType.MouseDown:  OnMouseDown(e); break;
                case EventType.MouseUp:    OnMouseUp(e); break;
                case EventType.MouseDrag:  OnMouseDrag(e); break;
            }
        }

        private void OnMouseDown(Event e)
        {
            boolMousePressed = true;
            mousePosition = e.mousePosition;

            if (mousePosition.y < controlLineHeight && mousePosition.y > controlLineOffset)
            {
                controlLineHandlerSelected = true;
                controlLineHandlerPosition = mousePosition.x;
            }

            if (mousePosition.y > controlLineHeight)
            {
                controlLineSelectionPosition = mousePosition.x;
                controlLineSelectionSize = 0;
            }
        }

        private void OnMouseUp(Event e)
        {

            boolMousePressed = false;
            mousePosition = e.mousePosition;

            if (selectedItem != null)
            {
                RoundItemDelay(selectedItem);
                RoundItemDuration(selectedItem);
                if (selectedItem.parentChainMode) RoundItemfirstChildDelay(selectedItem.parent);

                if (selectedItem.parentChainMode && e.shift)
                    for (int i = 0; i < selectedItem.parent.childItems.Count; i++) RoundItemDuration(selectedItem.parent.childItems[i]);

                controlLineSelectionPosition = selectedItemRect.x;
                controlLineSelectionSize = selectedItemRect.width;

            }

            if (controlLineHandlerSelected) controlLineHandlerSelected = false;
        }

        private void OnMouseDrag(Event e)
        {
            if (controlLineHandlerSelected)
            {
                controlLineHandlerPosition += e.delta.x;
                controlLineHandlerPosition = Mathf.Clamp(controlLineHandlerPosition, 0, position.width);
            }


            if (selectedItem == null && mousePosition.y > controlLineHeight)
                controlLineSelectionSize += e.delta.x;

            if (selectedItem != null && mousePosition.y > selectedItemRect.y && mousePosition.y < selectedItemRect.y + selectedItemRect.height)
            {
                if (mousePosition.x > selectedItemRect.x - itemHandleSize && mousePosition.x < selectedItemRect.x + itemHandleSize && !selectedItem.parentChainMode)
                {
                    AddItemDelay(selectedItem, e.delta.x * GetPeriod() / timeLineRect.width);
                    AddItemDuration(selectedItem,-e.delta.x * GetPeriod() / timeLineRect.width);
                }
                        
                else if (mousePosition.x > selectedItemRect.x + itemHandleSize && mousePosition.x < selectedItemRect.x + selectedItemRect.width - itemHandleSize)
                {
                    if (selectedItem.parentChainMode)
                    {
                        AddItemfirstChildDelay(selectedItem.parent, e.delta.x * GetPeriod() / timeLineRect.width);
                        Clamp0ItemfirstChildDelay(selectedItem.parent);
                    }
                    else
                        AddItemDelay(selectedItem, e.delta.x * GetPeriod() / timeLineRect.width);
                }
                else if (mousePosition.x > selectedItemRect.x + selectedItemRect.width - itemHandleSize && mousePosition.x < selectedItemRect.x + selectedItemRect.width + itemHandleSize)
                {
                    AddItemDuration(selectedItem, e.delta.x * GetPeriod() / timeLineRect.width);

                    if (selectedItem.parentChainMode && e.shift)
                        for (int i = 0; i < selectedItem.parent.childItems.Count; i++)
                            if (selectedItem.parent.childItems[i] != selectedItem) SetItemDuration(selectedItem.parent.childItems[i], ItemDuration(selectedItem));
                }

                Clamp0ItemDelay(selectedItem);
                Clamp0ItemDuration(selectedItem);

                controlLineSelectionPosition = selectedItemRect.x;
                controlLineSelectionSize = selectedItemRect.width;
            }
        }

        #endregion

        #region Items time values stuff

        private float RoundItemValue(float someValue)
        {
            return (float)Math.Round(someValue, 1, MidpointRounding.AwayFromZero);
        }

        private void RoundItemDelay(TSSItem item)
        {
            if (direction == ItemKey.opened)
                item.openDelay = RoundItemValue(item.openDelay);
            else
                item.closeDelay = RoundItemValue(item.closeDelay);

            if (item.parent != null) item.parent.UpdateItemDelaysInChain((int)direction);
        }

        private void RoundItemDuration(TSSItem item)
        {
            if (direction == ItemKey.opened)
                item.openDuration = RoundItemValue(item.openDuration);
            else
                item.closeDuration = RoundItemValue(item.closeDuration);
        }

        private void RoundItemfirstChildDelay(TSSItem item)
        {
            if (direction == ItemKey.opened)
                item.firstChildOpenDelay = RoundItemValue(item.firstChildOpenDelay);
            else
                item.firstChildCloseDelay = RoundItemValue(item.firstChildCloseDelay);

            if (item.parent != null) item.parent.UpdateItemDelaysInChain((int)direction);
        }

        private void Clamp0ItemDelay(TSSItem item)
        {
            if (direction == ItemKey.opened)
            { if (item.openDelay < 0) item.openDelay = 0; }
            else
            { if (item.closeDelay < 0) item.closeDelay = 0; }
        }

        private void Clamp0ItemDuration(TSSItem item)
        {
            if (direction == ItemKey.opened)
            { if (item.openDuration < timeLinePrecision) item.openDuration = timeLinePrecision; }
            else
            { if (item.closeDuration < timeLinePrecision) item.closeDuration = timeLinePrecision; }
        }

        private void Clamp0ItemfirstChildDelay(TSSItem item)
        {
            if (direction == ItemKey.opened)
            { if (item.firstChildOpenDelay < 0) item.firstChildOpenDelay = 0; }
            else
            { if (item.firstChildCloseDelay < 0) item.firstChildCloseDelay = 0; }
        }

        private void AddItemDelay(TSSItem item, float addition)
        {
            if (direction == ItemKey.opened)
                item.openDelay += addition;
            else
                item.closeDelay += addition;
        }

        private void AddItemfirstChildDelay(TSSItem item, float addition)
        {
            if (direction == ItemKey.opened)
                item.firstChildOpenDelay += addition;
            else
                item.firstChildCloseDelay += addition;
        }

        private void AddItemDuration(TSSItem item, float addition)
        {
            if (direction == ItemKey.opened)
                item.openDuration += addition;
            else
                item.closeDuration += addition;
        }

        private float ItemDelay(TSSItem item)
        {
            if (item == null) return 0;
            return direction == ItemKey.opened ? item.openDelay : item.closeDelay;
        }

        private float ItemDuration(TSSItem item)
        {
            return direction == ItemKey.opened ? item.openDuration : item.closeDuration;
        }

        private void SetItemDuration(TSSItem item, float value)
        {
            if (direction == ItemKey.opened)
                item.openDuration = value;
            else
                item.closeDuration = value;
        }

        private bool ItemChildBefore(TSSItem item)
        {
            if (direction == ItemKey.opened)
                return item.openChildBefore;
            else
                return item.closeChildBefore;
        }

        private float GetItemBeforeOffset(TSSItem item)
        {
            if (item.parent == null) return 0;

            if (direction == ItemKey.opened)
            {
                if (!item.parent.openChildBefore) return 0;
                else return timeLineRect.width / GetPeriod() * (item.parent.openDelay - item.openDelay);
            }
            else
            {
                if (!item.parent.closeChildBefore) return 0;
                else return timeLineRect.width / GetPeriod() * (item.parent.closeDelay - item.closeDelay);
            }
        }

        private float GetTotalDuration()
        {
            return direction == ItemKey.opened ?
                GetItemTotalOpenDuration(item) :
                GetItemTotalCloseDuration(item);
        }

        private float GetTotalDelay(TSSItem item)
        {
            return direction == ItemKey.opened ?
                GetTotalOpenDelay(item) :
                GetTotalCloseDelay(item);
        }

        private float GetTotalOpenDelay(TSSItem item)
        {
            if (item == null) return 0;
            return item.openDelay + ((item.parent != null && item.parent.openChildBefore) ? 0 : GetTotalOpenDelay(item.parent));
        }

        private float GetTotalCloseDelay(TSSItem item)
        {
            if (item == null) return 0;
            return item.closeDelay + ((item.parent != null && item.parent.closeChildBefore) ? 0 : GetTotalCloseDelay(item.parent));
        }

        private float GetPeriod()
        {
            return direction == ItemKey.opened ?
                timeLineOpenPeriod :
                timeLineClosePeriod;
        }

        private void SetPeriods()
        {
            if (mode == Mode.close || mode == Mode.openClode) timeLineClosePeriod = GetItemTotalCloseDuration(item);
            if (mode == Mode.open || mode == Mode.openClode) timeLineOpenPeriod = GetItemTotalOpenDuration(item);
        }

        private static float GetItemTotalCloseDuration(TSSItem item)
        {
            float result = item.closeDelay + item.closeDuration;
            for (int i = 0; i < item.childItems.Count; i++)
            {
                float childTotalCloseDuration = GetItemTotalCloseDuration(item.childItems[i]);

                if (item.closeChildBefore)
                {
                    if (childTotalCloseDuration > result) result = childTotalCloseDuration;
                }
                else if (item.closeDelay + childTotalCloseDuration > result) result = item.closeDelay + childTotalCloseDuration;
            }

            return result;
        }

        private static float GetItemTotalOpenDuration(TSSItem item)
        {
            float result = item.openDelay + item.openDuration;
            for (int i = 0; i < item.childItems.Count; i++)
            {
                float childTotalOpenDuration = GetItemTotalOpenDuration(item.childItems[i]);

                if (item.openChildBefore)
                {
                    if (childTotalOpenDuration > result) result = childTotalOpenDuration;
                }
                else if (item.openDelay + childTotalOpenDuration > result) result = item.openDelay + childTotalOpenDuration;
            }

            return result;
        }

        #endregion
    }
}