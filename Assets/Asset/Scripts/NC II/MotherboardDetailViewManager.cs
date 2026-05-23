using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On Motherboard root.
/// Handles right-click on CPU and Heatsink while Motherboard is in the editing panel.
/// Same pattern as DetailViewManager on SystemUnit:
///   - Motherboard root (this) disables when inner panel opens
///   - Component reparents to InnerEditingPanel and centers
///   - Close: Motherboard re-enables FIRST, then component reparents back to CPUSlot
/// </summary>
public class MotherboardDetailViewManager : MonoBehaviour
{
    [Header("Inner Editing Panel (auto-found if empty)")]
    [SerializeField] private GameObject innerEditingPanel;

    private GameObject _activeChildPrefab;
    private Transform _childOriginalParent;   // CPUSlot transform
    private Vector3 _childOriginalLocalPos;
    private Vector3 _childOriginalLocalScale;
    private bool _isInnerPanelOpen = false;

    public bool IsInnerPanelOpen => _isInnerPanelOpen;

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsEditorOpen) return;
        if (_isInnerPanelOpen) return;

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

        // Heatsink collider is disabled when CPU is exposed � only one is hittable at a time
        foreach (RaycastHit2D hit in hits)
        {
            HeatsinkController heatsink = hit.collider.GetComponent<HeatsinkController>();
            if (heatsink != null) { OpenInnerPanel(heatsink.gameObject); return; }

            CPUController cpu = hit.collider.GetComponent<CPUController>();
            if (cpu != null) { OpenInnerPanel(cpu.gameObject); return; }

            RAMController ram = hit.collider.GetComponent<RAMController>();
            if (ram != null) { OpenInnerPanelForRAMDetailed(ram); return; }
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
        _childOriginalParent = childPrefab.transform.parent;   // CPUSlot
        _childOriginalLocalPos = childPrefab.transform.localPosition;
        _childOriginalLocalScale = childPrefab.transform.localScale;

        // Disable Motherboard root � same pattern as SystemUnit disabling for MB inner panel
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

        // Activate only direct "Detailed" child of the component
        SetDetailedView(childPrefab, true);

        panel.SetActive(true);
        Debug.Log($"[MotherboardDetailViewManager] Opened inner panel for {childPrefab.name}");
    }

    // RAM variant: only the RAMDetailedView child moves to the inner panel; the RAM root stays in its slot.
    private void OpenInnerPanelForRAMDetailed(RAMController ram)
    {
        GameObject panel = GetInnerEditingPanel();
        if (panel == null)
        {
            Debug.LogError("[MotherboardDetailViewManager] InnerEditingPanel not found.");
            return;
        }

        Transform detailedChild = null;
        foreach (Transform child in ram.transform)
        {
            if (child.name.Contains("Detailed")) { detailedChild = child; break; }
        }

        if (detailedChild == null)
        {
            Debug.LogError($"[MotherboardDetailViewManager] No 'Detailed' child found on {ram.name}");
            return;
        }

        _activeChildPrefab = detailedChild.gameObject;
        _isInnerPanelOpen = true;
        _childOriginalParent = detailedChild.parent;        // RAM root
        _childOriginalLocalPos = detailedChild.localPosition;
        _childOriginalLocalScale = detailedChild.localScale;

        gameObject.SetActive(false);

        detailedChild.SetParent(panel.transform, true);
        RectTransform rect = panel.GetComponent<RectTransform>();
        if (rect != null)
        {
            Vector3 center = rect.TransformPoint(
                new Vector3(rect.rect.center.x, rect.rect.center.y, 0f));
            center.z = 0f;
            detailedChild.position = center;
        }

        detailedChild.gameObject.SetActive(true);
        panel.SetActive(true);
        Debug.Log($"[MotherboardDetailViewManager] Opened inner panel for {ram.name} (RAMDetailedView only)");
    }

    public void CloseInnerPanel()
    {
        if (!_isInnerPanelOpen || _activeChildPrefab == null) return;

        // RAM flow: _activeChildPrefab IS the detailed child — deactivate it directly.
        // CPU / Heatsink flow: _activeChildPrefab is the component root — find and hide its detailed child.
        if (_activeChildPrefab.GetComponent<RAMDetailedView>() != null)
            _activeChildPrefab.SetActive(false);
        else
            SetDetailedView(_activeChildPrefab, false);

        // Re-enable Motherboard FIRST � makes CPUSlot hierarchy active again
        gameObject.SetActive(true);

        // Reparent back to CPUSlot with exact saved local transform
        _activeChildPrefab.transform.SetParent(_childOriginalParent, false);
        _activeChildPrefab.transform.localPosition = _childOriginalLocalPos;
        _activeChildPrefab.transform.localScale = _childOriginalLocalScale;

        // Hide inner panel � component is no longer its child
        if (innerEditingPanel != null)
            innerEditingPanel.SetActive(false);

        _activeChildPrefab = null;
        _isInnerPanelOpen = false;

        Debug.Log("[MotherboardDetailViewManager] Inner panel closed, component returned to CPUSlot.");
    }

    private void SetDetailedView(GameObject component, bool active)
    {
        if (component == null) return;
        // Only direct children � prevents accidentally activating nested views
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