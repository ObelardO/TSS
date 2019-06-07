using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TSS;

public class CoreBuilder : MonoBehaviour
{
    // how many slides generate
    public int stateCount = 5;

    // slide prefab
    public TSSItem stateObject;

    // navigation buttons
    public Button prevBtn, nextBrn;

    // Use this for initialization
    void Awake ()
    {
        // Add "TSS Core" component
        TSSCore core = this.gameObject.AddComponent<TSSCore>();

        // Clone state object and additing clone to core as new state
        for (int i = 0; i < stateCount; i++)
        {
            // Clone state or use first
            TSSItem newStateObject = i == 0 ? 
                stateObject : 
                Instantiate(stateObject.gameObject, this.transform).GetComponent<TSSItem>();

            // Mark state text by index
            Text stateText = newStateObject.GetComponentInChildren<Text>();
            stateText.text = (i + 1).ToString();

            // Add state and selecting key
            TSSState stateState = core.AddState(newStateObject, (i + 1).ToString());
            stateState.AddSelectionKey((KeyCode)((int)KeyCode.Alpha1 + i));
        }

        // Set first core state as default. Default state will be selected and activated on start.
        // You can use any of syntax:
        // core.SetDefaultState(core.GetState("0"));
        // core.SetDefaultState("0");
        // core.SetDefaultState(core[0]);
        core[0].SetAsDefault();

        // Allow core events
        core.useEvents = true;

        // Update navigation buttons interactable by core events
        core.OnStateSelected.AddListener(state =>
        {
            prevBtn.interactable = !state.isFirst;
            nextBrn.interactable = !state.isLast;
        });

        // Attach core selecting method to buttons
        nextBrn.onClick.AddListener(core.SelectNextState);
        prevBtn.onClick.AddListener(core.SelectPreviousState);

        // Select core state
        // You can use any of syntax:
        // core.SelectState(core.GetState(0).ToString()));
        // core.SelectState("0");
        // core.GetState((currentStateID + 1).ToString()).Select();
        core[0].Select();
    }
}
