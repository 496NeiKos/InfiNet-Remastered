/*
 * ================================================================
 *  UNITY SETUP GUIDE — TopicCompletePopUp
 * ================================================================
 *  PURPOSE
 *    Victory divider panel that appears whenever an objective group
 *    inside the COC I scene is fully completed (e.g. Disassembly,
 *    Assembly, UEFI Configuration, etc.). Triggered by
 *    ObjectiveTaskDisplay.OnObjectiveGroupComplete — up to 8 times
 *    across the scene's three topics.
 *
 *  STEP 1 — Create the manager GameObject
 *    a) Empty enabled GameObject: "TopicCompletePopUpManager"
 *    b) Add component: TopicCompletePopUp  ← THIS SCRIPT
 *    c) Keep this object ENABLED — it must stay alive to receive events.
 *
 *  STEP 2 — Create the popup UI GameObject (separate)
 *    a) Canvas child or standalone Canvas (Screen Space – Overlay,
 *       Sort Order 110) named "TopicCompletePopUp"
 *    b) Start it INACTIVE in the scene.
 *
 *  STEP 3 — Build the popup child hierarchy
 *    TopicCompletePopUp  (starts inactive)
 *      ├─ Background          ← Image (dark semi-transparent overlay)
 *      ├─ TopicCompleteImage  ← Image (decorative banner / icon)
 *      ├─ HeaderText          ← TMP_Text  static: "Topic Completed"
 *      ├─ BodyText            ← TMP_Text  dynamic: "[name] has been..."
 *      └─ CloseButton         ← Button → TopicCompletePopUp.Close()
 *
 *  STEP 4 — Wire inspector fields on the manager
 *    popupPanel          → TopicCompletePopUp root GameObject
 *    background          → Background Image
 *    topicCompleteImage  → TopicCompleteImage Image
 *    headerText          → HeaderText TMP component
 *    bodyText            → BodyText TMP component
 *    closeButton         → CloseButton Button component
 *
 *  STEP 5 — Wire the Close button
 *    On CloseButton's OnClick(), add TopicCompletePopUpManager → Close()
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopicCompletePopUp : MonoBehaviour
{
    [Header("Panel Root")]
    [SerializeField] private GameObject popupPanel;

    [Header("UI References")]
    [SerializeField] private Image background;
    [SerializeField] private Image topicCompleteImage;
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button closeButton;

    private const string BodyTemplate = " has been completed, proceed to other topic of this COC if not yet finished.";

    private void Start()
    {
        ObjectiveTaskDisplay.OnObjectiveGroupComplete += Show;

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void OnDestroy()
    {
        ObjectiveTaskDisplay.OnObjectiveGroupComplete -= Show;

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    private void Show(string objectiveName)
    {
        if (headerText != null)
            headerText.text = "Topic Completed";

        if (bodyText != null)
            bodyText.text = objectiveName + BodyTemplate;

        if (popupPanel != null)
            popupPanel.SetActive(true);
    }

    public void Close()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);
    }
}
