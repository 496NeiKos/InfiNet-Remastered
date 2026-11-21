/*
 * ================================================================
 *  ScoreOverviewPanelController  — lesson selection all-COC panel
 * ================================================================
 *  PLACEMENT
 *    Add to a panel GameObject in the LessonSelection scene canvas.
 *    Panel starts INACTIVE.
 *    Wire a "Score Overview" button in LessonSelection UI
 *    → LessonSelectionController.OpenScoreOverview()
 *
 *  HIERARCHY
 *    ScoreOverviewPanel  (starts inactive)
 *      ├── CloseButton                → OnClick: Close()
 *      ├── COC1Section
 *      │     ├── DifficultyLabel      TMP_Text  e.g. "Normal" / "Hard" / "—"
 *      │     ├── PerformanceValue     TMP_Text
 *      │     ├── AccuracyValue        TMP_Text
 *      │     ├── TimeDurationValue    TMP_Text
 *      │     ├── QuizValue            TMP_Text
 *      │     ├── TotalValue           TMP_Text
 *      │     └── NotPlayedLabel       TMP_Text  "Not played yet"
 *      ├── COC2Section  (same child structure)
 *      ├── COC3Section  (same child structure)
 *      └── COC4Section  (same child structure)
 *
 *  INSPECTOR
 *    Assign the 4 COCSectionData entries in the cocSections array
 *    by dragging in each section's TMP children.
 * ================================================================
 */

using System;
using TMPro;
using UnityEngine;

public class ScoreOverviewPanelController : MonoBehaviour
{
    public static ScoreOverviewPanelController Instance { get; private set; }

    [Serializable]
    public class COCSectionData
    {
        public TMP_Text difficultyLabel;
        public TMP_Text performanceValue;
        public TMP_Text accuracyValue;
        public TMP_Text timeDurationValue;
        public TMP_Text totalValue;
        public TMP_Text notPlayedLabel;
    }

    [SerializeField] private GameObject    panel;
    [SerializeField] private COCSectionData[] cocSections = new COCSectionData[4];

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Open()
    {
        Refresh();
        panel.SetActive(true);
    }

    public void Close()
    {
        panel.SetActive(false);
    }

    private void Refresh()
    {
        var mgr = COCScoreManager.Instance;
        if (mgr == null) return;

        for (int i = 0; i < 4; i++)
        {
            var section = cocSections[i];
            if (section == null) continue;

            COCScoreData data = mgr.GetSavedScore(i);

            if (!data.isPlayed)
            {
                SetText(section.difficultyLabel,   "—");
                SetText(section.performanceValue,  "0");
                SetText(section.accuracyValue,     "0");
                SetText(section.timeDurationValue, "0");
                SetText(section.totalValue,        "0");
                if (section.notPlayedLabel != null)
                    section.notPlayedLabel.gameObject.SetActive(true);
            }
            else
            {
                bool hard = data.difficulty == Difficulty.Hard;

                SetText(section.difficultyLabel,   data.difficulty.ToString());
                SetText(section.performanceValue,  $"{data.performancePts:F1}");
                SetText(section.accuracyValue,     $"{data.accuracyPts:F1}");
                SetText(section.timeDurationValue, hard ? $"{data.timePts:F1}" : "N/A");
                SetText(section.totalValue,        $"{data.totalPts:F1}");
                if (section.notPlayedLabel != null)
                    section.notPlayedLabel.gameObject.SetActive(false);
            }
        }
    }

    private static void SetText(TMP_Text tmp, string value)
    {
        if (tmp != null) tmp.text = value;
    }
}
