/*
 * ================================================================
 *  UNITY SETUP GUIDE — PartitionManager
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to the ThirdPhase GameObject (child of
 *    SetUpLicenseAgreement, grandchild of WindowsSetupPanel).
 *
 *  HIERARCHY EXPECTED
 *
 *    ThirdPhase  (this script here)
 *      ├─ InstallationPath
 *      │    ├─ Drive1  → drive button 0  (Button + Text child)
 *      │    ├─ Drive2  → drive button 1
 *      │    ├─ Drive3  → drive button 2
 *      │    └─ Drive4  → drive button 3
 *      ├─ InstallationEditor
 *      │    ├─ Refresh  → refreshButton
 *      │    ├─ Delete   → deleteButton
 *      │    ├─ Format   → formatButton
 *      │    └─ New      → newButton
 *      ├─ NewDriveSize   (disabled at start)
 *      │    ├─ SizeField → sizeField (TMP_InputField)
 *      │    ├─ SizeUp    → sizeUpButton
 *      │    ├─ SizeDown  → sizeDownButton
 *      │    ├─ Apply     → applyButton
 *      │    └─ Cancel    → cancelNewButton
 *      ├─ Footer
 *      │    ├─ Next                           → nextButton
 *      │    └─ InstallationPathValidatorDisplay → validatorText
 *      ├─ Title
 *      └─ EditorPopUp   (disabled at start)
 *           ├─ Delete    (disabled at start — shown for delete confirmation)
 *           │    ├─ Proceed → popupDeleteProceedButton
 *           │    ├─ Return  → popupDeleteReturnButton
 *           │    └─ Text (TMP) — static body text in scene
 *           └─ New       (disabled at start — shown for new-partition confirmation)
 *                ├─ Proceed → popupNewProceedButton
 *                └─ Return  → popupNewReturnButton
 *
 *  HOW IT WORKS
 *    InitPartitions() resets the partition list to the four initial
 *    partitions each time the ThirdPhase is entered (called by
 *    WindowsSetupNavigator.OnCustomSelected()).
 *
 *    Drive buttons are fixed in the scene (Drive1–Drive4). The script
 *    shows/hides and re-labels them as partitions are deleted or
 *    created. There is always at most one "Unallocated Space" entry
 *    (deleted partitions merge into it).
 *
 *  BUTTON ENABLE RULES
 *    ┌──────────────────────┬────────┬─────┬────────┬──────────────────┐
 *    │ Selection            │ Delete │ New │ Format │ Next             │
 *    ├──────────────────────┼────────┼─────┼────────┼──────────────────┤
 *    │ Nothing              │   ✗    │  ✗  │   ✗    │  ✗               │
 *    │ Named partition      │   ✓    │  ✗  │   ✓    │  Only if Primary │
 *    │ Unallocated Space    │   ✗    │  ✓  │   ✗    │  ✗               │
 *    └──────────────────────┴────────┴─────┴────────┴──────────────────┘
 *
 *  DELETE FLOW
 *    1. Select partition → click Delete
 *    2. EditorPopUp + Delete sub-panel activates
 *    3. Click Proceed → partition removed; size merges into Unallocated
 *    4. Click Return  → popup closes, nothing changes
 *
 *  NEW PARTITION FLOW
 *    1. Select Unallocated Space → click New
 *    2. NewDriveSize panel shows; SizeField pre-filled with full unallocated MB
 *    3. Adjust size with SizeUp/SizeDown or type directly
 *    4. Click Apply  → NewDriveSize closes; EditorPopUp + New sub-panel opens (confirmation)
 *    5. Click Proceed in popup → Windows auto-creates three partitions from specified space:
 *         • Drive X   (System)  — 100 MB
 *         • Drive X+1 (MBR)     — 50 MB
 *         • Drive X+2 (Primary) — specified size minus 150 MB overhead
 *       Remaining unallocated (if ≥ 1 GB) stays as an Unallocated entry.
 *    6. Click Return in popup → popup closes, nothing changes
 *
 * ================================================================
 *  INSPECTOR ASSIGNMENTS
 * ================================================================
 *
 *  Drive Buttons  (InstallationPath children, in order)
 *    driveButtons[0]     → Drive1   (Button component)
 *    driveButtons[1]     → Drive2
 *    driveButtons[2]     → Drive3
 *    driveButtons[3]     → Drive4
 *    driveButtonTexts[0] → Drive1 > Text   (legacy Text component)
 *    driveButtonTexts[1] → Drive2 > Text
 *    driveButtonTexts[2] → Drive3 > Text
 *    driveButtonTexts[3] → Drive4 > Text
 *
 *  Editor Buttons  (InstallationEditor children)
 *    deleteButton  → Delete   (Button)
 *    newButton     → New      (Button)
 *    formatButton  → Format   (Button)
 *    refreshButton → Refresh  (Button)
 *
 *  Footer
 *    nextButton    → Next                            (Button)
 *    validatorText → InstallationPathValidatorDisplay (legacy Text)
 *
 *  NewDriveSize Panel
 *    newDriveSizePanel → NewDriveSize          (GameObject)
 *    sizeField         → NewDriveSize > SizeField (TMP_InputField)
 *    sizeUpButton      → NewDriveSize > SizeUp    (Button)
 *    sizeDownButton    → NewDriveSize > SizeDown  (Button)
 *    applyButton       → NewDriveSize > Apply     (Button)
 *    cancelNewButton   → NewDriveSize > Cancel    (Button)
 *
 *  EditorPopUp — Delete sub-panel
 *    editorPopUp              → EditorPopUp                    (GameObject)
 *    popupDeletePanel         → EditorPopUp > Delete           (GameObject)
 *    popupDeleteProceedButton → EditorPopUp > Delete > Proceed (Button)
 *    popupDeleteReturnButton  → EditorPopUp > Delete > Return  (Button)
 *
 *  EditorPopUp — New sub-panel
 *    popupNewPanel         → EditorPopUp > New           (GameObject)
 *    popupNewProceedButton → EditorPopUp > New > Proceed (Button)
 *    popupNewReturnButton  → EditorPopUp > New > Return  (Button)
 *
 *  BUTTON WIRING  (OnClick in Inspector)
 *    Drive1 > OnClick → PartitionManager.OnDrive0Selected()
 *    Drive2 > OnClick → PartitionManager.OnDrive1Selected()
 *    Drive3 > OnClick → PartitionManager.OnDrive2Selected()
 *    Drive4 > OnClick → PartitionManager.OnDrive3Selected()
 *
 *    Delete  > OnClick → PartitionManager.OnDeleteClicked()
 *    New     > OnClick → PartitionManager.OnNewClicked()
 *    Refresh > OnClick → PartitionManager.OnRefreshClicked()
 *    Format  > OnClick → PartitionManager.OnFormatClicked()
 *
 *    EditorPopUp > Delete > Proceed > OnClick → PartitionManager.OnDeletePopupProceed()
 *    EditorPopUp > Delete > Return  > OnClick → PartitionManager.OnDeletePopupReturn()
 *
 *    NewDriveSize > SizeUp   > OnClick → PartitionManager.OnSizeUp()
 *    NewDriveSize > SizeDown > OnClick → PartitionManager.OnSizeDown()
 *    NewDriveSize > Apply    > OnClick → PartitionManager.OnApplyClicked()
 *    NewDriveSize > Cancel   > OnClick → PartitionManager.OnCancelNewPartition()
 *
 *    EditorPopUp > New > Proceed > OnClick → PartitionManager.OnNewPopupProceed()
 *    EditorPopUp > New > Return  > OnClick → PartitionManager.OnNewPopupReturn()
 *
 *    Footer > Next > OnClick → PartitionManager.OnNextClicked()
 * ================================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartitionManager : MonoBehaviour
{
    // ----------------------------------------------------------------
    //  Data model
    // ----------------------------------------------------------------

    [System.Serializable]
    public class DrivePartition
    {
        public string partitionName;
        public float  totalSizeMB;
        public float  freeSpaceMB;
        public string partitionType;   // "System","MBR","Primary","Recovery" or "" for unallocated
        public bool   isUnallocated;

        public DrivePartition(string name, float totalMB, float freeMB, string type, bool unallocated = false)
        {
            partitionName = name;
            totalSizeMB   = totalMB;
            freeSpaceMB   = freeMB;
            partitionType = type;
            isUnallocated = unallocated;
        }

        // Formats MB → human-readable string (TB / GB / MB)
        public static string FormatSize(float mb)
        {
            if (mb >= 1024f * 1024f) return $"{mb / (1024f * 1024f):F1} TB";
            if (mb >= 1024f)         return $"{mb / 1024f:F0} GB";
            return $"{mb:F0} MB";
        }

        // Two-line label shown inside each drive button.
        // Columns are separated by wide spacing for readability.
        public string GetButtonLabel()
        {
            string total = FormatSize(totalSizeMB);
            string free  = FormatSize(freeSpaceMB);

            if (isUnallocated)
                return $"{partitionName}\nTotal: {total}               Free: {free}";

            return $"{partitionName}\nTotal: {total}               Free: {free}               Type: {partitionType}";
        }
    }

    // ----------------------------------------------------------------
    //  Inspector fields
    // ----------------------------------------------------------------

    [Header("Drive Buttons  (InstallationPath children, in order)")]
    [SerializeField] private Button[] driveButtons;      // Drive1, Drive2, Drive3, Drive4
    [SerializeField] private Text[]   driveButtonTexts;  // legacy Text child of each drive button

    [Header("Editor Buttons  (InstallationEditor children)")]
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button newButton;
    [SerializeField] private Button formatButton;
    [SerializeField] private Button refreshButton;

    [Header("Footer")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Text   validatorText;       // InstallationPathValidatorDisplay

    [Header("NewDriveSize Panel")]
    [SerializeField] private GameObject    newDriveSizePanel;
    [SerializeField] private TMP_InputField sizeField;
    [SerializeField] private Button        sizeUpButton;
    [SerializeField] private Button        sizeDownButton;
    [SerializeField] private Button        applyButton;
    [SerializeField] private Button        cancelNewButton;

    [Header("EditorPopUp — shared overlay")]
    [SerializeField] private GameObject editorPopUp;

    [Header("EditorPopUp — Delete sub-panel")]
    [SerializeField] private GameObject popupDeletePanel;
    [SerializeField] private Button     popupDeleteProceedButton;
    [SerializeField] private Button     popupDeleteReturnButton;

    [Header("EditorPopUp — New sub-panel")]
    [SerializeField] private GameObject popupNewPanel;
    [SerializeField] private Button     popupNewProceedButton;
    [SerializeField] private Button     popupNewReturnButton;

    [Header("Navigation")]
    [SerializeField] private WindowsSetupNavigator navigator;

    // ----------------------------------------------------------------
    //  Initial partition data — realistic 1 TB drive layout
    //  Sizes in MB:  100 MB System | 500 MB Reserved | ~931 GB Primary | 500 MB Recovery
    // ----------------------------------------------------------------

    private static readonly string[] InitialNames = {
        "Drive 0 Partition 1",
        "Drive 0 Partition 2",
        "Drive 0 Partition 3",
        "Drive 0 Partition 4"
    };
    private static readonly float[]  InitialTotal = { 100f, 500f, 953344f, 512f };
    private static readonly float[]  InitialFree  = { 70f,  490f, 901120f, 490f };
    private static readonly string[] InitialTypes = { "System", "Reserved", "Primary", "Recovery" };

    // Windows auto-creates System (100 MB) and MBR (50 MB) alongside every new Primary
    private const float SystemOverheadMB = 100f;
    private const float MbrOverheadMB    = 50f;
    private const float TotalOverheadMB  = SystemOverheadMB + MbrOverheadMB; // 150 MB

    // SizeUp / SizeDown step: 1 GB
    private const int SizeStepMB    = 1024;
    // Minimum allowed new partition size: 1 GB (must exceed 150 MB overhead)
    private const int MinNewSizeMB  = 1024;

    // ----------------------------------------------------------------
    //  Runtime state
    // ----------------------------------------------------------------

    private readonly List<DrivePartition> partitions = new List<DrivePartition>();
    private int   selectedIndex    = -1;
    private float pendingNewSizeMB = 0f;   // stores the size confirmed in NewDriveSize before popup

    // ----------------------------------------------------------------
    //  Public init — called by WindowsSetupNavigator on Custom selected
    // ----------------------------------------------------------------

    public void InitPartitions()
    {
        partitions.Clear();
        selectedIndex    = -1;
        pendingNewSizeMB = 0f;

        for (int i = 0; i < 4; i++)
        {
            partitions.Add(new DrivePartition(
                InitialNames[i], InitialTotal[i], InitialFree[i], InitialTypes[i]));
        }

        CloseAllPanels();
        RefreshUI();
        Debug.Log("[PartitionManager] Partition list reset to initial state.");
    }

    private void Start() => InitPartitions();

    // ----------------------------------------------------------------
    //  UI refresh
    // ----------------------------------------------------------------

    private void RefreshUI()
    {
        for (int i = 0; i < driveButtons.Length; i++)
        {
            bool visible = i < partitions.Count;
            driveButtons[i].gameObject.SetActive(visible);
            if (!visible) continue;

            if (driveButtonTexts[i] != null)
                driveButtonTexts[i].text = partitions[i].GetButtonLabel();

            // Blue tint on selected, white otherwise
            var img = driveButtons[i].GetComponent<Image>();
            if (img != null)
                img.color = (i == selectedIndex)
                    ? new Color(0.25f, 0.55f, 1f, 0.75f)
                    : Color.white;
        }

        if (selectedIndex >= partitions.Count) selectedIndex = -1;

        UpdateEditorButtons();
        UpdateValidatorText();
    }

    private void UpdateEditorButtons()
    {
        bool hasSelection      = selectedIndex >= 0;
        bool unallocSelected   = hasSelection && partitions[selectedIndex].isUnallocated;
        bool partitionSelected = hasSelection && !unallocSelected;
        bool isPrimary         = partitionSelected && partitions[selectedIndex].partitionType == "Primary";

        SetInteractable(deleteButton,  partitionSelected);
        SetInteractable(newButton,     unallocSelected);
        SetInteractable(formatButton,  partitionSelected);
        SetInteractable(nextButton,    isPrimary);
    }

    private void UpdateValidatorText()
    {
        if (validatorText == null) return;

        if (selectedIndex < 0)
        {
            validatorText.text = "Select a location for your Windows installation.";
            return;
        }

        DrivePartition p = partitions[selectedIndex];

        if (p.isUnallocated)
            validatorText.text = "Windows cannot be installed on Unallocated Space.\nClick New to create a partition first.";
        else if (p.partitionType == "Primary")
            validatorText.text = $"Windows will be installed on: {p.partitionName}";
        else
            validatorText.text = $"Windows cannot be installed on a {p.partitionType} partition.";
    }

    private static void SetInteractable(Button btn, bool value)
    {
        if (btn != null) btn.interactable = value;
    }

    private void CloseAllPanels()
    {
        editorPopUp?.SetActive(false);
        popupDeletePanel?.SetActive(false);
        popupNewPanel?.SetActive(false);
        newDriveSizePanel?.SetActive(false);
    }

    // ----------------------------------------------------------------
    //  Drive button selection
    // ----------------------------------------------------------------

    public void OnDrive0Selected() => SelectDrive(0);
    public void OnDrive1Selected() => SelectDrive(1);
    public void OnDrive2Selected() => SelectDrive(2);
    public void OnDrive3Selected() => SelectDrive(3);

    private void SelectDrive(int index)
    {
        if (index < 0 || index >= partitions.Count) return;
        selectedIndex = index;
        newDriveSizePanel?.SetActive(false);
        RefreshUI();
        Debug.Log($"[PartitionManager] Selected: {partitions[index].partitionName}");
    }

    // ----------------------------------------------------------------
    //  Delete flow
    // ----------------------------------------------------------------

    // Wired to: InstallationEditor > Delete > OnClick
    public void OnDeleteClicked()
    {
        if (selectedIndex < 0 || partitions[selectedIndex].isUnallocated) return;

        editorPopUp?.SetActive(true);
        popupDeletePanel?.SetActive(true);
        popupNewPanel?.SetActive(false);

        Debug.Log($"[PartitionManager] Delete popup for: {partitions[selectedIndex].partitionName}");
    }

    // Wired to: EditorPopUp > Delete > Proceed > OnClick
    public void OnDeletePopupProceed()
    {
        CloseAllPanels();
        ExecuteDelete();
    }

    // Wired to: EditorPopUp > Delete > Return > OnClick
    public void OnDeletePopupReturn()
    {
        CloseAllPanels();
        Debug.Log("[PartitionManager] Delete cancelled.");
    }

    private void ExecuteDelete()
    {
        if (selectedIndex < 0 || selectedIndex >= partitions.Count) return;

        DrivePartition deleted = partitions[selectedIndex];
        Debug.Log($"[PartitionManager] Deleted: {deleted.partitionName} ({DrivePartition.FormatSize(deleted.totalSizeMB)})");

        partitions.RemoveAt(selectedIndex);

        int unallocIdx = partitions.FindIndex(p => p.isUnallocated);
        if (unallocIdx >= 0)
        {
            partitions[unallocIdx].totalSizeMB += deleted.totalSizeMB;
            partitions[unallocIdx].freeSpaceMB += deleted.totalSizeMB;
        }
        else
        {
            int at = Mathf.Min(selectedIndex, partitions.Count);
            partitions.Insert(at, new DrivePartition(
                "Drive 0 Unallocated Space",
                deleted.totalSizeMB, deleted.totalSizeMB, "", true));
        }

        selectedIndex = -1;
        RefreshUI();
    }

    // ----------------------------------------------------------------
    //  New partition flow
    // ----------------------------------------------------------------

    // Wired to: InstallationEditor > New > OnClick
    public void OnNewClicked()
    {
        if (selectedIndex < 0 || !partitions[selectedIndex].isUnallocated) return;

        float maxMB = partitions[selectedIndex].totalSizeMB;
        sizeField.text = Mathf.RoundToInt(maxMB).ToString();
        newDriveSizePanel?.SetActive(true);

        Debug.Log($"[PartitionManager] NewDriveSize panel opened. Max: {DrivePartition.FormatSize(maxMB)}");
    }

    // Wired to: NewDriveSize > SizeUp > OnClick
    public void OnSizeUp()
    {
        if (!int.TryParse(sizeField.text, out int current)) current = MinNewSizeMB;
        sizeField.text = Mathf.Clamp(current + SizeStepMB, MinNewSizeMB, GetMaxNewSizeMB()).ToString();
    }

    // Wired to: NewDriveSize > SizeDown > OnClick
    public void OnSizeDown()
    {
        if (!int.TryParse(sizeField.text, out int current)) current = MinNewSizeMB;
        sizeField.text = Mathf.Clamp(current - SizeStepMB, MinNewSizeMB, GetMaxNewSizeMB()).ToString();
    }

    private int GetMaxNewSizeMB()
    {
        if (selectedIndex < 0 || selectedIndex >= partitions.Count) return MinNewSizeMB;
        return Mathf.RoundToInt(partitions[selectedIndex].totalSizeMB);
    }

    // Wired to: NewDriveSize > Apply > OnClick
    // Validates, stores size, closes NewDriveSize, opens New confirmation popup.
    public void OnApplyClicked()
    {
        if (selectedIndex < 0 || !partitions[selectedIndex].isUnallocated) return;
        if (!int.TryParse(sizeField.text, out int sizeMB)) return;

        sizeMB = Mathf.Clamp(sizeMB, MinNewSizeMB, GetMaxNewSizeMB());

        // Minimum usable primary after overhead
        if (sizeMB <= TotalOverheadMB + MinNewSizeMB)
        {
            Debug.LogWarning("[PartitionManager] Size too small — must exceed overhead + 1 GB.");
            return;
        }

        pendingNewSizeMB = sizeMB;
        newDriveSizePanel?.SetActive(false);

        // Open confirmation popup with New sub-panel
        editorPopUp?.SetActive(true);
        popupDeletePanel?.SetActive(false);
        popupNewPanel?.SetActive(true);

        Debug.Log($"[PartitionManager] New partition confirmation popup — pending size: {DrivePartition.FormatSize(sizeMB)}");
    }

    // Wired to: NewDriveSize > Cancel > OnClick
    public void OnCancelNewPartition()
    {
        newDriveSizePanel?.SetActive(false);
        Debug.Log("[PartitionManager] New partition cancelled.");
    }

    // Wired to: EditorPopUp > New > Proceed > OnClick
    public void OnNewPopupProceed()
    {
        CloseAllPanels();
        ExecuteNewPartition();
    }

    // Wired to: EditorPopUp > New > Return > OnClick
    public void OnNewPopupReturn()
    {
        CloseAllPanels();
        pendingNewSizeMB = 0f;
        Debug.Log("[PartitionManager] New partition confirmation cancelled.");
    }

    // Creates System + MBR + Primary partitions from pendingNewSizeMB,
    // mirroring the Windows Setup auto-partition behaviour.
    private void ExecuteNewPartition()
    {
        if (selectedIndex < 0 || selectedIndex >= partitions.Count || pendingNewSizeMB <= 0f) return;
        if (!partitions[selectedIndex].isUnallocated) return;

        float requestedMB   = pendingNewSizeMB;
        float remainingMB   = partitions[selectedIndex].totalSizeMB - requestedMB;
        int   insertAt      = selectedIndex;

        // Base partition number on how many named partitions already exist
        int baseNum = partitions.FindAll(p => !p.isUnallocated).Count;

        string systemName  = $"Drive 0 Partition {baseNum + 1}";
        string mbrName     = $"Drive 0 Partition {baseNum + 2}";
        string primaryName = $"Drive 0 Partition {baseNum + 3}";

        float primaryMB = requestedMB - TotalOverheadMB;

        DrivePartition systemPart  = new DrivePartition(systemName,  SystemOverheadMB, SystemOverheadMB * 0.70f, "System");
        DrivePartition mbrPart     = new DrivePartition(mbrName,     MbrOverheadMB,    MbrOverheadMB    * 0.90f, "MBR");
        DrivePartition primaryPart = new DrivePartition(primaryName, primaryMB,         primaryMB        * 0.95f, "Primary");

        // Remove the unallocated entry being consumed
        partitions.RemoveAt(selectedIndex);

        // If leftover space is ≥ 1 GB, keep it as an Unallocated entry after the new partitions
        if (remainingMB >= MinNewSizeMB)
        {
            partitions.Insert(insertAt, new DrivePartition(
                "Drive 0 Unallocated Space", remainingMB, remainingMB, "", true));
        }

        // Insert new partitions in reverse order at insertAt so they end up in order
        partitions.Insert(insertAt, primaryPart);
        partitions.Insert(insertAt, mbrPart);
        partitions.Insert(insertAt, systemPart);

        selectedIndex    = -1;
        pendingNewSizeMB = 0f;
        RefreshUI();

        Debug.Log($"[PartitionManager] Created: {systemName} (System {DrivePartition.FormatSize(SystemOverheadMB)}), " +
                  $"{mbrName} (MBR {DrivePartition.FormatSize(MbrOverheadMB)}), " +
                  $"{primaryName} (Primary {DrivePartition.FormatSize(primaryMB)})");
    }

    // ----------------------------------------------------------------
    //  Format / Refresh stubs
    // ----------------------------------------------------------------

    // Wired to: InstallationEditor > Refresh > OnClick
    public void OnRefreshClicked()
    {
        Debug.Log("[PartitionManager] Refresh — redrawing partition list.");
        RefreshUI();
    }

    // Wired to: InstallationEditor > Format > OnClick
    public void OnFormatClicked()
    {
        // TBD — same EditorPopUp pattern with a Format sub-panel if needed
        Debug.Log("[PartitionManager] Format — not yet implemented.");
    }

    // ----------------------------------------------------------------
    //  Next / Install
    // ----------------------------------------------------------------

    // Wired to: Footer > Next > OnClick
    public void OnNextClicked()
    {
        if (selectedIndex < 0 || partitions[selectedIndex].partitionType != "Primary")
        {
            Debug.LogWarning("[PartitionManager] Next blocked — no Primary partition selected.");
            return;
        }

        Debug.Log($"[PartitionManager] Installing Windows on: {partitions[selectedIndex].partitionName}");
        navigator?.OnEnterFourthPhase();
    }
}
