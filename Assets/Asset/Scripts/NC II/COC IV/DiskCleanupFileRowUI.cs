/*
 * ================================================================
 *  UNITY SETUP GUIDE — DiskCleanupFileRowUI (COC IV)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to the root of the file row prefab.
 *    Also add a Button component to the root.
 *    Save as a prefab — assign to DiskCleanupController.fileRowPrefab.
 *
 *  HIERARCHY (row prefab)
 *    File Row Root               ← this script + Button component here
 *      ├── CheckToggle           → checkToggle  (Toggle — Interactable: false)
 *      ├── Icon                  → iconImage    (Image)
 *      ├── NameTMP               → nameTMP      (TMP_Text)
 *      └── SizeTMP               → sizeTMP      (TMP_Text, right-aligned)
 *
 *  INSPECTOR ASSIGNMENTS
 *    checkToggle   Toggle on the CheckToggle child (leave Interactable = false)
 *    iconImage     Image on the Icon child
 *    nameTMP       TMP_Text for the category name
 *    sizeTMP       TMP_Text for the file size
 *    rowButton     Button component on this root GameObject
 *
 *  HOW IT WORKS
 *    DiskCleanupController.ShowFileList() instantiates this prefab per category.
 *    Clicking anywhere on the row (via the root Button) toggles the checkbox
 *    and fires two callbacks: onToggleChanged (controller recalculates total)
 *    and onSelected (controller updates the description TMP at the bottom).
 *    The Toggle is purely visual — it is driven by the root Button click.
 * ================================================================
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiskCleanupFileRowUI : MonoBehaviour
{
    [SerializeField] private Toggle   checkToggle;
    [SerializeField] private Image    iconImage;
    [SerializeField] private TMP_Text nameTMP;
    [SerializeField] private TMP_Text sizeTMP;
    [SerializeField] private Button   rowButton;

    public DiskCleanupFileEntry Entry { get; private set; }
    public bool IsChecked => checkToggle != null && checkToggle.isOn;

    private Action<DiskCleanupFileRowUI> _onToggleChanged;
    private Action<DiskCleanupFileRowUI> _onSelected;

    public void Setup(
        DiskCleanupFileEntry entry,
        Sprite               icon,
        Action<DiskCleanupFileRowUI> onToggleChanged,
        Action<DiskCleanupFileRowUI> onSelected)
    {
        Entry            = entry;
        _onToggleChanged = onToggleChanged;
        _onSelected      = onSelected;

        if (nameTMP != null) nameTMP.text = entry.Name;
        if (sizeTMP != null) sizeTMP.text = FormatSize(entry.SizeMB);
        if (iconImage != null && icon != null) iconImage.sprite = icon;

        if (checkToggle != null)
        {
            checkToggle.interactable = false;
            checkToggle.SetIsOnWithoutNotify(entry.DefaultChecked);
        }

        rowButton?.onClick.RemoveAllListeners();
        rowButton?.onClick.AddListener(OnRowClicked);
    }

    private void OnRowClicked()
    {
        if (checkToggle != null)
            checkToggle.SetIsOnWithoutNotify(!checkToggle.isOn);

        _onToggleChanged?.Invoke(this);
        _onSelected?.Invoke(this);
    }

    private static string FormatSize(float mb)
    {
        if (mb == 0f)     return "0 KB";
        if (mb < 1f)      return $"{mb * 1024f:F0} KB";
        if (mb >= 1024f)  return $"{mb / 1024f:F2} GB";
        return $"{mb:F2} MB";
    }
}
