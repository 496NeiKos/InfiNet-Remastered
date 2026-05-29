using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On the GPU root. Provides cable-only interaction during Motherboard Phase 1.
/// Right-clicking the GPU opens ThirdLayer showing only the cable slot —
/// screws and latch remain inactive until Phase 2.
/// Enabled/disabled by MotherboardPhaseManager.
/// </summary>
public class GPUPhase1CableInteraction : MonoBehaviour
{
    private GPUController _gpuController;
    private GameObject _detailedView;
    private Transform _originalParent;
    private Vector3 _originalLocalPos;
    private Vector3 _originalLocalScale;
    private bool _isPanelOpen = false;
    private GameObject _motherboard;

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
        if (_isPanelOpen)
        {
            SetGPUInteraction(false);
            if (_detailedView != null) _detailedView.SetActive(false);
            RestoreMotherboard();
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
        GameObject thirdLayer = GameManager.Instance?.thirdLayer;
        if (thirdLayer == null)
        {
            Debug.LogError("[GPUPhase1CableInteraction] thirdLayer not assigned in GameManager.");
            return;
        }

        _originalParent = transform.parent;
        _originalLocalPos = transform.localPosition;
        _originalLocalScale = transform.localScale;

        // Find and disable the Motherboard before reparenting (GetComponentInParent won't work after).
        MotherboardController mb = GetComponentInParent<MotherboardController>();
        _motherboard = mb?.gameObject;
        if (_motherboard != null) _motherboard.SetActive(false);

        transform.SetParent(thirdLayer.transform, true);

        RectTransform rect = thirdLayer.GetComponent<RectTransform>();
        if (rect != null)
        {
            Vector3 center = rect.TransformPoint(
                new Vector3(rect.rect.center.x, rect.rect.center.y, 0f));
            center.z = 0f;
            transform.position = center;
        }

        SetGPUInteraction(true);

        if (_detailedView != null)
            _detailedView.SetActive(true);

        thirdLayer.SetActive(true);
        _isPanelOpen = true;
        GameManager.Instance?.RegisterGPUPhase1Panel(this);

        Debug.Log("[GPUPhase1CableInteraction] Phase 1 GPU cable panel opened.");
    }

    public void ClosePanel()
    {
        if (!_isPanelOpen) return;

        // Set false early so OnDisable (triggered by reparenting into inactive hierarchy) is a no-op.
        _isPanelOpen = false;

        SetGPUInteraction(false);

        if (_detailedView != null)
            _detailedView.SetActive(false);

        // Re-enable MB BEFORE reparenting: reparenting into an inactive parent triggers OnDisable,
        // and calling SetActive inside that cascade throws "already being activated or deactivated".
        RestoreMotherboard();

        transform.SetParent(_originalParent, false);
        transform.localPosition = _originalLocalPos;
        transform.localScale = _originalLocalScale;

        if (GameManager.Instance?.thirdLayer != null)
            GameManager.Instance.thirdLayer.SetActive(false);

        GameManager.Instance?.RegisterGPUPhase1Panel(null);

        Debug.Log("[GPUPhase1CableInteraction] Phase 1 GPU cable panel closed.");
    }

    private void SetGPUInteraction(bool enable)
    {
        if (_gpuController == null) return;

        foreach (var cp in _gpuController.GetComponentsInChildren<CablePort>(true))
        {
            cp.enabled = enable;
            foreach (Collider2D col in cp.GetComponents<Collider2D>())
                col.enabled = enable;
        }

        foreach (var cb in _gpuController.GetComponentsInChildren<CableBehavior>(true))
        {
            if (!enable && cb.IsDetached) continue;
            cb.enabled = enable;
            foreach (Collider2D col in cb.GetComponents<Collider2D>())
                col.enabled = enable;
        }

        foreach (var sc in _gpuController.GetComponentsInChildren<ScrewController>(true))
        {
            sc.enabled = enable;
            foreach (Collider2D col in sc.GetComponents<Collider2D>())
                col.enabled = enable;
        }
    }

    private void RestoreMotherboard()
    {
        if (_motherboard != null)
        {
            _motherboard.SetActive(true);
            _motherboard = null;
        }
    }

    private bool IsMouseOver()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        foreach (Collider2D col in GetComponents<Collider2D>())
            if (col.OverlapPoint(mouseWorld)) return true;
        return false;
    }
}
