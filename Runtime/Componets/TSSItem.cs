// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru/tss
// MIT License

using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;
using TSS.Base;

namespace TSS
{
    [Serializable, DisallowMultipleComponent, AddComponentMenu("TSS/Item")]
#if UNITY_2018_3_OR_NEWER
    [ExecuteAlways]
#else
        [ExecuteInEditMode]
#endif
    public class TSSItem : MonoBehaviour
    {
        #region Properties

        /// <summary>Values container</summary>
        [HideInInspector] public TSSItemValues values;

        [NonSerialized] public int ID = 0;
        [NonSerialized] public float time = 0;
        [NonSerialized] public bool behaviourCached;

        /// <summary>Update, FixedUpdate or LateUpdate</summary>
        public ItemUpdateType updatingType { get { return values.updatingType; } set { if (!Application.isPlaying) values.updatingType = value; } }
        /// <summary>Use time scaling</summary>
        public bool timeScaled { get { return values.timeScaled; } set { values.timeScaled = value; } }

        /// <summary>Activation mode for starting (activated once at awake, default is CloseBranchImmediately)</summary>
        public ActivationMode activationStart { get { return values.startAction; } set { values.startAction = value; } }
        /// <summary>Activation mode for opening (activated at Open() calling, default is OpenBranch)</summary>
        public ActivationMode activationOpen { get { return values.activations[1]; } set { values.activations[1] = value; } }
        /// <summary>Activation mode for closing (activated at Close() calling, default is CloseBranch)</summary>
        public ActivationMode activationClose { get { return values.activations[0]; } set { values.activations[0] = value; } }

        /// <summary>Time in seconds which item waits before start opening</summary>
        public float openDelay { set { values.delays[1] = value; } get { return values.delays[1]; } }

        /// <summary>Time in seconds which item waits before start closing</summary>
        public float closeDelay { set { values.delays[0] = value; } get { return values.delays[0]; } }

        /// <summary>Time in seconds for which item opens</summary>
        public float openDuration { set { values.durations[1] = value; } get { return values.durations[1]; } }
        /// <summary>Time in seconds for which item closes</summary>
        public float closeDuration { set { values.durations[0] = value; } get { return values.durations[0]; } }

        /// <summary>Item's child open and close delays are controlled by this item</summary>
        public bool childChainMode { set { values.childChainMode = value; } get { return values.childChainMode; } }
        /// <summary>Item's child open and close delays are ignoring on a halfway</summary>
        public bool brakeChainDelay { set { values.brakeChainDelay = value; } get { return values.brakeChainDelay; } }
        /// <summary>Item's parent control this item's open and close delays/summary>
        public bool parentChainMode { get { if (parent == null) return false; else return parent.childChainMode; } }

        /// <summary>Item's child opens with own open delay without waiting this item open delay</summary>
        public bool openChildBefore { set { values.childBefore[1] = value; } get { return values.childBefore[1]; } }
        /// <summary>Item's child closes with own close delay without waiting this item close delay</summary>
        public bool closeChildBefore { set { values.childBefore[0] = value; } get { return values.childBefore[0]; } }

        /// <summary>Time in seconds which the next element in child chain wait before opening</summary>
        public float chainOpenDelay { set { values.chainDelays[1] = value; } get { return values.chainDelays[1]; } }
        /// <summary>Time in seconds which the next element in child chain wait before closing</summary>
        public float chainCloseDelay { set { values.chainDelays[0] = value; } get { return values.chainDelays[0]; } }

        /// <summary>Time in seconds which item's child chain waits before start opening</summary>
        public float firstChildOpenDelay { set { values.firstChildDelay[1] = value; } get { return values.firstChildDelay[1]; } }
        /// <summary>Time in seconds which item's child chain waits before start closing</summary>
        public float firstChildCloseDelay { set { values.firstChildDelay[0] = value; } get { return values.firstChildDelay[0]; } }

        /// <summary>Order of opening child items (auto control child open delays)</summary>
        public ChainDirection chainOpenDirection { set { values.chainDirections[1] = value; this.UpdateItemDelaysInChain((int)ItemKey.opened); } get { return values.chainDirections[1]; } }
        /// <summary>Order of closing child items (auto control child close delays)</summary>
        public ChainDirection chainCloseDirection { set { values.chainDirections[0] = value; this.UpdateItemDelaysInChain((int)ItemKey.closed); } get { return values.chainDirections[0]; } }

        /// <summary>Interpolating rotation mode</summary>
        public RotationMode rotationMode { set { values.rotationMode = value; } get { return values.rotationMode; } }
        /// <summary>Direct or Instance</summary>
        public MaterialMode materialMode { set { values.materialMode = value; RefreshMaterial(); } get { return values.materialMode; } }

