using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On FrontPanelConnector_Port. Provides the Phase 2 sub-pin detail view during Motherboard
/// Phase 1. Right-clicking the port (when the Phase 1 cable is already seated) opens
/// ThirdLayer showing the 5 front panel pin sub-ports and their cables.
/// Enabled/disabled by MotherboardPhaseManager alongside Phase 1 interactions.
/// Mirrors the pattern of GPUPhase1CableInteraction.
/// </summary>
public class FrontPanelConnectorInteraction : MonoBehaviour
{
    private GameObject _detailedView;
    private Transform _originalParent;
    private Vector3 _originalLocalPos;
    private Vector3 _originalLocalScale;
    private bool _isPanelOpen = false;
    private GameObject _motherboard;

    public bool IsPanelOpen => _isPanelOpen;

    private void Awake()
    {
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
            ClosePanel();
    }

    private void Update()
    {
        if (_isPanelOpen) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsEditorOpen) return;
        if (!Mouse.current.rightButton.wasPressedThisFrame) return;
        if (!IsMouseOver()) return;

        CablePort port = GetComponent<CablePort>();
        if (port != null && port.IsUninstalled)
        {
            ActivityLogManager.Log("Connect the front panel cable before accessing the sub-pins.", ActivityLogManager.EntryType.Warning);
            return;
        }

        OpenPanel();
    }

    private void OpenPanel()
    {
        GameObject thirdLayer = GameManager.Instance?.thirdLayer;
        if (thirdLayer == null)
        {
            Debug.LogError("[FrontPanelConnectorInteraction] thirdLayer not assigned in GameManager.");
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

        if (_detailedView != null)
            _detailedView.SetActive(true);

        thirdLayer.SetActive(true);
        _isPanelOpen = true;
        GameManager.Instance?.RegisterFrontPanelInteraction(this);

        Debug.Log("[FrontPanelConnectorInteraction] Front panel detail view opened.");
    }

    public void ClosePanel()
    {
        if (!_isPanelOpen) return;

        // Set false early so OnDisable (triggered by reparenting into inactive hierarchy) is a no-op.
        _isPanelOpen = false;

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

        GameManager.Instance?.RegisterFrontPanelInteraction(null);

        Debug.Log("[FrontPanelConnectorInteraction] Front panel detail view closed.");
    }

    private void RestoreMotherboard()
    {
        if (_motherboard != null)
        {
            _motherboard.SetActive(true);
            _motherboard = null;
        }
    }

    // Uses GetComponentsInChildren to catch clicks on the installed Phase 1 cable child.
    private bool IsMouseOver()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
        {
            if (!col.enabled) continue;
            if (col is BoxCollider2D box && (box.size.x < 0.01f || box.size.y < 0.01f)) continue;
            if (col.OverlapPoint(mouseWorld)) return true;
        }
        return false;
    }
}
