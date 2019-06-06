// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru/tss
// MIT License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using TSS.Base;

namespace TSS
{
    #region Enumerations

    public enum IncorrectStateAction { openDefault, none, closeAll }

    #endregion

    [System.Serializable, AddComponentMenu("TSS/Core")]
    public class TSSCore : MonoBehaviour
    {
        #region Properties

        /// <summary>States list</summary>
        public TSSState this[int stateID] { get { return stateID >= 0 && stateID < Count ? states[stateID] : null; } }

        /// <summary>Count of states list</summary>
        public int Count { get { return states.Count; } }

        /// <summary>Count of enabled states in states list</summary>
        public int CountEnabled { get { return states.Where(s => s.enabled).Count(); } }

        /// <summary>Last added state</summary>
        public TSSState Last { get { return this[Count - 1]; } }

        /// <summary>Last enabled state</summary>
        public TSSState LastEnabled { get { return states.Where(s => s.enabled).LastOrDefault(); } }

        /// <summary>Next after currentState enabled state. Return first enabled if currentState is null</summary>
        public TSSState NextEnabled { get {
                if (currentState == null) return states.Where(s => s.enabled).FirstOrDefault();
                return states.Where(s => s.enabled && GetStateID(s) > GetStateID(currentState)).FirstOrDefault();
            }
        }

        /// <summary>Next after currentState state. Return first if currentState is null</summary>
        public TSSState Next { get {
                if (currentState == null) return states.FirstOrDefault();
                return states.Where(s => GetStateID(s) > GetStateID(currentState)).FirstOrDefault();
            }
        }

        /// <summary>Previous before currentState enabled state. Return last enabled if currentState is null</summary>
        public TSSState PreviousEnabled { get {
                if (currentState == null) return states.Where(s => s.enabled).LastOrDefault();
                return states.Where(s => s.enabled && GetStateID(s) < GetStateID(currentState)).LastOrDefault();
            }
        }

        /// <summary>Previous before currentState state. Return last if currentState is null</summary>
        public TSSState Previous { get {
                if (currentState == null) return states.LastOrDefault();
                return states.Where(s => GetStateID(s) < GetStateID(currentState)).LastOrDefault();
            }
        }

        /// <summary>first added state</summary>
        public TSSState First { get { return this[0]; } }

        /// <summary>First enabled state</summary>
        public TSSState FirstEnabled { get { return states.Where(s => s.enabled).FirstOrDefault(); } }

        /// <summary>List of core states</summary>
        [SerializeField] private List<TSSState> states = new List<TSSState>();

        /// <summary>Current opened state</summary>
        public TSSState currentState;

        /// <summary>Core state marked as default</summary>
        public TSSState defaultState
        {
            get { return states.Where(s => s.isDefault == true).FirstOrDefault(); }
        }

        /// <summary>What happens if there are no any state with specified name</summary>
        public IncorrectStateAction incorrectAction;

        /// <summary>If toggled, events will Invoke by state selecting</summary>
        public bool useEvents = false;

        /// <summary>When toggled, input will be proccesed</summary>
        public bool useInput = true;

        /// <summary>Event will Invoke when any state has been selected</summary>
        public TSSCoreStateSelectedEvent OnStateSelected = new TSSCoreStateSelectedEvent();

        /// <summary>Event will Invoke when current state has been closed</summary>
        public TSSCoreStateSelectedEvent OnCurrentStatedClosed = new TSSCoreStateSelectedEvent();

        /// <summary>Event will Invoke when first has been selected</summary>
        public TSSCoreStateSelectedEvent OnFirstStateSelected = new TSSCoreStateSelectedEvent();

        /// <summary>Event will Invoke when last state has been selected</summary>
        public TSSCoreStateSelectedEvent OnLastStateSelected = new TSSCoreStateSelectedEvent();

        /// <summary>Event will Invoke when trying to open an unexisting state</summary>
        public UnityEvent OnIncorrectStateSelected = new UnityEvent();

        #endregion

        #region Unity methods

        private void OnEnable()
        {
            TSSBehaviour.AddCore(this);
        }

        private void OnDisable()
        {
            TSSBehaviour.RemoveCore(this);
        }

        public void UpdateCore()
        {
            if (!useInput || !Input.anyKeyDown) return;

            for (int stateID = 0; stateID < states.Count; stateID++)
                if (!this[stateID].enabled) continue; else this[stateID].UpdateInput();
        }

        #endregion

        #region States

        /// <summary>Set specified state as default core state</summary>
        /// <param name="state">state pointer</param>
        public void SetDefaultState(TSSState state)
        {
            states.ForEach(s => s.isDefault = false);
            if (state == null) return;
            state.isDefault = true;
        }

        /// <summary>Set specified state as default core state</summary>
        /// <param name="stateName">state identifier</param>
        public void SetDefaultState(string stateName)
        {
            SetDefaultState(states.Where(s => s.name.ToLower() == stateName.ToLower() && s.enabled).FirstOrDefault());
        }

        /// <summary>Release default state</summary>
        public void SetDefaultState()
        {
            states.ForEach(s => s.isDefault = false);
        }

