using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Drop Zones (UI rect checks only)")]
    public RectTransform workspaceArea;
    public RectTransform hardwareArea;

    [Header("World Parents")]
    public Transform worldRoot;
    public Transform hardwareStorage;

    [Header("UI Panels")]
    public GameObject editingPanel;

    public static GameManager Instance { get; private set; }

    public bool IsEditorOpen { get; private set; } = false;

    private PrefabInteraction _activeInteraction;
    private Transform _prefabOriginalParent;
    private Vector3 _prefabOriginalWorldPos;

    // Cached separately — Motherboard disables itself when MB inner panel opens,
    // making GetComponent unreliable at that point
    private MotherboardDetailViewManager _activeMbdvm;
    private GPUPhase1CableInteraction _activeGpuPhase1Panel;

    public void RegisterGPUPhase1Panel(GPUPhase1CableInteraction panel) =>
        _activeGpuPhase1Panel = panel;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void OpenEditor(PrefabInteraction interaction)
    {
        if (_activeInteraction != null && _activeInteraction != interaction)
            _activeInteraction.OnEditorClosed();

        _activeInteraction = interaction;
        IsEditorOpen = true;

        // Cache MotherboardDetailViewManager before it potentially disables itself
        _activeMbdvm = interaction.GetComponent<MotherboardDetailViewManager>();

        _prefabOriginalParent = interaction.transform.parent;
        _prefabOriginalWorldPos = interaction.transform.position;

        interaction.transform.SetParent(editingPanel.transform, true);
        _activeInteraction.ShowDetailCentered();

        if (editingPanel != null)
            editingPanel.SetActive(true);
        else
            Debug.LogError("GameManager: editingPanel is not assigned in the Inspector!");
    }

    public void CloseEditor()
    {
        // Check GPU Phase 1 cable panel (third layer) — close it first before anything else
        if (_activeGpuPhase1Panel != null && _activeGpuPhase1Panel.IsPanelOpen)
        {
            _activeGpuPhase1Panel.ClosePanel();
            return;
        }

        // Check SystemUnit inner panel (DetailViewManager on SystemUnit root)
        if (_activeInteraction != null)
        {
            DetailViewManager dvm = _activeInteraction.GetComponent<DetailViewManager>();
            if (dvm != null && dvm.IsInnerPanelOpen)
            {
                dvm.CloseInnerPanel();
                return;
            }
        }

        // Check Motherboard inner panel — use cached ref since Motherboard may be inactive
        if (_activeMbdvm != null && _activeMbdvm.IsInnerPanelOpen)
        {
            _activeMbdvm.CloseInnerPanel();
            return;
        }

        // No inner panels open — close full editing panel
        IsEditorOpen = false;

        if (_activeInteraction != null)
        {
            _activeInteraction.transform.SetParent(_prefabOriginalParent, true);
            _activeInteraction.transform.position = _prefabOriginalWorldPos;

            _activeInteraction.OnEditorClosed();
            _activeInteraction = null;
        }

        _activeMbdvm = null;
        _activeGpuPhase1Panel = null;

        if (editingPanel != null)
            editingPanel.SetActive(false);
    }
}