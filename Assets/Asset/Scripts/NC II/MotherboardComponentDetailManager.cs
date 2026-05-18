using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On Motherboard root.
/// Manages opening/closing CPU and Heatsink detail panels from Phase 2.
/// Detail panels open on top of Motherboard editing panel.
/// While a detail panel is open, all Phase 2 DragPrefab and PrefabInteraction are disabled.
/// </summary>
public class MotherboardComponentDetailManager : MonoBehaviour
{
    [Header("Detail Panels (in Canvas)")]
    [SerializeField] private GameObject cpuDetailPanel;
    [SerializeField] private GameObject heatsinkDetailPanel;

    [Header("Phase 2 Root (to disable interaction while panel open)")]
    [SerializeField] private GameObject phase2Root;

    private GameObject _activePanel = null;
    private GameObject _activeComponent = null;
    private Transform _componentOriginalParent;
    private Vector3 _componentOriginalLocalPos;

    public bool IsDetailPanelOpen => _activePanel != null;

    private void Update()
    {
        if (!GameManager.Instance.IsEditorOpen) return;
        if (IsDetailPanelOpen) return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
            CheckComponentRightClick();
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

        // Check Heatsink first (topmost), then CPU
        HeatsinkController heatsink = null;
        CPUController cpu = null;

        foreach (RaycastHit2D hit in hits)
        {
            if (heatsink == null)
                heatsink = hit.collider.GetComponent<HeatsinkController>();

            if (cpu == null)
                cpu = hit.collider.GetComponent<CPUController>();
        }

        if (heatsink != null)
        {
            OpenDetailPanel(heatsink.gameObject, heatsinkDetailPanel);
            return;
        }

        if (cpu != null)
        {
            OpenDetailPanel(cpu.gameObject, cpuDetailPanel);
        }
    }

    public void OpenDetailPanel(GameObject component, GameObject panel)
    {
        if (panel == null)
        {
            Debug.LogWarning("[MBComponentDetailManager] Detail panel not assigned.");
            return;
        }

        _activeComponent = component;
        _activePanel = panel;
        _componentOriginalParent = component.transform.parent;
        _componentOriginalLocalPos = component.transform.localPosition;

        // Disable Phase 2 interaction
        SetPhase2Interactable(false);

        // Center component in detail panel
        component.transform.SetParent(panel.transform, true);
        RectTransform rect = panel.GetComponent<RectTransform>();
        if (rect != null)
        {
            Vector3 center = rect.TransformPoint(new Vector3(rect.rect.center.x, rect.rect.center.y, 0f));
            center.z = 0f;
            component.transform.position = center;
        }

        // Activate detailed child view
        ActivateDetailedView(component, true);

        panel.SetActive(true);
        Debug.Log($"[MBComponentDetailManager] Opened detail panel for {component.name}");
    }

    public void CloseDetailPanel()
    {
        if (_activePanel == null) return;

        // Deactivate detailed child view
        ActivateDetailedView(_activeComponent, false);

        // Return component to original parent
        if (_activeComponent != null)
        {
            _activeComponent.transform.SetParent(_componentOriginalParent, true);
            _activeComponent.transform.localPosition = _componentOriginalLocalPos;
        }

        _activePanel.SetActive(false);
        _activePanel = null;
        _activeComponent = null;

        // Re-enable Phase 2 interaction
        SetPhase2Interactable(true);

        Debug.Log("[MBComponentDetailManager] Detail panel closed, Phase 2 restored.");
    }

    private void SetPhase2Interactable(bool interactable)
    {
        if (phase2Root == null) return;

        foreach (DragPrefab dp in phase2Root.GetComponentsInChildren<DragPrefab>(true))
            dp.enabled = interactable;

        foreach (PrefabInteraction pi in phase2Root.GetComponentsInChildren<PrefabInteraction>(true))
            pi.enabled = interactable;
    }

    private void ActivateDetailedView(GameObject component, bool active)
    {
        if (component == null) return;

        foreach (Transform child in component.transform)
        {
            if (child.name.Contains("Detailed"))
            {
                child.gameObject.SetActive(active);
                return;
            }
        }
    }
}