        /// <summary>Open default state (if there is one)</summary>
        public void SelectDefaultState()
        {
           
            if (defaultState != null) SelectState(defaultState.name);
            
        }

        /// <summary>Open specified state</summary>
        /// <param name="state">state pointer</param>
        public void SelectState(TSSState state)
        {
            if (state == null || !state.enabled)
            {
                switch (incorrectAction)
                {
                    case IncorrectStateAction.openDefault: SelectDefaultState(); break;
                    case IncorrectStateAction.closeAll: states.ForEach(s => s.Close()); break;
                }

                currentState = null;

                if (useEvents) OnIncorrectStateSelected.Invoke();

                return;
            }

            currentState = state;

            states.Where(s => s.name.ToLower() != state.name.ToLower()).ToList().ForEach(s => s.Close());
            currentState.Open();

            if (!useEvents) return;

            OnStateSelected.Invoke(currentState);
            if (currentState == FirstEnabled || currentState == First) OnFirstStateSelected.Invoke(currentState);
            if (currentState == LastEnabled || currentState == Last) OnLastStateSelected.Invoke(currentState);
        }

        /// <summary>Open specified state</summary>
        /// <param name="stateName">state identifier</param>
        public void SelectState(string stateName)
        {
            SelectState(states.Where(s => s.name.ToLower() == stateName.ToLower() && s.enabled).FirstOrDefault());
        }


        /// <summary>Open next after current state</summary>
        public void SelectNextState()
        {
            if (currentState == LastEnabled || Count <= 1) return;
            SelectState(Next);
        }

        /// <summary>Open next enabled after current state</summary>
        public void SelectNextEnabledState()
        {
            if (currentState == LastEnabled || CountEnabled <= 1) return;
            SelectState(NextEnabled);
        }

        /// <summary>Open previous before current state</summary>
        public void SelectPreviousState()
        {
            if (currentState == First || Count <= 1) return;
            SelectState(Previous);
        }

        /// <summary>Open previous enabled before current state</summary>
        public void SelectPreviousEnabledState()
        {
            if (currentState == FirstEnabled || CountEnabled <= 1) return;
            SelectState(PreviousEnabled);
        }

        /// <summary>Close all states</summary>
        /// <param name="stateName">state identifier</param>
        public void CloseAll()
        {
            states.ForEach(s => s.Close());
            if (currentState != null && useEvents) OnCurrentStatedClosed.Invoke(currentState);
            currentState = null;
        }

        /// <summary>Close specified state</summary>
        /// <param name="state">state pointer</param>
        public void Close(TSSState state)
        {
            state.Close();
            if (state != currentState) return;
            currentState = null;
            if (useEvents) OnCurrentStatedClosed.Invoke(state);
        }

        /// <summary>Add a new state to this core</summary>
        /// <returns>State pointer</returns>
        /// <param name="item">first item pointer in group (maybe a null)</param>
        /// <param name="stateName">state identifier</param>
        public TSSState AddState(TSSItem item, string stateName)
        {
            AddState(item).name = stateName;
            return states.Last();
        }

        /// <summary>
        /// Add a new state to this <b>core</b> (with default identifier)
        /// </summary>
        /// <returns>State pointer</returns>
        /// <param name="item">first item pointer in group (maybe a null)</param>
        public TSSState AddState(TSSItem item)
        {
            states.Add(new TSSState(this, item));
            return states.Last();
        }

        /// <summary>
        /// Get a specified state (first with coincidental identifier)
        /// </summary>
        /// <returns>State pointer</returns>
        /// <param name="stateName">state identifier</param>
        public TSSState GetState(string stateName)
        {
            return states.Where(s => s.name.ToLower() == stateName.ToLower()).FirstOrDefault();
        }

        /// <summary>Remove specified state by identifier</summary>
        /// <param name="stateName">state identifier</param>
        public void RemoveState(string stateName)
        {
            states = states.Where(s => s.name.ToLower() != stateName.ToLower()).ToList();
        }

        /// <summary>Remove specified state by index</summary>
        /// <param name="stateID">state index</param>
        public void RemoveState(int stateID)
        {
            if (stateID >= 0 && stateID < Count) states.RemoveAt(stateID);
        }

        /// <summary>Remove specified state by state pointer</summary>
        /// <param name="state">state pointer</param>
        public void RemoveState(TSSState state)
        {
            if (states.Contains(state)) states.Remove(state);
        }

        public int GetStateID(TSSState state)
        {
            return states.FindIndex(s => s == state);
        }

        #endregion
    }

    [System.Serializable]
    public class TSSCoreStateSelectedEvent : UnityEvent<TSSState>
    {
        public TSSState TSSState;
    }

    [System.Serializable]
    public class TSSState
    {
        #region Properties

        [SerializeField] private TSSCore core;

        /// <summary>State identifier</summary>
        public string name = "new state";

        /// <summary>State index</summary>
        public int ID { get { return core.GetStateID(this); } }

        /// <summary>State marked as default</summary>
        public bool isDefault;

        /// <summary>Unity editor property</summary>
        public bool editing = false;

