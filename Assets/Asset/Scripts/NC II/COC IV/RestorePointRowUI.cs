/*
 * ================================================================
 *  UNITY SETUP GUIDE — RestorePointRowUI (COC IV)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to the root of the restore point row prefab.
 *    Save the prefab to Assets/Asset/Prefabs/ (or equivalent).
 *
 *  HIERARCHY (row prefab)
 *    Row Root                    ← this script + Button component here
 *      ├── Background            → background (Image, fills the row)
 *      ├── DateTimeTMP           → dateTimeTMP
 *      ├── DescriptionTMP        → descriptionTMP
 *      └── TypeTMP               → typeTMP
 *
 *  INSPECTOR ASSIGNMENTS
 *    background     Image on Background child
 *    dateTimeTMP    TMP_Text for the date/time column
 *    descriptionTMP TMP_Text for the description column
 *    typeTMP        TMP_Text for the type column (always "Manual")
 *    normalColor    White or light grey
 *    selectedColor  Blue highlight (e.g. #3478F6)
 *    rowButton      Button component on the root GO itself
 *
 *  HOW IT WORKS
 *    SystemProtectionController.ShowPhase2() instantiates this prefab
 *    for each RestorePointEntry, then calls Setup() to populate it.
 *    Clicking the row calls the onSelected callback back to the
 *    controller, which clears all other selections and highlights
 *    this row.
 * ================================================================
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RestorePointRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text dateTimeTMP;
    [SerializeField] private TMP_Text descriptionTMP;
    [SerializeField] private TMP_Text typeTMP;
    [SerializeField] private Image    background;
    [SerializeField] private Color    normalColor   = Color.white;
    [SerializeField] private Color    selectedColor = new Color(0.204f, 0.471f, 0.965f, 1f);
    [SerializeField] private Button   rowButton;

    public RestorePointEntry Entry { get; private set; }

    public void Setup(RestorePointEntry entry, Action<RestorePointRowUI> onSelected)
    {
        Entry = entry;
        if (dateTimeTMP   != null) dateTimeTMP.text   = entry.DateTime;
        if (descriptionTMP != null) descriptionTMP.text = entry.Name;
        if (typeTMP        != null) typeTMP.text        = entry.Type;

        SetSelected(false);

        rowButton?.onClick.RemoveAllListeners();
        rowButton?.onClick.AddListener(() => onSelected?.Invoke(this));
    }

    public void SetSelected(bool selected)
    {
        if (background != null)
            background.color = selected ? selectedColor : normalColor;
    }
}
