using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On Motherboard root.
/// Handles right-click on CPU, Heatsink, and RAM while Motherboard is in the editing panel.
/// Same pattern as DetailViewManager on SystemUnit:
///   - Motherboard root (this) disables when inner panel opens
///   - Component reparents to InnerEditingPanel and centers
///   - Close: Motherboard re-enables FIRST, then component reparents back to its slot
/// </summary>
public class MotherboardDetailViewManager : MonoBehaviour
{
    [Header("Inner Editing Panel (auto-found if empty)")]
    [SerializeField] private GameObject innerEditingPanel;

    private GameObject _activeChildPrefab;
    private Transform _childOriginalParent;
    private Vector3 _childOriginalLocalPos;
    private Vector3 _childOriginalLocalScale;
    private bool _isInnerPanelOpen = false;

    public bool IsInnerPanelOpen => _isInnerPanelOpen;

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsEditorOpen) return;
        if (_isInnerPanelOpen) return;

        // Only handle input while this Motherboard is actually inside the editing panel.
        // Prevents responding to clicks meant for another object (e.g. SystemUnit) that
        // is currently being edited instead.
        if (GameManager.Instance.editingPanel == null ||
            !transform.IsChildOf(GameManager.Instance.editingPanel.transform))
            return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
            CheckComponentRightClick();
    }

    private void CheckComponentRightClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit2D[] hits = Physics2D.RaycastAll(ray.origin, ray.direction);
        if (hits.Length == 0) return;

        System.Array.Sort(hits, (a, b) =>
        {
            SpriteRenderer srA = a.collider.GetComponent<SpriteRenderer>();
            SpriteRenderer srB = b.collider.GetComponent<SpriteRenderer>();
            int orderA = srA != null ? srA.sortingOrder : 0;
            int orderB = srB != null ? srB.sortingOrder : 0;
            return orderB.CompareTo(orderA);
        });

        foreach (RaycastHit2D hit in hits)
        {
            HeatsinkController heatsink = hit.collider.GetComponent<HeatsinkController>();
            if (heatsink != null) { OpenInnerPanel(heatsink.gameObject); return; }

            CPUController cpu = hit.collider.GetComponent<CPUController>();
            if (cpu != null) { OpenInnerPanel(cpu.gameObject); return; }

            RAMController ram = hit.collider.GetComponent<RAMController>();
            if (ram != null) { OpenInnerPanel(ram.gameObject); return; }

            GPUController gpu = hit.collider.GetComponent<GPUController>();
            if (gpu != null)
            {
                // During Phase 1, GPUPhase1CableInteraction owns right-click on the GPU
                GPUPhase1CableInteraction phase1Cable = gpu.GetComponent<GPUPhase1CableInteraction>();
                if (phase1Cable != null && phase1Cable.enabled) return;
                OpenInnerPanel(gpu.gameObject);
                return;
            }
        }
    }

    private void OpenInnerPanel(GameObject childPrefab)
    {
        GameObject panel = GetInnerEditingPanel();
        if (panel == null)
        {
            Debug.LogError("[MotherboardDetailViewManager] InnerEditingPanel not found.");
            return;
        }

        _activeChildPrefab = childPrefab;
        _isInnerPanelOpen = true;
        _childOriginalParent = childPrefab.transform.parent;
        _childOriginalLocalPos = childPrefab.transform.localPosition;
        _childOriginalLocalScale = childPrefab.transform.localScale;

        // Disable Motherboard root — same pattern as SystemUnit disabling for MB inner panel
        gameObject.SetActive(false);

        // Reparent to inner panel and center
        childPrefab.transform.SetParent(panel.transform, true);
        RectTransform rect = panel.GetComponent<RectTransform>();
        if (rect != null)
        {
            Vector3 center = rect.TransformPoint(
                new Vector3(rect.rect.center.x, rect.rect.center.y, 0f));
            center.z = 0f;
            childPrefab.transform.position = center;
        }

        // Activate the direct "Detailed" child of the component
        SetDetailedView(childPrefab, true);

        panel.SetActive(true);
        Debug.Log($"[MotherboardDetailViewManager] Opened inner panel for {childPrefab.name}");
    }

    public void CloseInnerPanel()
    {
        if (!_isInnerPanelOpen || _activeChildPrefab == null) return;

        SetDetailedView(_activeChildPrefab, false);

        // Re-enable Motherboard FIRST — makes slot hierarchy active again before reparenting
        gameObject.SetActive(true);

        _activeChildPrefab.transform.SetParent(_childOriginalParent, false);
        _activeChildPrefab.transform.localPosition = _childOriginalLocalPos;
        _activeChildPrefab.transform.localScale = _childOriginalLocalScale;

        if (innerEditingPanel != null)
            innerEditingPanel.SetActive(false);

        _activeChildPrefab = null;
        _isInnerPanelOpen = false;

        Debug.Log("[MotherboardDetailViewManager] Inner panel closed, component returned to slot.");
    }

    private void SetDetailedView(GameObject component, bool active)
    {
        if (component == null) return;
        // Only direct children — prevents accidentally activating nested views
        foreach (Transform child in component.transform)
        {
            if (child.name.Contains("Detailed"))
            {
                child.gameObject.SetActive(active);
                return;
            }
        }
    }

    private GameObject GetInnerEditingPanel()
    {
        if (innerEditingPanel != null) return innerEditingPanel;

        if (GameManager.Instance?.editingPanel == null) return null;

        foreach (Transform t in GameManager.Instance.editingPanel
            .GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "InnerEditingPanel")
            {
                innerEditingPanel = t.gameObject;
                return innerEditingPanel;
            }
        }
        return null;
    }
}
