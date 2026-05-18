using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On Motherboard root.
/// Handles right-click on CPU and Heatsink while Motherboard editing panel is open.
/// Reuses InnerEditingPanel — same pattern as DetailViewManager on SystemUnit.
/// </summary>
public class MotherboardDetailViewManager : MonoBehaviour
{
    [Header("Inner Editing Panel (shared, auto-found if empty)")]
    [SerializeField] private GameObject innerEditingPanel;

    private GameObject _activeChildPrefab;
    private Transform _childOriginalParent;
    private Vector3 _childOriginalLocalPos;
    private bool _isInnerPanelOpen = false;

    public bool IsInnerPanelOpen => _isInnerPanelOpen;

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsEditorOpen) return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (_isInnerPanelOpen) return;
            CheckComponentRightClick();
        }
    }

    private void CheckComponentRightClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit2D[] hits = Physics2D.RaycastAll(ray.origin, ray.direction);

        if (hits.Length == 0) return;

        // Sort by SpriteRenderer sortingOrder descending — topmost visual wins
        System.Array.Sort(hits, (a, b) =>
        {
            SpriteRenderer srA = a.collider.GetComponent<SpriteRenderer>();
            SpriteRenderer srB = b.collider.GetComponent<SpriteRenderer>();
            int orderA = srA != null ? srA.sortingOrder : 0;
            int orderB = srB != null ? srB.sortingOrder : 0;
            return orderB.CompareTo(orderA);
        });

        // Heatsink takes priority over CPU when overlapping
        HeatsinkController heatsink = null;
        CPUController cpu = null;

        foreach (RaycastHit2D hit in hits)
        {
            if (heatsink == null) heatsink = hit.collider.GetComponent<HeatsinkController>();
            if (cpu == null) cpu = hit.collider.GetComponent<CPUController>();
        }

        if (heatsink != null)
        {
            OpenInnerPanel(heatsink.gameObject);
            return;
        }

        if (cpu != null)
            OpenInnerPanel(cpu.gameObject);
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

        panel.SetActive(true);

        childPrefab.transform.SetParent(panel.transform, true);

        RectTransform rect = panel.GetComponent<RectTransform>();
        if (rect != null)
        {
            Vector3 center = rect.TransformPoint(new Vector3(rect.rect.center.x, rect.rect.center.y, 0f));
            center.z = 0f;
            childPrefab.transform.position = center;
        }

        ActivateDetailedView(childPrefab, true);

        // Disable Phase 2 drag/interaction while inner panel is open
        SetPhase2Interactable(false);

        Debug.Log($"[MotherboardDetailViewManager] Opened inner panel for {childPrefab.name}");
    }

    public void CloseInnerPanel()
    {
        if (!_isInnerPanelOpen || _activeChildPrefab == null) return;

        ActivateDetailedView(_activeChildPrefab, false);

        _activeChildPrefab.transform.SetParent(_childOriginalParent, true);
        _activeChildPrefab.transform.localPosition = _childOriginalLocalPos;

        if (innerEditingPanel != null)
            innerEditingPanel.SetActive(false);

        _activeChildPrefab = null;
        _isInnerPanelOpen = false;

        // Re-enable Phase 2 interaction
        SetPhase2Interactable(true);

        Debug.Log("[MotherboardDetailViewManager] Inner panel closed.");
    }

    private void ActivateDetailedView(GameObject component, bool active)
    {
        if (component == null) return;

        // Search all descendants — CPUDetailed may be a grandchild (CPU root -> CPUDetailed)
        foreach (Transform t in component.GetComponentsInChildren<Transform>(true))
        {
            if (t == component.transform) continue; // skip self
            if (t.name.Contains("Detailed"))
            {
                t.gameObject.SetActive(active);
                return;
            }
        }
    }

    private void SetPhase2Interactable(bool interactable)
    {
        MotherboardPhaseManager phase = GetComponent<MotherboardPhaseManager>();
        if (phase == null) return;

        Transform phase2Root = phase.GetPhase2Root();
        if (phase2Root == null) return;

        foreach (DragPrefab dp in phase2Root.GetComponentsInChildren<DragPrefab>(true))
            dp.enabled = interactable;

        foreach (PrefabInteraction pi in phase2Root.GetComponentsInChildren<PrefabInteraction>(true))
            pi.enabled = interactable;
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