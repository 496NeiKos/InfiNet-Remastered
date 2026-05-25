using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On the GPU root. Provides cable-only interaction during Motherboard Phase 1
/// (system unit is open in the editing panel, mobo not yet dragged out).
/// Right-clicking the GPU opens a third-layer panel showing only the cable slot —
/// screws and latch remain inactive until Phase 2.
/// Enabled/disabled by MotherboardPhaseManager. The GPU root Collider2D is also
/// toggled by MotherboardPhaseManager so right-click is detectable during Phase 1.
/// </summary>
public class GPUPhase1CableInteraction : MonoBehaviour
{
    [SerializeField] private GameObject thirdLayerPanel;

    private GPUController _gpuController;
    private GameObject _detailedView;
    private Transform _originalParent;
    private Vector3 _originalLocalPos;
    private Vector3 _originalLocalScale;
    private bool _isPanelOpen = false;

    public bool IsPanelOpen => _isPanelOpen;

    private void Awake()
    {
        _gpuController = GetComponent<GPUController>();

        foreach (Transform child in transform)
        {
            if (child.name.Contains("Detailed"))
            {
                _detailedView = child.gameObject;
                break;
            }
        }
    }

    private void OnDisable()
    {
        // Do NOT reparent here — Unity forbids SetParent while a parent is mid-deactivation.
        // GameManager.CloseEditor() closes this panel explicitly before deactivating any panel.
        if (_isPanelOpen)
        {
            SetCableOnlyInteraction(false);
            if (_detailedView != null) _detailedView.SetActive(false);
            RestoreGPUDetailedView();
            _isPanelOpen = false;
        }
    }

    private void Update()
    {
        if (_isPanelOpen) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsEditorOpen) return;
        if (!Mouse.current.rightButton.wasPressedThisFrame) return;
        if (!IsMouseOver()) return;

        OpenPanel();
    }

    private void OpenPanel()
    {
        if (thirdLayerPanel == null)
        {
            Debug.LogError("[GPUPhase1CableInteraction] ThirdLayerPanel not assigned.");
            return;
        }

        _originalParent = transform.parent;
        _originalLocalPos = transform.localPosition;
        _originalLocalScale = transform.localScale;

        transform.SetParent(thirdLayerPanel.transform, true);

        RectTransform rect = thirdLayerPanel.GetComponent<RectTransform>();
        if (rect != null)
        {
            Vector3 center = rect.TransformPoint(
                new Vector3(rect.rect.center.x, rect.rect.center.y, 0f));
            center.z = 0f;
            transform.position = center;
        }

        // Disable GPUDetailedView BEFORE activating the detail view so its OnEnable
        // does not show sub-views (TopView/SideView) during Phase 1.
        SetCableOnlyInteraction(true);

        if (_detailedView != null)
            _detailedView.SetActive(true);

        thirdLayerPanel.SetActive(true);
        _isPanelOpen = true;
        GameManager.Instance?.RegisterGPUPhase1Panel(this);

        Debug.Log("[GPUPhase1CableInteraction] Phase 1 GPU cable panel opened.");
    }

    public void ClosePanel()
    {
        if (!_isPanelOpen) return;

        SetCableOnlyInteraction(false);

        if (_detailedView != null)
            _detailedView.SetActive(false);

        // Re-enable GPUDetailedView AFTER deactivating the Detailed child so OnEnable
        // doesn't fire now. Phase 2 will trigger it fresh when it opens the GPU view.
        RestoreGPUDetailedView();

        transform.SetParent(_originalParent, false);
        transform.localPosition = _originalLocalPos;
        transform.localScale = _originalLocalScale;

        if (thirdLayerPanel != null)
            thirdLayerPanel.SetActive(false);

        _isPanelOpen = false;
        GameManager.Instance?.RegisterGPUPhase1Panel(null);

        Debug.Log("[GPUPhase1CableInteraction] Phase 1 GPU cable panel closed.");
    }

    private void SetCableOnlyInteraction(bool enable)
    {
        if (_gpuController == null) return;

        foreach (var cs in _gpuController.GetComponentsInChildren<CableSlot>(true))
        {
            cs.enabled = enable;
            foreach (Collider2D col in cs.GetComponents<Collider2D>())
                col.enabled = enable;
        }

        foreach (var mc in _gpuController.GetComponentsInChildren<MBCable>(true))
        {
            if (!enable && mc.IsDetached) continue;
            mc.enabled = enable;
            foreach (Collider2D col in mc.GetComponents<Collider2D>())
                col.enabled = enable;
        }

        // When opening: explicitly disable screws and latch view (Phase 2 only)
        if (enable)
        {
            foreach (var sc in _gpuController.GetComponentsInChildren<ScrewController>(true))
            {
                sc.enabled = false;
                foreach (Collider2D col in sc.GetComponents<Collider2D>())
                    col.enabled = false;
            }

            foreach (var gdv in _gpuController.GetComponentsInChildren<GPUDetailedView>(true))
                gdv.enabled = false;
        }
    }

    private void RestoreGPUDetailedView()
    {
        if (_gpuController == null) return;
        foreach (var gdv in _gpuController.GetComponentsInChildren<GPUDetailedView>(true))
            gdv.enabled = true;
    }

    private bool IsMouseOver()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        foreach (Collider2D col in GetComponents<Collider2D>())
            if (col.OverlapPoint(mouseWorld)) return true;
        return false;
    }
}
