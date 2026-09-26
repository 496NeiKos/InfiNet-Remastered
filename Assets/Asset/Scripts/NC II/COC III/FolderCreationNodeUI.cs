/*
 * ================================================================
 *  UNITY SETUP GUIDE — FolderCreationNodeUI (COC III)
 * ================================================================
 *  PREFAB HIERARCHY  (folderCreationPrefab)
 *    FolderCreationNode                ← FolderCreationNodeUI here
 *      ├── FolderIcon (Image)          optional folder icon sprite
 *      ├── RenameInput (TMP_InputField) → inputField
 *      └── ConfirmedButton (Button)    → confirmedButton   [starts INACTIVE]
 *            └── Label (TMP_Text)      → buttonLabel
 *
 *  COMPONENT SETUP
 *    Root GO:
 *      • RectTransform — Height: 28
 *      • HorizontalLayoutGroup — padding L:4 R:4, spacing:6, child force expand W: ON
 *      • FolderCreationNodeUI
 *    RenameInput:
 *      • TMP_InputField — placeholder text: ""
 *      • ContentType: Standard
 *    ConfirmedButton:
 *      • Button — no transition (color tint or none)
 *      • Starts INACTIVE in prefab
 *
 *  HOW IT WORKS
 *    Instantiated by FileExplorerController when New Folder is clicked.
 *    Start() activates the InputField and focuses it immediately.
 *    Pressing Enter or losing focus fires OnEndEdit → confirms the name.
 *    Empty input defaults to "New folder".
 *    onConfirmed callback fires → FileExplorerController registers the folder.
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FolderCreationNodeUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button         confirmedButton;
    [SerializeField] private TMP_Text       buttonLabel;

    public System.Action<string> onConfirmed;

    private void Start()
    {
        confirmedButton.gameObject.SetActive(false);
        inputField.gameObject.SetActive(true);
        inputField.text = "";
        inputField.ActivateInputField();
        inputField.onEndEdit.AddListener(OnEndEdit);
    }

    private void OnEndEdit(string value)
    {
        string name = string.IsNullOrWhiteSpace(value) ? "New folder" : value.Trim();
        inputField.gameObject.SetActive(false);
        if (buttonLabel != null) buttonLabel.text = name;
        confirmedButton.gameObject.SetActive(true);
        onConfirmed?.Invoke(name);
    }
}
