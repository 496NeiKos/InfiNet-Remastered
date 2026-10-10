/*
 * ================================================================
 *  UNITY SETUP GUIDE — DefragDriveRowUI (COC IV)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to the root of the drive row prefab.
 *    Also add a Button component to the root.
 *    Save as a prefab — assign to DefragController.driveRowPrefab.
 *
 *  HIERARCHY (row prefab)
 *    Drive Row Root              ← this script + Button component here
 *      ├── Background            → background       (Image — fills the row)
 *      ├── DriveTMP              → driveTMP         (TMP_Text — e.g. "C: (Windows)")
 *      ├── MediaTypeTMP          → mediaTypeTMP     (TMP_Text — e.g. "Solid State Drive")
 *      ├── LastRunTMP            → lastRunTMP       (TMP_Text — e.g. "10/7/2026 2:14 AM")
 *      └── CurrentStatusTMP      → currentStatusTMP (TMP_Text — e.g. "OK (0 days since last run)")
 *
 *  INSPECTOR ASSIGNMENTS
 *    background        Image on Background child
 *    driveTMP          TMP_Text for the Drive column
 *    mediaTypeTMP      TMP_Text for the Media type column
 *    lastRunTMP        TMP_Text for the Last run column
 *    currentStatusTMP  TMP_Text for the Current status column
 *    normalColor       White or light grey (unselected state)
 *    selectedColor     Blue highlight (e.g. #3478F6)
 *    rowButton         Button component on this root GameObject
 *
 *  HOW IT WORKS
 *    DefragController.PopulateDriveList() instantiates this prefab per drive.
 *    Clicking a row selects it (highlight) and enables the Analyze/Optimize buttons.
 *    UpdateStatus() and UpdateLastRun() are called by the controller during and
 *    after operations to reflect live progress and final results.
 * ================================================================
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DefragDriveRowUI : MonoBehaviour
{
    [SerializeField] private Image    background;
    [SerializeField] private TMP_Text driveTMP;
    [SerializeField] private TMP_Text mediaTypeTMP;
    [SerializeField] private TMP_Text lastRunTMP;
    [SerializeField] private TMP_Text currentStatusTMP;
    [SerializeField] private Color    normalColor   = Color.white;
    [SerializeField] private Color    selectedColor = new Color(0.204f, 0.471f, 0.965f, 1f);
    [SerializeField] private Button   rowButton;

    public DefragDriveEntry Entry { get; private set; }

    public void Setup(DefragDriveEntry entry, Action<DefragDriveRowUI> onSelected)
    {
        Entry = entry;

        if (driveTMP         != null) driveTMP.text         = entry.DriveDisplay;
        if (mediaTypeTMP     != null) mediaTypeTMP.text     = entry.MediaType;
        if (lastRunTMP       != null) lastRunTMP.text       = entry.LastRun;
        if (currentStatusTMP != null) currentStatusTMP.text = entry.CurrentStatus;

        SetSelected(false);

        rowButton?.onClick.RemoveAllListeners();
        rowButton?.onClick.AddListener(() => onSelected?.Invoke(this));
    }

    public void SetSelected(bool selected)
    {
        if (background != null)
            background.color = selected ? selectedColor : normalColor;
    }

    public void UpdateStatus(string status)
    {
        if (currentStatusTMP != null) currentStatusTMP.text = status;
    }

    public void UpdateLastRun(string lastRun)
    {
        if (lastRunTMP != null) lastRunTMP.text = lastRun;
    }
}
