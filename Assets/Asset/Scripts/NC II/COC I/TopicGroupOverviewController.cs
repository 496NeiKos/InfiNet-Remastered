/*
 * ================================================================
 *  UNITY SETUP GUIDE — TopicGroupOverviewController (COC I)
 * ================================================================
 *  PURPOSE
 *    Owns the single shared Group Overview Popup that sits on the
 *    persistent canvas. Keeps the popup in sync with whichever topic
 *    is currently active so it always shows the right groups
 *    (Pending / Active / Done) without closing and reopening.
 *
 *  COMPONENT PLACEMENT
 *    Add this script to the persistent-canvas GameObject that is the
 *    parent (or sibling) of the shared Group Overview Popup panel.
 *    It must NOT be inside any per-topic task container.
 *
 *  INSPECTOR ASSIGNMENTS
 *    groupOverviewPopup  → the shared popup panel GameObject
 *    topic1Manager       → NCIITaskListManager in the scene
 *    topic2Manager       → T2TaskListManager  in the scene
 *    topic3Manager       → T3TaskListManager  in the scene
 *
 *  HOW IT WORKS
 *    • Subscribes to TopicManager.OnTopicSwitched.
 *    • If the popup is open when a topic switch fires, it immediately
 *      calls RebuildOverview() on the newly active topic's manager,
 *      which destroys the old rows and rebuilds them with current data.
 *    • If the popup is closed when a switch fires, nothing happens —
 *      the next time a group-title button is clicked it will open with
 *      the correct topic's data via each manager's own OpenPopup path.
 *    • CloseOverviewPopup() is public so TopicManager.SwitchToTopic()
 *      can call it directly as a fallback safety close.
 * ================================================================
 */

using UnityEngine;

public class TopicGroupOverviewController : MonoBehaviour
{
    public static TopicGroupOverviewController Instance { get; private set; }

    [Header("Shared Overview Popup")]
    [SerializeField] private GameObject groupOverviewPopup;

    [Header("Task Managers")]
    [SerializeField] private NCIITaskListManager topic1Manager;
    [SerializeField] private T2TaskListManager   topic2Manager;
    [SerializeField] private T3TaskListManager   topic3Manager;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        TopicManager.OnTopicSwitched += OnTopicSwitched;
    }

    private void OnDisable()
    {
        TopicManager.OnTopicSwitched -= OnTopicSwitched;
    }

    private void OnTopicSwitched(int index)
    {
        if (groupOverviewPopup == null || !groupOverviewPopup.activeSelf)
            return;

        // Popup is open — rebuild its rows so it reflects the new topic's groups.
        RebuildForTopic(index);
    }

    private void RebuildForTopic(int index)
    {
        switch (index)
        {
            case 0: topic1Manager?.RebuildOverview(); break;
            case 1: topic2Manager?.RebuildOverview(); break;
            case 2: topic3Manager?.RebuildOverview(); break;
        }
    }

    public void CloseOverviewPopup()
    {
        if (groupOverviewPopup != null)
            groupOverviewPopup.SetActive(false);
    }
}