        /// <summary>Which axis controlled by path rotation</summary>
        public Vector3 rotationMask { set { values.rotationMask = value; } get { return values.rotationMask; } }
        public bool rotationMaskX { set { values.rotationMask.x = value ? 1 : 0; } get { return values.rotationMask.x == 1; } }
        public bool rotationMaskY { set { values.rotationMask.y = value ? 1 : 0; } get { return values.rotationMask.y == 1; } }
        public bool rotationMaskZ { set { values.rotationMask.z = value ? 1 : 0; } get { return values.rotationMask.z == 1; } }

        /// <summary>Path alignment vector (for eliminate sharp turns, Up for 3D and Forward for 2D usually)</summary>
        public PathNormal pathNormal { set { values.pathNormal = value; } get { return values.pathNormal; } }

        /// <summary>Control interactable components</summary>
        public bool interactions { set { values.interactions = value; } get { return values.interactions; } }
        /// <summary>Control components with raycast target</summary>
        public bool blockRaycasting { set { values.blockRaycasting = value; } get { return values.blockRaycasting; } }

        /// <summary>Control AudioSource component</summary>
        public bool soundControl { set { values.soundControl = value; } get { return values.soundControl; } }
        /// <summary>Restart AudioSource playing at opening</summary>
        public bool soundRestart { set { values.soundRestart = value; } get { return values.soundRestart; } }

        /// <summary>Control VideoPlauer component</summary>
        public bool videoControl { set { values.videoControl = value; } get { return values.videoControl; } }
        /// <summary>Restart VideoPlauer playing at opening</summary>
        public bool videoRestart { set { values.videoRestart = value; } get { return values.videoRestart; } }

        /// <summary>Length of random symbols in text interpolation</summary>
        public int randomWave { set { values.randomWaveLength = value; } get { return values.randomWaveLength; } }
        /// <summary>Format for number to text converting</summary>
        public string floatFormat { set { values.floatFormat = value; } get { return values.floatFormat; } }

        /// <summary>Don't consider child in inheriting</summary>
        public bool ignoreChilds { set { values.ignoreChilds = value; Refresh(); } get { return values.ignoreChilds; } }
        /// <summary>Don't consider parent in inheriting</summary>
        public bool ignoreParent { set { values.ignoreParent = value; if (parent) parent.Refresh(); } get { return values.ignoreParent; } }

        /// <summary>Time in seconds which button animation is playing</summary>
        public float buttonDuration { set { values.buttonDuration = value; } get { return values.buttonDuration; } }
        private float _buttonEvaluation;
        /// <summary>Button animation evaluation</summary>
        public float buttonEvaluation { set { _buttonEvaluation = value; for (int i = 0; i < childItems.Count; i++) childItems[i].buttonEvaluation = value; } get { return _buttonEvaluation; } }
        /// <summary>what item state to use as the pressed button state</summary>
        public ButtonDirection buttonDirection { set { values.buttonDirection = value; } get { return values.buttonDirection; } }

        /// <summary>Item evaluation</summary>
        [NonSerialized] public float evaluation;
        /// <summary>Item delteTime affected by updating type and time scaling</summary>
        [NonSerialized] public float deltaTime;

        /// <summary>Count of item loops. Loop activation will start after every opening (-1 as infinity loop)</summary>
        public int loops { set { values.loops = value; Refresh(); } get { return values.loops; } }
        /// <summary>Loop activation mode what start after opening</summary>
        public ActivationMode loopMode { set { values.loopMode = value; Refresh(); } get { return values.loopMode; } }
        /// <summary>Current item activation inwoked by loop</summary>
        [HideInInspector] public bool loopActivated;
        /// <summary>Number of loops remaining</summary>
        [HideInInspector] public int currentLoops;
        /// <summary>Count of child items without any loops</summary>
        [HideInInspector] public int childCountWithoutLoops;

        /// <summary>Event invoked after item completely closed</summary>
        [HideInInspector] public UnityEvent OnClosed;
        /// <summary>Event invoked at item start opeing</summary>
        [HideInInspector] public UnityEvent OnOpening;
        /// <summary>Event invoked after item completely opened</summary>
        [HideInInspector] public UnityEvent OnOpened;
        /// <summary>Event invoked at item start closing</summary>
        [HideInInspector] public UnityEvent OnClosing;

        /// <summary>Item current state</summary>
        [NonSerialized] public ItemState _state;
        public ItemState state
        {
            set { if (_state != value) UpdateState(value); }
            get { return _state; }
        }

