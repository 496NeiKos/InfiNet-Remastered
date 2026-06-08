using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("Drop Zones (UI rect checks only)")]
    public RectTransform workspaceArea;
    public RectTransform hardwareArea;

    [Header("World Parents")]
    public Transform worldRoot;
    public Transform hardwareStorage;

    [Header("Detail Panel Layers")]
    public GameObject firstLayer;   // top-level hardware editing (was EditingPanel)
    public GameObject secondLayer;  // inner component editing (was InnerEditingPanel)
    public GameObject thirdLayer;   // GPU Phase 1 cable panel (was ThirdLayerPanel)

    public static GameManager Instance { get; private set; }

    public bool IsEditorOpen { get; private set; } = false;

    public Transform ActiveWorldContainer =>
        TopicManager.Instance != null
            ? (TopicManager.Instance.GetActiveWorldContainer() ?? worldRoot)
            : worldRoot;

    public Transform ActiveHardwareStorageContainer =>
        TopicManager.Instance != null
            ? (TopicManager.Instance.GetActiveHardwareStorageContainer() ?? hardwareStorage)
            : hardwareStorage;

    private PrefabInteraction _activeInteraction;
    private Transform _prefabOriginalParent;
    private Vector3 _prefabOriginalWorldPos;

    private MotherboardDetailViewManager _activeMbdvm;
    private GPUPhase1CableInteraction _activeGpuPhase1Panel;
    private IInPlaceInteraction _activeInPlaceInteraction;

    public void RegisterGPUPhase1Panel(GPUPhase1CableInteraction panel) =>
        _activeGpuPhase1Panel = panel;

    public void OpenEditorInPlace(IInPlaceInteraction interaction)
    {
        _activeInPlaceInteraction = interaction;
        IsEditorOpen = true;
        interaction.ShowDetail();
        Debug.Log("[GameManager] In-place editor opened.");
    }

    private void Update()
    {
        if (IsEditorOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            CloseEditor();
    }

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

        _activeMbdvm = interaction.GetComponent<MotherboardDetailViewManager>();

        _prefabOriginalParent = interaction.transform.parent;
        _prefabOriginalWorldPos = interaction.transform.position;

        interaction.transform.SetParent(firstLayer.transform, true);
        _activeInteraction.ShowDetailCentered();

        if (firstLayer != null)
            firstLayer.SetActive(true);
        else
            Debug.LogError("GameManager: firstLayer is not assigned in the Inspector!");
    }

    public void CloseEditor()
    {
        // In-place editor (T2Monitor / UEFI) — just hide the Canvas, no reparenting needed
        if (_activeInPlaceInteraction != null)
        {
            _activeInPlaceInteraction.HideDetail();
            _activeInPlaceInteraction = null;
            IsEditorOpen = false;
            Debug.Log("[GameManager] In-place editor closed.");
            return;
        }

        if (_activeGpuPhase1Panel != null && _activeGpuPhase1Panel.IsPanelOpen)
        {
            _activeGpuPhase1Panel.ClosePanel();
            return;
        }

        if (_activeInteraction != null)
        {
            DetailViewManager dvm = _activeInteraction.GetComponent<DetailViewManager>();
            if (dvm != null && dvm.IsInnerPanelOpen)
            {
                dvm.CloseInnerPanel();
                return;
            }
        }

        if (_activeMbdvm != null && _activeMbdvm.IsInnerPanelOpen)
        {
            _activeMbdvm.CloseInnerPanel();
            return;
        }

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

        // Layers are now siblings — close all explicitly
        if (firstLayer != null) firstLayer.SetActive(false);
        if (secondLayer != null) secondLayer.SetActive(false);
        if (thirdLayer != null) thirdLayer.SetActive(false);
    }
}
