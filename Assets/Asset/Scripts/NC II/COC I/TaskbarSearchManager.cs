/*
 * ================================================================
 *  UNITY SETUP GUIDE — TaskbarSearchManager
 * ================================================================
 *  PURPOSE
 *    Manages the Desktop taskbar search flow:
 *      Taskbar click → opens WindowIconContent
 *      TaskbarSearch typing → filters visible app icons
 *      RunAsAdministratorBtn → opens CommandPromptApp
 *
 * ----------------------------------------------------------------
 *  COMPONENT PLACEMENT
 * ----------------------------------------------------------------
 *    Add this script to the Taskbar GameObject (the same one that
 *    already has the Button component).
 *
 * ----------------------------------------------------------------
 *  INITIAL SCENE STATE — SET THESE BEFORE ENTERING PLAY MODE
 * ----------------------------------------------------------------
 *    WindowIconContent  → INACTIVE  (hidden until Taskbar is clicked)
 *    SearchedPanel      → INACTIVE  (hidden until search matches)
 *    CommandPromptApp   → INACTIVE  (hidden until RunAsAdmin is clicked)
 *    CommandPromptIcon  → ACTIVE    (always visible when SearchedPanel is open)
 *
 * ----------------------------------------------------------------
 *  INSPECTOR WIRING
 * ----------------------------------------------------------------
 *    windowIconContent  → WindowIconContent  (child of Taskbar)
 *    taskbarSearch      → TaskbarSearch      (TMP_InputField, child of WindowIconContent)
 *    searchedPanel      → SearchedPanel      (child of WindowIconContent)
 *    commandPromptApp   → CommandPromptApp   (child of DesktopPanel)
 *
 * ----------------------------------------------------------------
 *  BUTTON / EVENT WIRING
 * ----------------------------------------------------------------
 *    Taskbar Button          OnClick → OnTaskbarClicked()
 *    RunAsAdministratorBtn   OnClick → OnRunAsAdminClicked()
 *
 *    TaskbarSearch InputField — NO wiring needed. Remove any existing
 *    OnValueChanged listener from its Inspector event list. The script
 *    reads the InputField text directly every frame via Update().
 *
 * ----------------------------------------------------------------
 *  SEARCH KEYWORDS  (checked every frame via Update)
 * ----------------------------------------------------------------
 *    Typing any of the following (case-insensitive) opens
 *    SearchedPanel; anything else hides it:
 *      "cmd"
 *      "command"
 *      "command prompt"
 * ================================================================
 */

using TMPro;
using UnityEngine;

public class TaskbarSearchManager : MonoBehaviour
{
    [Header("Taskbar Search")]
    [Tooltip("The panel that appears when the Taskbar is clicked. Starts INACTIVE.")]
    [SerializeField] private GameObject windowIconContent;
    [Tooltip("TMP_InputField inside WindowIconContent. Wire OnValueChanged → OnSearchValueChanged.")]
    [SerializeField] private TMP_InputField taskbarSearch;

    [Header("Search Results")]
    [Tooltip("The panel revealed when search matches. Starts INACTIVE. CommandPromptIcon lives inside here and stays active.")]
    [SerializeField] private GameObject searchedPanel;

    [Header("Apps")]
    [Tooltip("The CommandPromptApp panel opened by RunAsAdministratorBtn. Starts INACTIVE.")]
    [SerializeField] private GameObject commandPromptApp;

    private void Awake()
    {
        // Enforce correct initial state regardless of scene authoring.
        if (windowIconContent != null) windowIconContent.SetActive(false);
        if (searchedPanel     != null) searchedPanel.SetActive(false);
        if (commandPromptApp  != null) commandPromptApp.SetActive(false);
    }

    // ----------------------------------------------------------------
    //  Taskbar Button → OnClick
    // ----------------------------------------------------------------

    public void OnTaskbarClicked()
    {
        if (windowIconContent == null) return;

        bool opening = !windowIconContent.activeSelf;
        windowIconContent.SetActive(opening);

        if (opening)
        {
            // Clear search and hide results so each open starts fresh.
            if (taskbarSearch != null)
            {
                taskbarSearch.text = string.Empty;
                taskbarSearch.ActivateInputField();
            }
            if (searchedPanel != null)
                searchedPanel.SetActive(false);
        }
    }

    // ----------------------------------------------------------------
    //  Search polling — runs every frame while WindowIconContent is open
    // ----------------------------------------------------------------

    private void Update()
    {
        if (windowIconContent == null || !windowIconContent.activeSelf) return;
        if (taskbarSearch == null || searchedPanel == null) return;

        string normalized = taskbarSearch.text == null
            ? string.Empty
            : taskbarSearch.text.Trim().ToLowerInvariant();

        bool matches = normalized == "cmd"
                    || normalized == "command"
                    || normalized == "command prompt";

        if (searchedPanel.activeSelf != matches)
            searchedPanel.SetActive(matches);
    }

    // ----------------------------------------------------------------
    //  RunAsAdministratorBtn → OnClick
    // ----------------------------------------------------------------

    public void OnRunAsAdminClicked()
    {
        if (commandPromptApp != null)
            commandPromptApp.SetActive(true);

        // Close the search panel after launching.
        if (windowIconContent != null)
            windowIconContent.SetActive(false);

        Debug.Log("[TaskbarSearchManager] Command Prompt opened as Administrator.");
    }
}