        /// <summary>Item is completely opened</summary>
        public bool isOpened { get { return _state == ItemState.opened; } }
        /// <summary>Item is completely closed</summary>
        public bool IsClosed { get { return _state == ItemState.closed; } }
        /// <summary>Item is opening</summary>
        public bool isOpening { get { return _state == ItemState.opening; } }
        /// <summary>Item is closing</summary>
        public bool isClosing { get { return _state == ItemState.closing; } }
        /// <summary>Item's evaluation is controlled from external script</summary>
        public bool isSlave { get { return _state == ItemState.slave; } }

        /// <summary>Item's child states</summary>
        [NonSerialized] public int[] childStateCounts = new int[4];
        [NonSerialized] public float stateChgTime;
        [NonSerialized] public bool stateChgBranchMode;

        /// <summary>List of child</summary>
        [NonSerialized] public List<TSSItem> childItems = new List<TSSItem>();
        /// <summary>List of attached tweens</summary>
        [HideInInspector] public List<TSSTween> tweens = new List<TSSTween>();
        /// <summary>parent item</summary>
        [NonSerialized] public TSSItem parent;

        [SerializeField] private TSSProfile _profile;
        /// <summary>Attached profile</summary>
        public TSSProfile profile { set { _profile = value; } get { return _profile; } }

        [HideInInspector] public CanvasGroup canvasGroup;
        [HideInInspector] public Image image;
        [HideInInspector] public RawImage rawImage;
        [HideInInspector] public Text text;
        [HideInInspector] public TSSGradient gradient;
        [HideInInspector] public RectTransform rect;
        [HideInInspector] public Button button;
        [HideInInspector] public Collider colider;
        [HideInInspector] public AudioSource audioPlayer;
        [HideInInspector] public VideoPlayer videoPlayer;
        [HideInInspector] public Material material;
        [HideInInspector] public SphereCollider sphereCollider;
        [HideInInspector] public Renderer itemRenderer;
        [HideInInspector] public Light itemLight;
        [HideInInspector] public TSSPath path;
        [HideInInspector] public string stringPart;

        #endregion

        #region Runtime activation methods

        /// <summary>Open item with open activation mode (OpenBranch as default)</summary>
        public void Open() { TSSItemBase.Activate(this, activationOpen); }

        /// <summary>Close item with close activation mode (CloseBranch as default)</summary>
        public void Close() { TSSItemBase.Activate(this, activationClose); }

        /// <summary>Open (if closed) or Close (if opened) item with close and open activation modes (OpenBranch and CloseBranch as defaults)</summary>
        public void OpenClose() { if (state == ItemState.closing || state == ItemState.closed) Open(); else Close(); }

        /// <summary>Activate item with specified activation mode</summary>
        /// <param name="mode">activation mode</param>
        public void Activate(ActivationMode mode) { TSSItemBase.Activate(this, mode); }

        /// <summary>Controll item's branch (item with child and child of child of ... etc.) manualy</summary>
        /// <param name="value">time between 0 and 1</param>
        public void EvaluateBranch(float value) { TSSItemBase.EvaluateBranch(this, value); }

        /// <summary>Controll item (only one) manualy</summary>
        /// <param name="value">time between 0 and 1</param>
        public void Evaluate(float value) { TSSItemBase.Evaluate(this, value); }

        /// <summary>Controll item's branch (item with child and child of child of ... etc.) manualy</summary>
        /// <param name="value">time between 0 and 1</param>
        /// <param name="direction">use tween with specified direction</param>
        public void EvaluateBranch(float value, ItemKey direction) { TSSItemBase.EvaluateBranch(this, value, direction); }

        /// <summary>Controll item (only one) manualy</summary>
        /// <param name="value">time between 0 and 1</param>
        /// <param name="direction">use tween with specified direction</param>
        public void Evaluate(float value, ItemKey direction) { TSSItemBase.Evaluate(this, value, direction); }

        /// <summary>Open only this item without activation mode</summary>
        public void OpenSinge() { TSSItemBase.Activate(this, ActivationMode.open); }

        /// <summary>Close only this item without activation mode</summary>
        public void CloseSingle() { TSSItemBase.Activate(this, ActivationMode.close); }

        /// <summary>Open (if closed) or close (if opened) only this item without activation modes</summary>
        public void OpenCloseSingle() { TSSItemBase.Activate(this, ActivationMode.openClose); }

        /// <summary>Open this item's branch without activation mode</summary>
        public void OpenBranch() { TSSItemBase.Activate(this, ActivationMode.openBranch); }

        /// <summary>Close this item branch without activation mode</summary>
        public void CloseBranch() { TSSItemBase.Activate(this, ActivationMode.closeBranch); }

