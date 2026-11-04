/*
 * ================================================================
 *  UNITY SETUP GUIDE — ObjectiveTaskDisplayCOCII
 * ================================================================
 *  PURPOSE
 *    Always-visible HUD banner showing the current high-level
 *    objective for the active COC II category. Mirrors the COC I
 *    ObjectiveTaskDisplay but wired to COC II's three task managers
 *    via NetworkCableTaskCategoryController.
 *
 *    Objectives are latched per-category and never revert.
 *    Fires ObjectiveTaskDisplay.FireGroupComplete() when a group
 *    finishes so TopicCompletePopUp works with no changes.
 *
 *  STEP 1 — Create the canvas
 *    a) New empty GameObject: "ObjectiveTaskDisplayCanvas"
 *    b) Add component: Canvas
 *         Render Mode: Screen Space - Overlay
 *         Sort Order: 99
 *    c) Add component: CanvasScaler
 *    d) Add component: ObjectiveTaskDisplayCOCII  ← THIS SCRIPT
 *
 *  STEP 2 — Create the text child
 *    a) Child TMP - Text (UI): "ObjectiveText"
 *
 *  STEP 3 — Wire inspector fields
 *    displayPanel        → the canvas root (or a child panel)
 *    objectiveText       → ObjectiveText (TMP component)
 *    categoryController  → NetworkCableTaskCategoryController in the scene
 *    networkCableManager → NetworkCableTaskManager in the scene
 *    ipConfigManager     → IPConfigTaskManager in the scene
 *    pingCmdManager      → PingCmdTaskManager in the scene
 *
 *  STEP 4 — Define objectives per category
 *    Each array entry has:
 *      text          — objective label to display
 *      tasksRequired — sequential tasks this objective consumes
 *
 *    Category 0 — Network Cable (45 tasks total)
 *    Category 1 — IP Config    (74 tasks total)
 *    Category 2 — Ping CMD     (37 tasks total)
 *
 *  STEP 5 — Add TopicCompletePopUp to the scene
 *    Place a TopicCompletePopUpManager GameObject with TopicCompletePopUp
 *    component — it subscribes to ObjectiveTaskDisplay.OnObjectiveGroupComplete
 *    and works here without any changes. Set bodyTemplate in its inspector
 *    to "has been completed, proceed to other category if not yet finished."
 * ================================================================
 */

using System;
using TMPro;
using UnityEngine;

public class ObjectiveTaskDisplayCOCII : MonoBehaviour
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

    [Header("Manager References")]
    [SerializeField] private NetworkCableTaskCategoryController categoryController;
    [SerializeField] private NetworkCableTaskManager            networkCableManager;
    [SerializeField] private IPConfigTaskManager                ipConfigManager;
    [SerializeField] private PingCmdTaskManager                 pingCmdManager;

    [Header("Special Messages")]
    [SerializeField] private string allObjectivesCompletedText = "All Objectives Completed!";

    [Header("Category 0 — Network Cable Objectives")]
    [SerializeField] private ObjectiveEntry[] cat0Objectives;

    [Header("Category 1 — IP Config Objectives")]
    [SerializeField] private ObjectiveEntry[] cat1Objectives;

    [Header("Category 2 — Ping CMD Objectives")]
    [SerializeField] private ObjectiveEntry[] cat2Objectives;

    private bool _wasEditorOpen;

    // Per-category latched objective indices.
    private int _c0Index;
    private int _c1Index;
    private int _c2Index;

    // Per-category completion latch.
    private bool _c0AllDone;
    private bool _c1AllDone;
    private bool _c2AllDone;

    private void Start()
    {
        NetworkCableTaskCategoryController.OnActiveCategoryUpdated += Refresh;

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
        NetworkCableTaskCategoryController.OnActiveCategoryUpdated -= Refresh;
    }

    private void Refresh()
    {
        if (categoryController == null) return;

        switch (categoryController.ActiveCategoryIndex)
        {
            case 0:
                if (networkCableManager != null)
                    RefreshForCategory(cat0Objectives, networkCableManager.GetCompletedTaskCount(), ref _c0Index, ref _c0AllDone);
                break;
            case 1:
                if (ipConfigManager != null)
                    RefreshForCategory(cat1Objectives, ipConfigManager.GetCompletedTaskCount(), ref _c1Index, ref _c1AllDone);
                break;
            case 2:
                if (pingCmdManager != null)
                    RefreshForCategory(cat2Objectives, pingCmdManager.GetCompletedTaskCount(), ref _c2Index, ref _c2AllDone);
                break;
        }
    }

    private void RefreshForCategory(ObjectiveEntry[] objectives, int completedCount, ref int latchedIndex, ref bool allDone)
    {
        if (objectives == null || objectives.Length == 0) return;

        bool wasAllDone = allDone;

        if (!allDone)
        {
            int total = 0;
            foreach (var o in objectives) total += o.tasksRequired;
            if (completedCount >= total) allDone = true;
        }

        if (allDone)
        {
            if (!wasAllDone)
            {
                string completedName = latchedIndex < objectives.Length ? objectives[latchedIndex].text : string.Empty;
                ObjectiveTaskDisplay.FireGroupComplete(completedName);
            }
            if (objectiveText != null)
                objectiveText.text = allObjectivesCompletedText;
            return;
        }

        int naturalIndex    = ResolveObjectiveIndex(objectives, completedCount);
        int prevLatchedIndex = latchedIndex;
        latchedIndex = Mathf.Max(latchedIndex, naturalIndex);

        if (latchedIndex > prevLatchedIndex)
        {
            string completedName = prevLatchedIndex < objectives.Length ? objectives[prevLatchedIndex].text : string.Empty;
            ObjectiveTaskDisplay.FireGroupComplete(completedName);
        }

        if (objectiveText != null && latchedIndex < objectives.Length)
            objectiveText.text = objectives[latchedIndex].text;
    }

    private static int ResolveObjectiveIndex(ObjectiveEntry[] objectives, int completedCount)
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
