using System.Collections.Generic;
using UnityEngine;

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

    private PrefabInteraction _activeInteraction;
    private Transform _prefabOriginalParent;
    private Vector3 _prefabOriginalWorldPos;

    private MotherboardDetailViewManager _activeMbdvm;
    private GPUPhase1CableInteraction _activeGpuPhase1Panel;

    // Sorting-order snapshot for every SpriteRenderer in worldRoot taken when the
    // editor opens. Restored when the editor fully closes.
    private readonly Dictionary<SpriteRenderer, int> _worldRootSavedOrders
        = new Dictionary<SpriteRenderer, int>();

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

        _activeMbdvm = interaction.GetComponent<MotherboardDetailViewManager>();

        _prefabOriginalParent = interaction.transform.parent;
        _prefabOriginalWorldPos = interaction.transform.position;

        // Move the component out of worldRoot first so it isn't caught by DimWorldRoot.
        interaction.transform.SetParent(firstLayer.transform, true);
        _activeInteraction.ShowDetailCentered();

        DimWorldRoot();

        if (firstLayer != null)
            firstLayer.SetActive(true);
        else
            Debug.LogError("GameManager: firstLayer is not assigned in the Inspector!");
    }

    public void CloseEditor()
    {
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
        RestoreWorldRoot();

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

    private void DimWorldRoot()
    {
        _worldRootSavedOrders.Clear();
        if (worldRoot == null) return;

        foreach (Transform child in worldRoot)
        {
            foreach (SpriteRenderer sr in child.GetComponentsInChildren<SpriteRenderer>(true))
            {
                _worldRootSavedOrders[sr] = sr.sortingOrder;
                sr.sortingOrder = -1;
            }
        }
    }

    private void RestoreWorldRoot()
    {
        foreach (var kvp in _worldRootSavedOrders)
        {
            if (kvp.Key != null)
                kvp.Key.sortingOrder = kvp.Value;
        }
        _worldRootSavedOrders.Clear();
    }
}