        /// <summary>State is enabled</summary>
        public bool enabled = true;

        /// <summary>Use overrided activation modes</summary>
        public bool overrideModes;

        /// <summary>Return true if this state is last</summary>
        public bool isLast { get { return core.Last == this; } }

        /// <summary>Return true if this state is first</summary>
        public bool isFirst { get { return core.First == this; } }

        /// <summary>Return true if this state is last enabled state</summary>
        public bool isLastEnabled { get { return core.LastEnabled == this; } }

        /// <summary>Return true if this state is first enabled state</summary>
        public bool isFirstEnabled { get { return core.FirstEnabled == this; } }

        /// <summary>State open activation mode overriding (default is openBranch)</summary>
        public ActivationMode modeOpenOverride = ActivationMode.openBranch;
        /// <summary>State close activation mode overriding (default is closeBranch)</summary>
        public ActivationMode modeCloseOverride = ActivationMode.closeBranch;

        /// <summary>
        /// List of state activators (state contains only activators, not items directly)
        /// Activator is a container for item and items activation modes.
        /// Each item has activation modes for open and close. 
        /// Item activation modes can be overrided by activator and state can override this modes for all contained activators.
        /// </summary>
        public TSSItemActivator this[int activatorID] { get { return activatorID >= 0 && activatorID < Count ? activators[activatorID] : null; } }

        /// <summary>Count of contined activators</summary>
        public int Count { get { return activators.Count; } }

        [SerializeField] private List<TSSItemActivator> activators = new List<TSSItemActivator>();

        /// <summary>List of keyboard keys for select this state</summary>
        [TSSKeyCode] public List<KeyCode> onKeyboard = new List<KeyCode>();

        /// <summary>Event awakes at state opening</summary>
        /// 
        public UnityEvent onOpen = new UnityEvent();
        /// <summary>Event awakes at state closing</summary>
        public UnityEvent onClose = new UnityEvent();

        #endregion

        #region State

        /// <summary>Opening this state (not affect on others states of parent core)</summary>
        public void Open() 
        {
            if (!enabled) return;
            if (overrideModes) activators.ForEach(i => i.ActivateManualy(modeOpenOverride));
            else activators.ForEach(i => i.Open());

            onOpen.Invoke();
        }

        /// <summary>Close this state (not affect on others states of parent core)</summary>
        public void Close()
        {
            if (!enabled) return;
            if (overrideModes) activators.ForEach(i => i.ActivateManualy(modeCloseOverride));
            else activators.ForEach(i => i.Close());

            onClose.Invoke();
        }

        /// <summary>Activate manualy this state (not affect on others states of parent core)</summary>
        /// <param name="mode">Activation mode</param>
        /// <param name="force">Activate even state is disabled</param>
        public void ActivateManualy(ActivationMode mode, bool force = false)
        {
            if (!force && !enabled) return;
            activators.ForEach(i => i.ActivateManualy(mode));
        }

        /// <summary>Opening this state (and close all other states of parent core)</summary>
        public void Select()
        {
            core.SelectState(this);
        }

        /// <summary>Add activator to state with specified item</summary>
        /// <param name="item">item identifier</param>
        /// <returns>Activator pointer</returns>
        public TSSItemActivator AddItem(TSSItem item)
        {
            activators.Add(new TSSItemActivator(item));
            return activators.Last();
        }

        /// <summary>Remove activator from state by specified item</summary>
        /// <param name="item">item identifier</param>
        public void RemoveItem(TSSItem item)
        {
            activators = activators.Where(a => a.item != item).ToList();
        }

        /// <summary>Remove activator from state by index</summary>
        /// <param name="itemID">activator index</param>
        public void RemoveItem(int itemID)
        {
            if (itemID >= 0 && itemID < Count) activators.RemoveAt(itemID);
        }

        /// <summary>Add selecting key to state</summary>
        /// <param name="key">key on keyboard</param>
        public void AddSelectionKey(KeyCode key)
        {
            if (!onKeyboard.Contains(key)) onKeyboard.Add(key);
        }

        /// <summary>Remove selecting key from state</summary>
        /// <param name="key">key on keyboard</param>
        public void RemoveSelectionKey(KeyCode key)
        {
            if (onKeyboard.Contains(key)) onKeyboard.Remove(key);
        }

        /// <summary>Checks for key pressing (called automatically from core)</summary>
        public void UpdateInput()
        {
            for (int keyID = 0; keyID < onKeyboard.Count; keyID++)
                if (Input.GetKeyDown(onKeyboard[keyID])) { core.SelectState(name); break; }
        }

        /// <summary>Remove this state from parent core</summary>
        public void Remove()
        {
            core.RemoveState(this);
        }

        /// <summary>Mark this state as default for parent core</summary>
        public void SetAsDefault()
        {
            core.SetDefaultState(this);
        }

        public TSSState(TSSCore core)
        {
            this.core = core;
            activators.Add(new TSSItemActivator());
        }

        public TSSState(TSSCore core, TSSItem item)
        {
            this.core = core;
            activators.Add(new TSSItemActivator(item));
        }

        #endregion
    }
}