        /// <summary>Open (if closed) or close (if opened) this item's branch without activation modes</summary>
        public void OpenCloseBranch() { TSSItemBase.Activate(this, ActivationMode.openCloseBranch); }

        /// <summary>Open only this item immediately without activation mode</summary>
        public void OpenImmediately() { TSSItemBase.Activate(this, ActivationMode.openImmediately); }

        /// <summary>Close only this item immediately without activation mode</summary>
        public void CloseImmediately() { TSSItemBase.Activate(this, ActivationMode.closeImmediately); }

        /// <summary>Open (if closed) or close (if opened) this item immediately without activation modes</summary>
        public void OpenCloseImmediately() { TSSItemBase.Activate(this, ActivationMode.openCloseImmediately); }

        /// <summary>Open this item's branch immediately without activation mode</summary>
        public void OpenBranchImmediately() { TSSItemBase.Activate(this, ActivationMode.openBranchImmediately); }

        /// <summary>Close this item's branch immediately without activation mode</summary>
        public void CloseBranchImmediately() { TSSItemBase.Activate(this, ActivationMode.closeBranchImmediately); }

        /// <summary>Open (if closed) or close (if opened) this item's branch immediately without activation modes</summary>
        public void OpenCloseBranchImmediately() { TSSItemBase.Activate(this, ActivationMode.openCloseBranchImmediately); }

        #endregion

        #region Refreshing

        /// <summary>Refresh items inheritance and components (automatically only at editor, called once on awake at runtime)</summary>
        public void Refresh()
        {
            if (gameObject == null) return;

            TSSItem[] childs = GetComponentsInChildren<TSSItem>();

            childItems.Clear();
            childCountWithoutLoops = 0;

            for (int i = 0; i < childs.Length; i++)
            {
                if (childs[i] == this || childs[i].ignoreParent || TSSItemBase.GetItemParentTransform(childs[i]) != transform || !childs[i].enabled) continue;

                if (ignoreChilds)
                {
                    childs[i].ID = 1;
                    childs[i].parent = null;
                }
                else
                {
                    childItems.Add(childs[i]);
                    childs[i].ID = childItems.Count;
                    childs[i].parent = this;
                    if (childs[i].loops == 0) childCountWithoutLoops++;
                }

                childs[i].Refresh();
            }

            this.UpdateItemDelaysInChain((int)ItemKey.closed);
            this.UpdateItemDelaysInChain((int)ItemKey.opened);

            canvasGroup = GetComponent<CanvasGroup>();
            image = GetComponent<Image>();
            rawImage = GetComponent<RawImage>();
            text = GetComponent<Text>();
            gradient = GetComponent<TSSGradient>();
            rect = GetComponent<RectTransform>();
            button = GetComponent<Button>();
            colider = GetComponent<Collider>();
            audioPlayer = GetComponent<AudioSource>();
            videoPlayer = GetComponent<VideoPlayer>();
            sphereCollider = GetComponent<SphereCollider>();
            itemLight = GetComponent<Light>();
            itemRenderer = GetComponent<Renderer>();
            path = GetComponent<TSSPath>();

            if (button != null) button.onClick.AddListener(OnClick);

            RefreshMaterial();
            if (path != null) path.Refresh();
        }

        private void RefreshMaterial()
        {
            material = null;

            if (itemRenderer != null)
            {
                if (materialMode == MaterialMode.direct) material = itemRenderer.sharedMaterial;
                else if (Application.isPlaying) material = itemRenderer.material;
                else material = itemRenderer.sharedMaterial;
            }
            else if (image != null && materialMode == MaterialMode.direct)
            {
                material = image.material == null ? image.defaultMaterial : image.material;
            }
            else if (rawImage != null && materialMode == MaterialMode.direct)
            {
                material = rawImage.material == null ? rawImage.defaultMaterial : rawImage.material;
            }
        }

        #endregion

        #region Unity methods

        private void Awake()
        {
            TSSBehaviour.OnItemAwake(this);
            TSSItemBase.InitValues(ref values);
            TSSItemBase.DoAllEffects(this, 0);
            UpdateState(ItemState.closed);
        }

        private void OnDestroy()
        {
            TSSBehaviour.OnItemDestroy(this);
        }

        private void Reset()
        {
            values = new TSSItemValues();
            TSSItemBase.InitValues(ref values);
            TSSItemBase.DoAllEffects(this, 0);
            UpdateState(ItemState.closed);
        }

        private void OnDisable()
        {
            if (!ignoreParent && parent && parent.childItems.Contains(this))
            {
                parent.childItems.Remove(this);
                if (values.loops != 0) parent.childCountWithoutLoops--;
                parent.childStateCounts[(int)_state]--;
            }
        }

