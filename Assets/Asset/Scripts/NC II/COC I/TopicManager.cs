using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TopicManager : MonoBehaviour
{
    public static TopicManager Instance { get; private set; }

    [System.Serializable]
    public class TopicEntry
    {
        public string topicName;

        [Tooltip("Indices of topics that must be complete before this one unlocks. Leave empty = always available.")]
        public int[] requiredTopicIndices = new int[0];

        [Header("Root Containers")]
        public GameObject worldRootContainer;
        public GameObject hardwareStorageContainer;
        public GameObject hardwareAreaContainer;
        public GameObject taskListContainer;
        public GameObject terminalContainer;
    }

    [System.Serializable]
    public class TopicTabButton
    {
        public Button button;
        public TMP_Text nameText;
    }

    [Header("Topics")]
    [SerializeField] private List<TopicEntry> topics = new();

    [Header("Access Control")]
    [Tooltip("When unchecked (default): all topics are accessible from the start.\n" +
             "When checked: Topic 2 and Topic 3 are locked until Topic 1 (NCIITaskListManager) is fully completed.")]
    [SerializeField] private bool required = false;

    [Header("Tab UI")]
    [SerializeField] private Button tabBurgerButton;
    [SerializeField] private GameObject tabDropdown;
    [SerializeField] private List<TopicTabButton> tabButtons = new();

    private int _activeTopicIndex;
    private bool _isDropdownOpen;
    private bool[] _topicComplete;

    private int _sevenPressCount;
    private float _lastSevenPressTime;
    private const float SevenPressWindow = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private static readonly Key[] TopicDigitKeys = { Key.Digit1, Key.Digit2, Key.Digit3 };

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.shiftKey.isPressed && !kb.ctrlKey.isPressed && kb[Key.Digit7].wasPressedThisFrame)
        {
            if (_sevenPressCount > 0 && Time.unscaledTime - _lastSevenPressTime > SevenPressWindow)
                _sevenPressCount = 0;

            _sevenPressCount++;
            _lastSevenPressTime = Time.unscaledTime;

            if (_sevenPressCount >= 3)
            {
                _sevenPressCount = 0;
                required = false;
                RefreshTabUI();
                Debug.Log("[TopicManager] Bypass activated — topic lock removed.");
            }
            return;
        }

        if (!kb.ctrlKey.isPressed || !kb.shiftKey.isPressed) return;

        for (int i = 0; i < TopicDigitKeys.Length; i++)
        {
            if (!kb[TopicDigitKeys[i]].wasPressedThisFrame) continue;
            SwitchToTopic(i);
            return;
        }
    }

    [ContextMenu("Debug: Unlock Topic 3")]
    public void DebugUnlockTopic3()
    {
        MarkTopicComplete(0);
        MarkTopicComplete(1);
        SwitchToTopic(2);
        Debug.Log("[TopicManager] DEBUG — Topic 3 force-unlocked.");
    }

    private void Start()
    {
        _topicComplete = new bool[topics.Count];

        if (tabBurgerButton != null)
        {
            tabBurgerButton.onClick.RemoveListener(ToggleDropdown);
            tabBurgerButton.onClick.AddListener(ToggleDropdown);
        }

        for (int i = 0; i < tabButtons.Count; i++)
        {
            int index = i;
            if (tabButtons[i].button != null)
            {
                tabButtons[i].button.onClick.RemoveAllListeners();
                tabButtons[i].button.onClick.AddListener(() => OnTabClicked(index));
                tabButtons[i].button.gameObject.SetActive(true);
            }
        }

        if (tabDropdown != null)
            tabDropdown.SetActive(false);

        NCIITaskListManager.OnTasksUpdated += RefreshTabUI;

        ActivateTopic(0);
        RefreshTabUI();
    }

    private void OnDestroy()
    {
        NCIITaskListManager.OnTasksUpdated -= RefreshTabUI;
    }

    public void ToggleDropdown()
    {
        _isDropdownOpen = !_isDropdownOpen;
        if (tabDropdown != null)
            tabDropdown.SetActive(_isDropdownOpen);

        if (_isDropdownOpen)
            RefreshTabUI();
    }

    private void OnTabClicked(int index)
    {
        if (!IsTopicUnlocked(index))
        {
            Debug.Log($"[TopicManager] Topic {index} ({topics[index].topicName}) is locked.");
            return;
        }

        if (index == _activeTopicIndex)
        {
            CloseDropdown();
            return;
        }

        SwitchToTopic(index);
    }

    public void SwitchToTopic(int index)
    {
        if (index < 0 || index >= topics.Count) return;
        if (!IsTopicUnlocked(index)) return;

        if (GameManager.Instance != null && GameManager.Instance.IsEditorOpen)
            GameManager.Instance.CloseEditor();

        SetTopicContainersActive(_activeTopicIndex, false);
        _activeTopicIndex = index;
        SetTopicContainersActive(_activeTopicIndex, true);

        CloseDropdown();
        RefreshTabUI();

        Debug.Log($"[TopicManager] Switched to: {topics[_activeTopicIndex].topicName}");
    }

    public void MarkTopicComplete(int index)
    {
        if (index < 0 || index >= _topicComplete.Length) return;
        _topicComplete[index] = true;
        RefreshTabUI();
        Debug.Log($"[TopicManager] Topic {index} ({topics[index].topicName}) marked complete.");
    }

    public bool IsTopicComplete(int index)
    {
        if (index < 0 || index >= _topicComplete.Length) return false;
        return _topicComplete[index];
    }

    public bool IsTopicUnlocked(int index)
    {
        if (index < 0 || index >= topics.Count) return false;
        // Topic 0 (Topic 1) is always accessible.
        if (index == 0) return true;
        // Without the required flag, all topics are freely accessible.
        if (!required) return true;
        // With required: Topics 2 and 3 only unlock once Topic 1 is complete.
        return _topicComplete[0];
    }

    public Transform GetActiveWorldContainer()
    {
        if (_activeTopicIndex < 0 || _activeTopicIndex >= topics.Count) return null;
        var go = topics[_activeTopicIndex].worldRootContainer;
        if (go == null)
            Debug.LogWarning($"[TopicManager] worldRootContainer is not assigned for topic index {_activeTopicIndex} — falling back to worldRoot.");
        return go != null ? go.transform : null;
    }

    public Transform GetActiveHardwareStorageContainer()
    {
        if (_activeTopicIndex < 0 || _activeTopicIndex >= topics.Count) return null;
        var go = topics[_activeTopicIndex].hardwareStorageContainer;
        return go != null ? go.transform : null;
    }

    private void ActivateTopic(int index)
    {
        for (int i = 0; i < topics.Count; i++)
            SetTopicContainersActive(i, i == index);
        _activeTopicIndex = index;
    }

    private void SetTopicContainersActive(int index, bool active)
    {
        if (index < 0 || index >= topics.Count) return;
        var t = topics[index];
        if (t.worldRootContainer != null) t.worldRootContainer.SetActive(active);
        if (t.hardwareAreaContainer != null) t.hardwareAreaContainer.SetActive(active);
        if (t.taskListContainer != null) t.taskListContainer.SetActive(active);
        if (t.terminalContainer != null) t.terminalContainer.SetActive(active);
    }

    private void CloseDropdown()
    {
        _isDropdownOpen = false;
        if (tabDropdown != null)
            tabDropdown.SetActive(false);
    }

    private void RefreshTabUI()
    {
        for (int i = 0; i < tabButtons.Count && i < topics.Count; i++)
        {
            if (tabButtons[i].nameText != null)
                tabButtons[i].nameText.text = topics[i].topicName;

            bool unlocked = IsTopicUnlocked(i);

            if (tabButtons[i].button != null)
                tabButtons[i].button.interactable = unlocked;

            // Dim the entire tab entry when locked so the player can see it but knows it's unavailable.
            var cg = tabButtons[i].button != null
                ? tabButtons[i].button.GetComponent<CanvasGroup>()
                : null;
            if (cg == null && tabButtons[i].button != null)
                cg = tabButtons[i].button.gameObject.AddComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha          = unlocked ? 1f : 0.4f;
                cg.interactable   = unlocked;
                cg.blocksRaycasts = unlocked;
            }
        }
    }
}
