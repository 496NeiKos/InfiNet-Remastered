using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NCIITaskListManager : MonoBehaviour
{
    public static NCIITaskListManager Instance { get; private set; }

    [Header("Disassembly UI")]
    [SerializeField] private Transform disassemblyTaskParent;
    [SerializeField] private Transform disassemblyFinishedParent;
    [SerializeField] private GameObject[] disassemblyTaskObjects;

    [Header("Assembly UI (future)")]
    [SerializeField] private Transform assemblyTaskParent;
    [SerializeField] private Transform assemblyFinishedParent;

    [Header("Toggle Button")]
    [SerializeField] private Button toggleButton;

    [Header("Condition References")]
    [SerializeField] private CoverController coverController;
    [SerializeField] private MotherboardController motherboardController;
    [SerializeField] private HardwareHolder psuHolder;
    [SerializeField] private HardwareHolder hddHolder;
    [SerializeField] private HardwareHolder cpuHolder;
    [SerializeField] private HardwareHolder heatsinkHolder;
    [SerializeField] private HardwareHolder ram1Holder;
    [SerializeField] private HardwareHolder ram2Holder;
    [SerializeField] private HardwareHolder cmosHolder;
    [SerializeField] private HardwareHolder ssdHolder;

    private class TaskEntry
    {
        public GameObject taskObject;
        public int originalIndex;
        public bool isCompleted;
        public bool isFlashing;
        public Func<bool> condition;
    }

    private List<TaskEntry> _disassemblyTasks;
    private BackPortSlot[] _allBackPortSlots;
    private bool _showingAssembly = false;
    private const int WindowSize = 3;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _allBackPortSlots = FindObjectsOfType<BackPortSlot>(true);

        if (disassemblyTaskObjects == null || disassemblyTaskObjects.Length < 4)
        {
            Debug.LogError("[NCIITaskListManager] Assign all 4 disassembly task objects in the inspector.");
            return;
        }

        _disassemblyTasks = new List<TaskEntry>
        {
            new TaskEntry
            {
                taskObject = disassemblyTaskObjects[0],
                originalIndex = 0,
                condition = () => _allBackPortSlots != null
                               && _allBackPortSlots.Length > 0
                               && _allBackPortSlots.All(p => p.IsUninstalled)
            },
            new TaskEntry
            {
                taskObject = disassemblyTaskObjects[1],
                originalIndex = 1,
                condition = () => coverController != null && coverController.IsOpen()
            },
            new TaskEntry
            {
                taskObject = disassemblyTaskObjects[2],
                originalIndex = 2,
                condition = () =>
                    (motherboardController == null || motherboardController.IsUninstalledFromSystemUnit) &&
                    (psuHolder == null || psuHolder.IsAvailable()) &&
                    (hddHolder == null || hddHolder.IsAvailable())
            },
            new TaskEntry
            {
                taskObject = disassemblyTaskObjects[3],
                originalIndex = 3,
                condition = () =>
                    (cpuHolder == null || cpuHolder.IsAvailable()) &&
                    (heatsinkHolder == null || heatsinkHolder.IsAvailable()) &&
                    (ram1Holder == null || ram1Holder.IsAvailable()) &&
                    (ram2Holder == null || ram2Holder.IsAvailable()) &&
                    (cmosHolder == null || cmosHolder.IsAvailable()) &&
                    (ssdHolder == null || ssdHolder.IsAvailable())
            }
        };

        foreach (var task in _disassemblyTasks)
        {
            task.taskObject.SetActive(false);
            task.taskObject.transform.SetParent(disassemblyTaskParent, false);
            task.taskObject.transform.SetSiblingIndex(task.originalIndex);
            var tmp = task.taskObject.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.color = Color.white;
        }

        if (assemblyTaskParent != null)
            assemblyTaskParent.gameObject.SetActive(false);

        if (toggleButton != null)
            toggleButton.onClick.AddListener(OnToggleClicked);

        RefreshWindow();
    }

    public static void CheckConditions()
    {
        if (Instance == null) return;
        Instance.EvaluateConditions();
    }

    private void EvaluateConditions()
    {
        if (_disassemblyTasks == null) return;

        foreach (var task in _disassemblyTasks)
        {
            if (task.isFlashing) continue;

            bool met = task.condition();

            if (!task.isCompleted && met)
            {
                task.isCompleted = true;
                StartCoroutine(FlashAndComplete(task));
            }
            else if (task.isCompleted && !met)
            {
                task.isCompleted = false;
                task.taskObject.transform.SetParent(disassemblyTaskParent, false);
                task.taskObject.transform.SetSiblingIndex(task.originalIndex);
                task.taskObject.SetActive(false);
                RefreshWindow();
                if (task.taskObject.activeSelf)
                    StartCoroutine(FlashRevert(task));
            }
        }
    }

    private IEnumerator FlashAndComplete(TaskEntry task)
    {
        task.isFlashing = true;
        var tmp = task.taskObject.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.color = Color.green;
        yield return new WaitForSeconds(0.6f);
        task.isFlashing = false;
        task.taskObject.transform.SetParent(disassemblyFinishedParent, false);
        task.taskObject.SetActive(false);
        RefreshWindow();
        EvaluateConditions();
    }

    private IEnumerator FlashRevert(TaskEntry task)
    {
        task.isFlashing = true;
        var tmp = task.taskObject.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.color = new Color(1f, 0.647f, 0f);
        yield return new WaitForSeconds(0.6f);
        task.isFlashing = false;
        if (tmp != null) tmp.color = Color.white;
        EvaluateConditions();
    }

    private void RefreshWindow()
    {
        if (_disassemblyTasks == null) return;

        var incomplete = _disassemblyTasks
            .Where(t => !t.isCompleted)
            .OrderBy(t => t.originalIndex)
            .ToList();

        for (int i = 0; i < incomplete.Count; i++)
            incomplete[i].taskObject.SetActive(i < WindowSize);
    }

    private bool IsDisassemblyComplete() =>
        _disassemblyTasks != null && _disassemblyTasks.All(t => t.isCompleted);

    private void OnToggleClicked()
    {
        if (!IsDisassemblyComplete())
        {
            Debug.Log("[NCIITaskListManager] Assembly locked — complete disassembly first.");
            return;
        }

        _showingAssembly = !_showingAssembly;

        if (disassemblyTaskParent != null)
            disassemblyTaskParent.gameObject.SetActive(!_showingAssembly);
        if (assemblyTaskParent != null)
            assemblyTaskParent.gameObject.SetActive(_showingAssembly);
    }
}