        private void OnEnable()
        {
            if (!ignoreParent && parent && !parent.ignoreChilds)
            {
                parent.childItems.Add(this);
                if (values.loops != 0) parent.childCountWithoutLoops++;
                parent.childStateCounts[(int)_state]++;
            }
        }

        #endregion

        #region Update Methods

        /// <summary>Switching to new state</summary>
        private void UpdateState(ItemState newState)
        {
            bool enable = false;

            if (newState == ItemState.opening || newState == ItemState.opened)
            {
                enable = true;
            }
            else if (newState != ItemState.opened)
            {
                enable = false;
            }

            if (interactions)
            {
                if (colider != null) colider.enabled = enable;
                if (button != null) button.interactable = enable;
                if (canvasGroup != null) canvasGroup.interactable = enable;
            }

            if (blockRaycasting)
            {
                if (rawImage != null) rawImage.raycastTarget = enable;
                if (image != null) image.raycastTarget = enable;
                if (canvasGroup != null) canvasGroup.blocksRaycasts = enable;
                if (text != null) text.raycastTarget = enable;
            }

            if (soundControl && audioPlayer != null)
            {
                if (enable && !audioPlayer.isPlaying) audioPlayer.Play();
                else if (newState == ItemState.closed) { if (soundRestart) audioPlayer.Stop(); else audioPlayer.Pause(); }
            }

            if (videoControl && videoPlayer != null)
            {
                if (enable && !videoPlayer.isPlaying) videoPlayer.Play();
                else if (newState == ItemState.closed) { if (videoRestart) videoPlayer.Stop(); else videoPlayer.Pause(); }
            }

            if (loops == 0 && !ignoreParent && parent != null && !parent.ignoreChilds && _state != ItemState.slave && parent.childStateCounts[(int)_state] > 0) parent.childStateCounts[(int)_state] -= 1;

            _state = newState;

            switch (_state)
            {
                case ItemState.closed: if (OnClosed != null && OnClosed.GetPersistentEventCount() > 0) OnClosed.Invoke(); break;
                case ItemState.opening: if (OnOpening != null && OnOpening.GetPersistentEventCount() > 0) OnOpening.Invoke(); break;
                case ItemState.opened: if (OnOpened != null && OnOpened.GetPersistentEventCount() > 0) OnOpened.Invoke(); break;
                case ItemState.closing: if (OnClosing != null && OnClosing.GetPersistentEventCount() > 0) OnClosing.Invoke(); break;
            }

            if (loops == 0 && !ignoreParent && parent != null && !parent.ignoreChilds && _state != ItemState.slave) parent.childStateCounts[(int)_state] += 1;
        }

        private void OnClick()
        {
            if (buttonEvaluation <= 0) buttonEvaluation = buttonDuration;
            for (int i = 0; i < childItems.Count; i++) if (childItems[i].buttonEvaluation <= 0) childItems[i].buttonEvaluation = childItems[i].buttonDuration;
        }

        public void UpdateMedia()
        {
            if (videoControl && videoPlayer != null && !videoPlayer.isLooping && videoPlayer.isPlaying
                && (((videoPlayer.frameCount / videoPlayer.frameRate) - (videoPlayer.frame / videoPlayer.frameRate)) * videoPlayer.playbackSpeed) <= closeDuration + closeDelay) Close();

            if (videoPlayer == null && soundControl && audioPlayer != null && !audioPlayer.loop && audioPlayer.isPlaying
                && (audioPlayer.clip.length - audioPlayer.time) <= closeDuration + closeDelay) Close();
        }

        public void UpdateInput()
        {
            if (button == null || !button.interactable || !Input.anyKeyDown) return;
            for (int i = 0; i < values.onKeyboard.Count; i++) if (Input.GetKeyDown(values.onKeyboard[i])) button.onClick.Invoke();
        }

        public void UpdateButton(float deltaTime)
        {
            if (buttonEvaluation > 0)
            {
                buttonEvaluation -= deltaTime;
                if (buttonEvaluation < 0) buttonEvaluation = 0;

                for (int i = 0; i < tweens.Count; i++)
                {
                    if (!tweens[i].enabled || tweens[i].direction != TweenDirection.Button) continue;
                    TSSItemBase.DoEffect(this, tweens[i].Evaluate(buttonDirection == ButtonDirection.open2Close ?
                        buttonEvaluation / buttonDuration :
                        1 - (buttonEvaluation / buttonDuration), tweens[i].type), tweens[i].effect);
                }
            }
        }

        #endregion
    }
}