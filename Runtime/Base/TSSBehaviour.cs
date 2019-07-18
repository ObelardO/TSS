// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru/tss
// MIT License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TSS.Base
{
    [DisallowMultipleComponent]
    public partial class TSSBehaviour : MonoBehaviour
    {
        #region Properties

        /// <summary>Shows nehaviour object in editor. This property only works in editor.</summary>
        public static bool showBehaviour = false;

        private static TSSBehaviour _instance;

        /// <summary>Global behaviour pointer</summary>
        public static TSSBehaviour instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject gameObject = new GameObject()
                    {
                        name = "TSS Behaviour",
                        hideFlags = showBehaviour ? HideFlags.DontSave : HideFlags.HideAndDontSave
                    };

                    _instance = gameObject.AddComponent<TSSBehaviour>();
                }

                return _instance;
            }
        }

        private static List<TSSItem> AllItems = new List<TSSItem>();
        private static List<TSSItem> updatingItems = new List<TSSItem>();
        private static List<TSSItem> fixedUpdatingItems = new List<TSSItem>();
        private static List<TSSItem> lateUpdateingItems = new List<TSSItem>();
        private static List<TSSCore> cores = new List<TSSCore>();

        /// <summary>Readonly list of contained items</summary>
        public static List<TSSItem> GetItems()
        {
            return AllItems.ToList();
        }

        /// <summary>Count of contained items with default Update()</summary>
        public static int updatingItemsCount { get { return updatingItems.Count; } }

        /// <summary>Count of contained items with FixedUpdate()</summary>
        public static int fixedUpdatingItemsCount { get { return fixedUpdatingItems.Count; } }

        /// <summary>Count of contained items with LateUpdate()</summary>
        public static int lateUpdateingItemsCount { get { return lateUpdateingItems.Count; } }
        public static int coresCount { get { return cores.Count; } }

        #endregion

        #region Public methods

        /// <summary>Add item to behaviour on Awake. Strongly not recommended for manual use</summary>
        /// <param name="item">TSSitem</param>
        public static void OnItemAwake(TSSItem item)
        {
            AllItems.Add(item);
        }

        /// <summary>Remove item from behaviour on Destroy. Strongly not recommended for manual use</summary>
        /// <param name="item">TSSitem</param>
        public static void OnItemDestroy(TSSItem item)
        {
            RemoveItem(item);
            AllItems.Remove(item);
        }

        /// <summary>Add item to behaviour. Strongly not recommended for manual use</summary>
        /// <param name="item">TSSitem</param>
        public static void AddItem(TSSItem item)
        {
            if (item.behaviourCached) return;

            switch (item.updatingType)
            {
                case ItemUpdateType.update: updatingItems.Add(item); break;
                case ItemUpdateType.fixedUpdate: fixedUpdatingItems.Add(item); break;
                case ItemUpdateType.lateUpdate: lateUpdateingItems.Add(item); break;
            }

            item.behaviourCached = true;
        }

        /// <summary>Remove item from behaviour. Strongly not recommended for manual use</summary>
        /// <param name="item">TSSitem</param>
        public static void RemoveItem(TSSItem item)
        {
            if (!item.behaviourCached) return;

            switch (item.updatingType)
            {
                case ItemUpdateType.update: updatingItems.Remove(item); break;
                case ItemUpdateType.fixedUpdate: fixedUpdatingItems.Remove(item); break;
                case ItemUpdateType.lateUpdate: lateUpdateingItems.Remove(item); break;
            }

            item.behaviourCached = false;
        }

        /// <summary>Add core to behaviour. Strongly not recommended for manual use</summary>
        /// <param name="core">TSSCore</param>
        public static void AddCore(TSSCore core)
        {
            cores.Add(core);
        }

        /// <summary>Remove core from behaviour. Strongly not recommended for manual use</summary>
        /// <param name="core">TSSCore</param>
        public static void RemoveCore(TSSCore core)
        {
            cores.Remove(core);
        }

        /// <summary>Manualy refresh all items inheritances and activate start states. Strongly not recommended for manual use</summary>
        public static void RefreshAndStart()
        {
            for (int i = 0; i < AllItems.Count; i++)
            {
                if (AllItems[i].parent == null) AllItems[i].Refresh();
                TSSItemBase.Activate(AllItems[i], AllItems[i].activationStart);
            }

            for (int i = 0; i < cores.Count; i++) cores[i].SelectDefaultState();
        }

        #endregion

        #region Private methods

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnApplicationStart()
        {
            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) => RefreshAndStart();

            if (instance == null) return;
        }

        ///Strongly not recommended for manual use</summary>
        [System.Obsolete("Only for debug")]
        private static void Clear()
        {
            updatingItems.Clear();
            fixedUpdatingItems.Clear();
            lateUpdateingItems.Clear();
            cores.Clear();
        }

        #endregion

        #region Unity methods

        private void Awake()
        {
            if (_instance != null) Destroy(gameObject);
        }

        private void Update()
        {
            float time = Time.deltaTime;
            float unscaledTIme = Time.unscaledDeltaTime;

            for (int i = 0; i < updatingItems.Count; i++)
                UpdateItem(updatingItems[i], updatingItems[i].timeScaled ? time : unscaledTIme);

            for (int i = 0; i < cores.Count; i++) cores[i].UpdateCore();
        }

        private void FixedUpdate()
        {
            float time = Time.fixedDeltaTime;
            float unscaledTIme = Time.fixedUnscaledDeltaTime;

            for (int i = 0; i < fixedUpdatingItems.Count; i++)
                UpdateItem(fixedUpdatingItems[i], fixedUpdatingItems[i].timeScaled ? time : unscaledTIme);
        }

        private void LateUpdate()
        {
            float time = Time.deltaTime;
            float unscaledTIme = Time.unscaledDeltaTime;

            for (int i = 0; i < lateUpdateingItems.Count; i++)
                UpdateItem(lateUpdateingItems[i], lateUpdateingItems[i].timeScaled ? time : unscaledTIme);
        }

        #endregion

        #region Updating

        private void UpdateItem(TSSItem item, float deltaTime)
        {
            if (!item.enabled) return;

            item.deltaTime = deltaTime;

            switch (item.state)
            {
                case ItemState.opening:
                    if (item.stateChgTime >= 0)
                    {
                        item.stateChgTime -= deltaTime;
                        if (item.stateChgTime > 0) break;
                        for (int i = 0; i < item.tweens.Count; i++) item.tweens[i].blendTime = 0;
                        if (item.stateChgBranchMode && !item.openChildBefore) TSSItemBase.OpenChilds(item);
                    }
                    else if (item.time < 1)
                    {
                        item.time += deltaTime / item.openDuration;
                    }
                    else if (item.childStateCounts[(int)ItemState.opened] == item.childCountWithoutLoops)
                    {
                        item.time = 1;
                        item.state = ItemState.opened;
                    }

                    item.UpdateInput();

                    for (int i = 0; i < item.tweens.Count; i++) item.tweens[i].Update();
                    break;

                case ItemState.closing:
                    if (item.stateChgTime >= 0)
                    {
                        item.stateChgTime -= deltaTime;
                        if (item.stateChgTime > 0) break;
                        for (int i = 0; i < item.tweens.Count; i++) item.tweens[i].blendTime = 0;
                        if (item.stateChgBranchMode && !item.closeChildBefore) TSSItemBase.CloseChilds(item);
                    }
                    else if (item.time > 0)
                    {
                        item.time -= deltaTime / item.closeDuration;
                    }
                    else if (item.childStateCounts[(int)ItemState.closed] == item.childCountWithoutLoops)
                    {
                        item.time = 0;
                        item.state = ItemState.closed;
                        if (!item.loopActivated) RemoveItem(item);

                    }

                    for (int i = 0; i < item.tweens.Count; i++) item.tweens[i].Update();
                    break;

                case ItemState.closed:

                    if (!item.loopActivated)
                    {
                        RemoveItem(item);
                        if (!Application.isPlaying) break;
                    }

                    if (item.currentLoops > 0 || item.loops < 0)
                    {
                        TSSItemBase.Activate(item, item.activationOpen);
                        item.loopActivated = true;
                        item.stateChgTime = 0;
                        break;
                    }

                    item.loopActivated = false;

                    return;

                case ItemState.opened:

                    item.UpdateInput();

                    item.UpdateMedia();

                    if (item.loops == 0 || !Application.isPlaying) break;

                    if (!item.loopActivated) { item.loopActivated = true; item.currentLoops = item.loops; }

                    if (item.currentLoops > 0 || item.loops < 0)
                    {
                        float time = item.time;

                        TSSItemBase.Activate(item, item.loopMode);
                        item.loopActivated = true;
                        if (item.loops > 0) item.currentLoops--;
                        if (item.loopMode == ActivationMode.closeImmediately ||
                            item.loopMode == ActivationMode.closeBranchImmediately)
                        {
                            item.time = time - 1;
                            UpdateItem(item, deltaTime);
                        }
                    }

                    break;
            }

            item.UpdateButton(deltaTime);
            if (item.path != null) item.path.UpdatePath();
        }

        #endregion
    }
}