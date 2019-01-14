using UnityEngine;
using UnityEngine.UI;
using TSS;

public class CoreBuilder : MonoBehaviour
{
    public int stateCount = 5;
    public TSSItem stateObject;
    public Button prevBtn, nextBrn;

    private TSSCore core;
    private int currentStateID = 0;

	// Use this for initialization
	void Start ()
    {
        // Add "TSS Core" component
        core = this.gameObject.AddComponent<TSSCore>();

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
        // core.SetDefaultState(core.GetState("1"));
        // core.SetDefaultState("3");
        // core.SetDefaultState(core[2]);
        core[0].SetAsDefault();

        // Update navigation buttons
        SelectState(0);
    }

    // Calling from navigation buttons
    public void SelectState(int stateIDoffset)
    {
        // Check for limits
        if (currentStateID + stateIDoffset < 0 || currentStateID + stateIDoffset > stateCount - 1) return;

        // New state ID
        currentStateID += stateIDoffset;

        // Update navigation buttons interactable
        prevBtn.interactable = !(currentStateID == 0);
        nextBrn.interactable = !(currentStateID == stateCount - 1);

        // Select core state
        // You can use any of syntax:
        // core.SelectState(core.GetState((currentStateID + 1).ToString()));
        // core.SelectState((currentStateID + 1).ToString());
        // core.GetState((currentStateID + 1).ToString()).Select();
        core[currentStateID].Select();
    }
}
