using UnityEngine;

public class MotherboardController : MonoBehaviour
{
    public bool IsUninstalledFromSystemUnit { get; private set; } = false;
    private bool _wasEverInSystemUnit = false;

    [Header("Cable & Component Indicators")]
    [Tooltip("Drag the source GameObject — the script finds the correct controller automatically. Supports CableSlot, RAM, GPU, SSD, CPU, and Heatsink.")]
    [SerializeField] private GameObject[] indicatorSources;
    [SerializeField] private GameObject[] cableIndicators;
    [Tooltip("The Indicators parent GameObject — hidden while the motherboard detail view is open.")]
    [SerializeField] private GameObject indicators;

    private bool _started;
    private bool _inSlot;

    private void Start()
    {
        _started = true;
        _inSlot = GetComponentInParent<SlotContainer>(true) != null;
        SyncCollider();
        RefreshIndicators();
    }

    private void Update()
    {
        if (indicators == null) return;
        indicators.SetActive(!IsDetailActive());
    }

    private bool IsDetailActive()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsEditorOpen) return false;
        var fl = GameManager.Instance.firstLayer;
        var sl = GameManager.Instance.secondLayer;
        return (fl != null && transform.parent == fl.transform)
            || (sl != null && transform.parent == sl.transform);
    }

    public void OnSnappedToSlot()
    {
        _inSlot = true;
        RefreshIndicators();
    }

    public void OnRemovedFromSlot()
    {
        _inSlot = false;
        RefreshIndicators();
    }

    private void SyncCollider()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.size   = sr.sprite.bounds.size;
            box.offset = sr.sprite.bounds.center;
            return;
        }

        PolygonCollider2D poly = GetComponent<PolygonCollider2D>();
        if (poly != null)
        {
            int count = sr.sprite.GetPhysicsShapeCount();
            poly.pathCount = count;
            var path = new System.Collections.Generic.List<Vector2>();
            for (int i = 0; i < count; i++)
            {
                path.Clear();
                sr.sprite.GetPhysicsShape(i, path);
                poly.SetPath(i, path);
            }
        }
    }

    private void OnEnable()
    {
        if (_started) RefreshIndicators();
    }

    public void RefreshCableSprite() => RefreshIndicators();

    private void RefreshIndicators()
    {
        for (int i = 0; i < cableIndicators.Length && i < indicatorSources.Length; i++)
        {
            if (cableIndicators[i] != null && indicatorSources[i] != null)
                cableIndicators[i].SetActive(_inSlot && CheckInstalled(indicatorSources[i]));
        }
    }

    private static bool CheckInstalled(GameObject go)
    {
        // CablePort: uses its own tracked state (never hierarchy-dependent)
        CablePort cs = go.GetComponent<CablePort>();
        if (cs != null) return cs.IsInstalled;

        // RAM: uses its own latch state
        RAMController ram = go.GetComponent<RAMController>();
        if (ram != null) return ram.IsInstalled;

        // GPU / SSD: in a slot when parented under a SlotContainer
        if (go.GetComponent<GPUController>() != null)
            return go.GetComponentInParent<SlotContainer>(true) != null;

        if (go.GetComponent<SSDController>() != null)
            return go.GetComponentInParent<SlotContainer>(true) != null;

        // CPU / Heatsink: try CPUSlotController first, fall back to SlotContainer
        if (go.GetComponent<CPUController>() != null)
        {
            CPUSlotController cpuSlot = go.GetComponentInParent<CPUSlotController>(true);
            if (cpuSlot != null) return cpuSlot.IsCPUInstalled;
            return go.GetComponentInParent<SlotContainer>(true) != null;
        }

        if (go.GetComponent<HeatsinkController>() != null)
        {
            CPUSlotController cpuSlot = go.GetComponentInParent<CPUSlotController>(true);
            if (cpuSlot != null) return cpuSlot.IsHeatsinkInstalled;
            return go.GetComponentInParent<SlotContainer>(true) != null;
        }

        // Fallback for components with no dedicated controller (e.g. CMOS):
        // consider installed if parented inside any slot
        return go.GetComponentInParent<SlotContainer>(true) != null
            || go.GetComponentInParent<CPUSlotController>(true) != null;
    }

    public void MarkInstalledInSystemUnit() => _wasEverInSystemUnit = true;
    public void MarkUninstalled() => IsUninstalledFromSystemUnit = true;
    public void MarkInstalled()
    {
        IsUninstalledFromSystemUnit = false;
        _wasEverInSystemUnit = false;
    }

    public bool IsPhase1CablesAndScrewsRemoved()
    {
        MotherboardPhaseManager phase = GetComponent<MotherboardPhaseManager>();
        Transform p1 = phase != null ? phase.GetPhase1Root() : transform;
        if (p1 == null) p1 = transform;
        foreach (var s in p1.GetComponentsInChildren<ScrewController>(true))
            if (!s.IsUnscrewed()) return false;
        foreach (var c in p1.GetComponentsInChildren<CablePort>(true))
            if (c.IsInstalled) return false;
        return true;
    }

    public bool IsPhase1CablesAndScrewsInstalled()
    {
        MotherboardPhaseManager phase = GetComponent<MotherboardPhaseManager>();
        Transform p1 = phase != null ? phase.GetPhase1Root() : transform;
        if (p1 == null) p1 = transform;
        foreach (var s in p1.GetComponentsInChildren<ScrewController>(true))
            if (!s.IsScrewed()) return false;
        foreach (var c in p1.GetComponentsInChildren<CablePort>(true))
            if (!c.IsInstalled) return false;
        return true;
    }

    public bool IsPhase1Complete()
    {
        if (!_wasEverInSystemUnit) return true;
        if (!IsUninstalledFromSystemUnit) return false;

        // Scope checks to Phase1 root only � avoids finding heatsink screws in Phase2
        MotherboardPhaseManager phase = GetComponent<MotherboardPhaseManager>();
        Transform phase1Root = phase != null ? phase.GetPhase1Root() : transform;
        if (phase1Root == null) phase1Root = transform;

        foreach (var s in phase1Root.GetComponentsInChildren<ScrewController>(true))
            if (!s.IsUnscrewed()) return false;

        foreach (var c in phase1Root.GetComponentsInChildren<CablePort>(true))
            if (c.IsInstalled) return false;

        // GPU must be fully removed from its slot before the motherboard can be removed
        GPUPhase1CableInteraction gpuPhase1 = phase?.GetGPUPhase1CableInteraction();
        if (gpuPhase1 != null && gpuPhase1.GetComponentInParent<SlotContainer>() != null)
            return false;

        return true;
    }
}