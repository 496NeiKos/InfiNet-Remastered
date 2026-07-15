/*
 * ================================================================
 *  UNITY SETUP GUIDE — SettingPanelController
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to the SettingPanel GameObject.
 *
 *  HIERARCHY
 *
 *    SettingPanel  (this script here, start INACTIVE)
 *      ├─ SettingNav                        (always active — persistent)
 *      │    ├─ SettingHomeBtn → OnClick: SettingPanelController.GoHome()
 *      │    ├─ MinimizeBtn   → OnClick: SettingPanelController.Minimize()
 *      │    └─ ExitBtn       → OnClick: SettingPanelController.Exit()
 *      ├─ 1stLevel                          → firstLevel
 *      │    ├─ SettingHeader
 *      │    │    ├─ WindowsUpdateBtnHeader → OnClick: SettingPanelController.GoToWindowsUpdate()
 *      │    │    └─ [other buttons — no function]
 *      │    └─ SettingMainContents
 *      │         └─ WindowsUpdateBtnMain  → OnClick: SettingPanelController.GoToWindowsUpdate()
 *      ├─ 2ndLevel                          → secondLevel (start INACTIVE)
 *      │    └─ WindowsUpdate               → windowsUpdate
 *      │         └─ WU_MainContent
 *      │              ├─ WU_MainContentBody
 *      │              │    └─ [Required driver buttons — always visible, click = download (TBD)]
 *      │              └─ OptionalUpdatesBtn → OnClick: SettingPanelController.ShowOptionalUpdates()
 *      └─ 3rdLevel                          → thirdLevel (start INACTIVE)
 *           └─ OptionalUpdate              → optionalUpdate
 *                └─ OptionalUpdatesContent
 *                     ├─ DriverUpdatesBtn  → OnClick: SettingPanelController.ToggleDriverUpdates()
 *                     ├─ DriverUpdatesOption → driverUpdatesOption (start INACTIVE)
 *                     │    └─ [Optional driver buttons] → driverOptionButtons[]
 *                     └─ Download&Install  → downloadInstallButton (non-interactable by default)
 *                          OnClick: SettingPanelController.OnDownloadInstall()
 *
 *  INSPECTOR ASSIGNMENTS
 *    firstLevel            → SettingPanel > 1stLevel
 *    secondLevel           → SettingPanel > 2ndLevel
 *    thirdLevel            → SettingPanel > 3rdLevel
 *    windowsUpdate         → 2ndLevel > WindowsUpdate
 *    optionalUpdate        → 3rdLevel > OptionalUpdate
 *    driverUpdatesOption   → OptionalUpdate > DriverUpdatesOption
 *    driverOptionButtons   → all Button components inside DriverUpdatesOption (assign as array)
 *    downloadInstallButton → OptionalUpdate > Download&Install (Button)
 *    selectedColor         → highlight color when an option button is selected (default blue)
 *    normalColor           → resting color of option buttons (default white)
 *
 *  HOW IT WORKS — two independent driver systems:
 *
 *    WU_MainContentBody (required drivers):
 *      Buttons are always visible. Each represents a required driver/update.
 *      Clicking one will trigger its individual download — logic TBD.
 *      These buttons are not managed by this script.
 *
 *    DriverUpdatesOption (optional drivers):
 *      DriverUpdatesBtn toggles the panel open/closed.
 *      Clicking an option button highlights it (selected) or restores it
 *      (deselected). Download&Install is interactable when ≥1 is selected.
 *      Clicking Download&Install clears all selections and disables itself.
 *      The actual install effect (task list, etc.) is added later.
 *
 *    Level navigation:
 *      Only one level is visible at a time.
 *      GoHome / Exit always returns to 1stLevel.
 *      Minimize preserves the current level.
 * ================================================================
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingPanelController : MonoBehaviour
{
    // ----------------------------------------------------------------
    //  Levels
    // ----------------------------------------------------------------

    [Header("Levels")]
    [SerializeField] private GameObject firstLevel;
    [SerializeField] private GameObject secondLevel;
    [SerializeField] private GameObject thirdLevel;

    // ----------------------------------------------------------------
    //  Sub-panels
    // ----------------------------------------------------------------

    [Header("Sub-Panels")]
    [SerializeField] private GameObject windowsUpdate;
    [SerializeField] private GameObject optionalUpdate;
    [SerializeField] private GameObject driverUpdatesOption;

    // ----------------------------------------------------------------
    //  Driver Updates — optional drivers
    // ----------------------------------------------------------------

    [Header("Driver Updates — Optional")]
    [SerializeField] private Button[] driverOptionButtons;
    [SerializeField] private Button   downloadInstallButton;
    [SerializeField] private Color    selectedColor = new Color(0.2f, 0.55f, 1f, 1f);
    [SerializeField] private Color    normalColor   = Color.white;

    // ----------------------------------------------------------------
    //  Runtime state
    // ----------------------------------------------------------------

    // Latches true the first time Windows Update (2ndLevel) is opened (Task 16).
    public bool WindowsUpdateAccessed { get; private set; }

    private readonly HashSet<int> _selectedIndices = new HashSet<int>();

    // ----------------------------------------------------------------
    //  Lifecycle
    // ----------------------------------------------------------------

    private void Awake()
    {
        for (int i = 0; i < driverOptionButtons.Length; i++)
        {
            int captured = i;
            if (driverOptionButtons[i] != null)
                driverOptionButtons[i].onClick.AddListener(() => OnDriverOptionClicked(captured));
        }

        SetButtonInteractable(downloadInstallButton, false);
    }

    // ----------------------------------------------------------------
    //  Window control
    // ----------------------------------------------------------------

    public void Minimize()
    {
        gameObject.SetActive(false);
        Debug.Log("[SettingPanelController] Minimized — level state preserved.");
    }

    public void Exit()
    {
        ShowLevel(firstLevel);
        gameObject.SetActive(false);
        Debug.Log("[SettingPanelController] Exited — reset to 1stLevel.");
    }

    // ----------------------------------------------------------------
    //  Level navigation
    // ----------------------------------------------------------------

    public void Open()
    {
        ShowLevel(firstLevel);
        Debug.Log("[SettingPanelController] Opened — showing 1stLevel.");
    }

    public void GoHome()
    {
        ShowLevel(firstLevel);
        Debug.Log("[SettingPanelController] SettingHome — returned to 1stLevel.");
    }

    public void GoToWindowsUpdate()
    {
        ShowLevel(secondLevel);
        if (windowsUpdate != null) windowsUpdate.SetActive(true);

        if (!WindowsUpdateAccessed)
        {
            WindowsUpdateAccessed = true;
            T3TaskListManager.CheckConditions();
        }

        Debug.Log("[SettingPanelController] Navigated to WindowsUpdate (2ndLevel).");
    }

    public void ShowOptionalUpdates()
    {
        ShowLevel(thirdLevel);
        if (optionalUpdate != null)      optionalUpdate.SetActive(true);
        if (driverUpdatesOption != null) driverUpdatesOption.SetActive(false);
        Debug.Log("[SettingPanelController] Navigated to OptionalUpdates (3rdLevel).");
    }

    public void ToggleDriverUpdates()
    {
        if (driverUpdatesOption == null) return;
        bool show = !driverUpdatesOption.activeSelf;
        driverUpdatesOption.SetActive(show);
        Debug.Log($"[SettingPanelController] DriverUpdatesOption {(show ? "shown" : "hidden")}.");
    }

    // ----------------------------------------------------------------
    //  Download & Install — optional drivers
    // ----------------------------------------------------------------

    public void OnDownloadInstall()
    {
        int count = _selectedIndices.Count;

        foreach (int idx in _selectedIndices)
        {
            if (idx < driverOptionButtons.Length && driverOptionButtons[idx] != null)
                driverOptionButtons[idx].gameObject.SetActive(false);
        }

        _selectedIndices.Clear();
        SetButtonInteractable(downloadInstallButton, false);

        // Install effect (task list integration etc.) — added later
        Debug.Log($"[SettingPanelController] Download & Install triggered for {count} optional driver(s).");
    }

    // ----------------------------------------------------------------
    //  Driver option button selection
    // ----------------------------------------------------------------

    private void OnDriverOptionClicked(int index)
    {
        if (_selectedIndices.Contains(index))
        {
            _selectedIndices.Remove(index);
            SetOptionColor(index, false);
        }
        else
        {
            _selectedIndices.Add(index);
            SetOptionColor(index, true);
        }

        SetButtonInteractable(downloadInstallButton, _selectedIndices.Count > 0);
    }

    // ----------------------------------------------------------------
    //  Helpers
    // ----------------------------------------------------------------

    private void SetOptionColor(int index, bool selected)
    {
        if (index >= driverOptionButtons.Length) return;
        var btn = driverOptionButtons[index];
        if (btn == null) return;

        var img = btn.targetGraphic as Image;
        if (img != null)
            img.color = selected ? selectedColor : normalColor;
    }

    private void ShowLevel(GameObject levelToShow)
    {
        if (firstLevel  != null) firstLevel.SetActive(false);
        if (secondLevel != null) secondLevel.SetActive(false);
        if (thirdLevel  != null) thirdLevel.SetActive(false);
        if (levelToShow != null) levelToShow.SetActive(true);
    }

    private static void SetButtonInteractable(Button btn, bool interactable)
    {
        if (btn != null) btn.interactable = interactable;
    }
}
