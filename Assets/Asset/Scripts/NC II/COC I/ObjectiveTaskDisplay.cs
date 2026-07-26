/*
 * ================================================================
 *  UNITY SETUP GUIDE — ObjectiveTaskDisplay
 * ================================================================
 *  PURPOSE
 *    Always-visible HUD text that shows the current high-level
 *    objective. Unlike SingleTaskDisplay (task-granularity,
 *    editor-gated), this stays on screen at all times and advances
 *    only when enough tasks have been completed to satisfy an
 *    objective. Objectives never revert — the index is latched
 *    per topic as a high-water mark.
 *
 *    Two special states are also handled:
 *      • Disassembly→Assembly transition (Topic 1 only):
 *        shows a configurable transition message for the same
 *        duration as the NCIITaskListManager transition window.
 *      • All objectives complete: shows a configurable completion
 *        message. This state is also latched (never reverts).
 *
 *  STEP 1 — Create the canvas
 *    a) New empty GameObject: "ObjectiveTaskDisplayCanvas"
 *    b) Add component: Canvas
 *         Render Mode: Screen Space - Overlay
 *         Sort Order: 99  (below SingleTaskDisplay at 100)
 *    c) Add component: CanvasScaler
 *    d) Add component: ObjectiveTaskDisplay  ← THIS SCRIPT
 *
 *  STEP 2 — Create the text child
 *    a) Child TMP - Text (UI): "ObjectiveText"
 *         Position and style as desired (e.g. top-centre banner).
 *
 *  STEP 3 — Wire inspector fields
 *    objectiveText               → ObjectiveText (TMP component)
 *    topic1Manager               → NCIITaskListManager in the scene
 *    topic2Manager               → T2TaskListManager  in the scene
 *    topic3Manager               → T3TaskListManager  in the scene
 *    disassemblyTransitionText   → message shown during D→A transition
 *    allObjectivesCompletedText  → message shown when all objectives done
 *
 *  STEP 4 — Define objectives per topic
 *    Expand each "Topic N Objectives" array. Each entry has:
 *      text          — the objective label to display
 *      tasksRequired — how many sequential tasks this objective
 *                      consumes before the next one activates
 *
 *    Example for Topic 1 (30 tasks total):
 *      [0] "Prepare the workstation"         tasksRequired = 3
 *      [1] "Disassemble internal components" tasksRequired = 13
 *      [2] "Reassemble the system"           tasksRequired = 14
 *
 *    The display advances to objective[1] when 3 tasks are done,
 *    to objective[2] when 16 tasks are done, and shows the
 *    completion message when all 30 tasks are done.
 * ================================================================
 */

using System;
using TMPro;
using UnityEngine;

public class ObjectiveTaskDisplay : MonoBehaviour
{
    [Serializable]
    public class ObjectiveEntry
    {
        [Tooltip("Text shown in the objective display while this objective is active.")]
        public string text;
        [Tooltip("Number of sequential tasks that must complete to finish this objective.")]
        public int tasksRequired;
    }

    [Header("Display")]
    [SerializeField] private GameObject displayPanel;
    [SerializeField] private TMP_Text objectiveText;
    [Tooltip("When enabled, hides the display while a detail panel is open — opposite of SingleTaskDisplay.")]
    [SerializeField] private bool hideWhenEditorOpen;

    [Header("Task Manager References")]
    [SerializeField] private NCIITaskListManager topic1Manager;
    [SerializeField] private T2TaskListManager   topic2Manager;
    [SerializeField] private T3TaskListManager   topic3Manager;

    [Header("Special Messages")]
    [SerializeField] private string disassemblyTransitionText  = "Disassembly objective completed, Now transitioning to assembly.";
    [SerializeField] private string allObjectivesCompletedText = "All Objective Completed!";

    [Header("Topic 1 Objectives")]
    [SerializeField] private ObjectiveEntry[] topic1Objectives;

    [Header("Topic 2 Objectives")]
    [SerializeField] private ObjectiveEntry[] topic2Objectives;

    [Header("Topic 3 Objectives")]
    [SerializeField] private ObjectiveEntry[] topic3Objectives;

    private bool _wasEditorOpen;

    // Per-topic latched objective indices — only ever increase.
    private int _t1Index;
    private int _t2Index;
    private int _t3Index;

    // Per-topic completion latch — flips true once, never resets.
    private bool _t1AllDone;
    private bool _t2AllDone;
    private bool _t3AllDone;

    private void Start()
    {
        NCIITaskListManager.OnTasksUpdated += Refresh;
        T2TaskListManager.OnTasksUpdated   += Refresh;
        T3TaskListManager.OnTasksUpdated   += Refresh;

        if (hideWhenEditorOpen && objectiveText != null)
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

        if (objectiveText != null)
            displayPanel?.SetActive(!isOpen);
    }

    private void OnDestroy()
    {
        NCIITaskListManager.OnTasksUpdated -= Refresh;
        T2TaskListManager.OnTasksUpdated   -= Refresh;
        T3TaskListManager.OnTasksUpdated   -= Refresh;
    }

    private void Refresh()
    {
        if (topic1Manager != null && topic1Manager.gameObject.activeInHierarchy)
        {
            if (topic1Manager.IsTransitioningToAssembly)
            {
                if (objectiveText != null)
                    objectiveText.text = disassemblyTransitionText;
                return;
            }
            RefreshForTopic(topic1Objectives, topic1Manager.GetCompletedTaskCount(), ref _t1Index, ref _t1AllDone);
        }
        else if (topic2Manager != null && topic2Manager.gameObject.activeInHierarchy)
            RefreshForTopic(topic2Objectives, topic2Manager.GetCompletedTaskCount(), ref _t2Index, ref _t2AllDone);
        else if (topic3Manager != null && topic3Manager.gameObject.activeInHierarchy)
            RefreshForTopic(topic3Objectives, topic3Manager.GetCompletedTaskCount(), ref _t3Index, ref _t3AllDone);
    }

    private void RefreshForTopic(ObjectiveEntry[] objectives, int completedCount, ref int latchedIndex, ref bool allDone)
    {
        if (objectives == null || objectives.Length == 0) return;

        if (!allDone)
        {
            int total = 0;
            foreach (var o in objectives) total += o.tasksRequired;
            if (completedCount >= total)
                allDone = true;
        }

        if (allDone)
        {
            if (objectiveText != null)
                objectiveText.text = allObjectivesCompletedText;
            return;
        }

        int naturalIndex = ResolveObjectiveIndex(objectives, completedCount);
        latchedIndex = Mathf.Max(latchedIndex, naturalIndex);

        if (objectiveText != null && latchedIndex < objectives.Length)
            objectiveText.text = objectives[latchedIndex].text;
    }

    // Walks objectives cumulatively and returns the index of the first objective
    // whose cumulative task threshold has not yet been reached.
    private static int ResolveObjectiveIndex(ObjectiveEntry[] objectives, int completedCount)
    {
        int cumulative = 0;
        for (int i = 0; i < objectives.Length; i++)
        {
            cumulative += objectives[i].tasksRequired;
            if (completedCount < cumulative)
                return i;
        }
        return objectives.Length - 1;
    }
}
