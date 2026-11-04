/*
 * ================================================================
 *  UNITY SETUP GUIDE — ObjectiveTaskDisplayCOCIV
 * ================================================================
 *  PURPOSE
 *    Always-visible HUD banner showing the current high-level
 *    objective for COC IV. Single manager (COCIVMaintenanceTaskManager).
 *    Subscribes to COCIVMaintenanceTaskManager.OnTasksUpdated.
 *
 *    Fires ObjectiveTaskDisplay.FireGroupComplete() when a group
 *    finishes so TopicCompletePopUp works with no changes.
 *
 *  STEP 1 — Create the canvas
 *    a) New empty GameObject: "ObjectiveTaskDisplayCanvas"
 *    b) Add component: Canvas
 *         Render Mode: Screen Space - Overlay
 *         Sort Order: 99
 *    c) Add component: CanvasScaler
 *    d) Add component: ObjectiveTaskDisplayCOCIV  ← THIS SCRIPT
 *
 *  STEP 2 — Create the text child
 *    a) Child TMP - Text (UI): "ObjectiveText"
 *
 *  STEP 3 — Wire inspector fields
 *    displayPanel   → the canvas root (or a child panel)
 *    objectiveText  → ObjectiveText (TMP component)
 *    taskManager    → COCIVMaintenanceTaskManager in the scene
 *
 *  STEP 4 — Define objectives
 *    Expand the "Objectives" array. Each entry:
 *      text          — objective label to display
 *      tasksRequired — sequential tasks this objective consumes
 *
 *    COCIVMaintenanceTaskManager has 33 tasks total across 6 groups:
 *      Group 0: System Protection — Create Restore Point  (tasks 0–5,   6 tasks)
 *      Group 1: System Protection — Built-in Restore      (tasks 6–9,   4 tasks)
 *      Group 2: Disk Cleanup                              (tasks 10–16, 7 tasks)
 *      Group 3: Defragment and Optimize Drives            (tasks 17–20, 4 tasks)
 *      Group 4: Windows Security                          (tasks 21–25, 5 tasks)
 *      Group 5: System Restore via Run Dialog             (tasks 26–32, 7 tasks)
 *
 *    Map tasksRequired to the task counts above, e.g.:
 *      [0] "System Protection — Create Restore Point"  tasksRequired = 6
 *      [1] "System Protection — Built-in Restore"      tasksRequired = 4
 *      [2] "Disk Cleanup"                              tasksRequired = 7
 *      ... etc.
 *
 *  STEP 5 — Add TopicCompletePopUp to the scene
 *    Place TopicCompletePopUp component in the scene. Set
 *    bodyTemplate to e.g. " has been completed, proceed to the
 *    next objective." in its inspector.
 * ================================================================
 */

using System;
using TMPro;
using UnityEngine;

public class ObjectiveTaskDisplayCOCIV : MonoBehaviour
{
    [Serializable]
    public class ObjectiveEntry
    {
        [Tooltip("Text shown while this objective is active.")]
        public string text;
        [Tooltip("Number of sequential tasks that must complete to finish this objective.")]
        public int tasksRequired;
    }

    [Header("Display")]
    [SerializeField] private GameObject displayPanel;
    [SerializeField] private TMP_Text   objectiveText;
    [Tooltip("Hides the display while a detail panel is open.")]
    [SerializeField] private bool hideWhenEditorOpen;

    [Header("Manager Reference")]
    [SerializeField] private COCIVMaintenanceTaskManager taskManager;

    [Header("Special Messages")]
    [SerializeField] private string allObjectivesCompletedText = "All Objectives Completed!";

    [Header("Objectives")]
    [SerializeField] private ObjectiveEntry[] objectives;

    private bool _wasEditorOpen;
    private int  _latchedIndex;
    private bool _allDone;

    private void Start()
    {
        COCIVMaintenanceTaskManager.OnTasksUpdated += Refresh;

        if (hideWhenEditorOpen)
        {
            bool isOpen = GameManager.Instance != null && GameManager.Instance.IsEditorOpen;
            _wasEditorOpen = isOpen;
            displayPanel?.SetActive(!isOpen);
        }

        Refresh();
    }

    private void Update()
    {
        if (!hideWhenEditorOpen) return;

        bool isOpen = GameManager.Instance != null && GameManager.Instance.IsEditorOpen;
        if (isOpen == _wasEditorOpen) return;
        _wasEditorOpen = isOpen;
        displayPanel?.SetActive(!isOpen);
    }

    private void OnDestroy()
    {
        COCIVMaintenanceTaskManager.OnTasksUpdated -= Refresh;
    }

    private void Refresh()
    {
        if (taskManager == null || objectives == null || objectives.Length == 0) return;

        int completedCount = taskManager.GetCompletedTaskCount();
        bool wasAllDone    = _allDone;

        if (!_allDone)
        {
            int total = 0;
            foreach (var o in objectives) total += o.tasksRequired;
            if (completedCount >= total) _allDone = true;
        }

        if (_allDone)
        {
            if (!wasAllDone)
            {
                string completedName = _latchedIndex < objectives.Length ? objectives[_latchedIndex].text : string.Empty;
                ObjectiveTaskDisplay.FireGroupComplete(completedName);
            }
            if (objectiveText != null)
                objectiveText.text = allObjectivesCompletedText;
            return;
        }

        int naturalIndex     = ResolveObjectiveIndex(completedCount);
        int prevLatchedIndex = _latchedIndex;
        _latchedIndex = Mathf.Max(_latchedIndex, naturalIndex);

        if (_latchedIndex > prevLatchedIndex)
        {
            string completedName = prevLatchedIndex < objectives.Length ? objectives[prevLatchedIndex].text : string.Empty;
            ObjectiveTaskDisplay.FireGroupComplete(completedName);
        }

        if (objectiveText != null && _latchedIndex < objectives.Length)
            objectiveText.text = objectives[_latchedIndex].text;
    }

    private int ResolveObjectiveIndex(int completedCount)
    {
        int cumulative = 0;
        for (int i = 0; i < objectives.Length; i++)
        {
            cumulative += objectives[i].tasksRequired;
            if (completedCount < cumulative) return i;
        }
        return objectives.Length - 1;
    }
}
