// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru/tss
// MIT License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TSS.Base
{
    [DisallowMultipleComponent]
    public class TSSBehaviour : MonoBehaviour
    {
        #region Properties

        public static bool showBehaviour = false;

        private static TSSBehaviour _instance;
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
                    SceneManager.sceneUnloaded += Clear;
                    SceneManager.sceneLoaded += SceneLoaded;
                    if (Application.isPlaying) DontDestroyOnLoad(gameObject);
                }

                return _instance;
            }
        }

        private static bool _clearLists = false;
        public bool clearListsOnSceneLoad
        {
            set { _clearLists = value; }
            get { return _clearLists; }
        }

        private static List<TSSItem> updatingItems = new List<TSSItem>();
        private static List<TSSItem> fixedUpdatingItems = new List<TSSItem>();
        private static List<TSSItem> lateUpdateingItems = new List<TSSItem>();
        private static List<TSSCore> cores = new List<TSSCore>();

        #endregion

        #region Public methods

        private static void Clear(Scene scene)
        {
            if (!_clearLists) return;
            updatingItems.Clear();
            fixedUpdatingItems.Clear();
            lateUpdateingItems.Clear();
            cores.Clear();
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            for (int i = 0; i < TSSItemBase.AllItems.Count; i++)
            {
                if (TSSItemBase.AllItems[i].parent == null) TSSItemBase.AllItems[i].Refresh();
                TSSItemBase.Activate(TSSItemBase.AllItems[i], TSSItemBase.AllItems[i].activationStart);
            }

            for (int i = 0; i < cores.Count; i++)
            {
                cores[i].SelectDefaultState();
            }
        }

        public static void AddItem(TSSItem item)
        {
            if ((object)instance == null || item.behaviourCatched) return;

            switch (item.updatingType)
            {
                case ItemUpdateType.update: updatingItems.Add(item); break;
                case ItemUpdateType.fixedUpdate: fixedUpdatingItems.Add(item); break;
                case ItemUpdateType.lateUpdate: lateUpdateingItems.Add(item); break;
            }

            item.behaviourCatched = true;
        }

        public static void RemoveItem(TSSItem item)
        {
            switch (item.updatingType)
            {
                case ItemUpdateType.update: updatingItems.Remove(item); break;
                case ItemUpdateType.fixedUpdate: fixedUpdatingItems.Remove(item); break;
                case ItemUpdateType.lateUpdate: lateUpdateingItems.Remove(item); break;
            }

            item.behaviourCatched = false;
        }

        public static void AddCore(TSSCore core)
        {
            if ((object)instance == null) return;

            cores.Add(core);
            Debug.Log("Added core: " + core.gameObject.name);
        }

        public static void RemoveCore(TSSCore core)
        {
            if ((object)instance == null) return;

            cores.Remove(core);
        }

        #endregion

        #region Unity methods

        private void Awake()
        {
            if (_instance != null) DestroyImmediate(gameObject);
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
            item.deltaTime = deltaTime;

            if (item.path != null) item.path.UpdatePath();

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
                        if (!item.loopActivated) TSSBehaviour.RemoveItem(item);
                    }

                    for (int i = 0; i < item.tweens.Count; i++) item.tweens[i].Update();
                    break;

                case ItemState.closed:

                    if (!item.loopActivated || !Application.isPlaying) break;

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
        }

        #endregion
    }
}

