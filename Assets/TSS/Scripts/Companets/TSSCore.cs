// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru
// MIT License

using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using TSS.Base;

namespace TSS
{
    #region Enumerations

    public enum IncorrectStateAction { OpenDefault, None, CloseAll }

    #endregion

    [System.Serializable, AddComponentMenu("TSS/Core")]
    public class TSSCore : MonoBehaviour
    {
        #region Properties

        /// <summary>States list</summary>
        public TSSState this[int stateID] { get { return stateID >= 0 && stateID < Count ? states[stateID] : null; } }

        /// <summary>Count of states list</summary>
        public int Count { get { return states.Count; } }

        /// <summary>Last added state</summary>
        public TSSState last { get { return this[Count - 1]; } }

        [SerializeField] private List<TSSState> states = new List<TSSState>();

        /// <summary>Current opened state</summary>
        public TSSState currentState;

        /// <summary>Core state marked as default</summary>
        public TSSState defaultState
        {
            get { return states.Where(s => s.isDefault == true).FirstOrDefault(); }
        }

        public IncorrectStateAction incorrectAction;

        #endregion

        #region Unity methods

        private IEnumerator Start()
        {
            if (Application.isPlaying) yield return new WaitForSeconds(0.5f);
            SelectDefaultState();
        }

        private void Update()
        {
            if (!Input.anyKeyDown) return;

            for (int stateID = 0; stateID < states.Count; stateID++)
                if (!this[stateID].enabled) continue; else this[stateID].UpdateInput();
        }

        private void OnDrawGizmos()
        {

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
                    case IncorrectStateAction.OpenDefault: SelectDefaultState(); break;
                    case IncorrectStateAction.CloseAll: states.ForEach(s => s.Close()); break;
                }
                return;
            }

            currentState = state;

            states.Where(s => s.name.ToLower() != state.name.ToLower()).ToList().ForEach(s => s.Close());
            currentState.Open();
        }

        /// <summary>Open specified state</summary>
        /// <param name="stateName">state identifier</param>
        public void SelectState(string stateName)
        {
            SelectState(states.Where(s => s.name.ToLower() == stateName.ToLower() && s.enabled).FirstOrDefault());
        }

        /// <summary>Close all states</summary>
        /// <param name="stateName">state identifier</param>
        public void CloseAll()
        {
            states.ForEach(s => s.Close());
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

        #endregion
    }
    
    [System.Serializable]
    public class TSSState
    {
        #region Properties

        [SerializeField] private TSSCore core;

        /// <summary>State identifier</summary>
        public string name = "new state";

        /// <summary>State marked as default</summary>
        public bool isDefault;

        /// <summary>Unity editor property</summary>
        public bool editing = false;

        /// <summary>State is enabled</summary>
        public bool enabled = true;

        /// <summary>Use overrided activation modes</summary>
        public bool overrideModes;

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