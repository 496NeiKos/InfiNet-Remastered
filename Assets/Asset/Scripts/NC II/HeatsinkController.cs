using UnityEngine;

public class HeatsinkController : MonoBehaviour
{
    [Header("Root Sprites (cable state)")]
    [SerializeField] private Sprite rootConnectedSprite;
    [SerializeField] private Sprite rootDisconnectedSprite;

    private SpriteRenderer _sr;
    private CPUSlotController _cpuSlot;
    private Vector3 _installedLocalScale;
    private Vector3 _installedLocalPosition;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        _cpuSlot = GetComponentInParent<CPUSlotController>();
        // Capture transform while correctly parented under CPUSlot
        _installedLocalScale = transform.localScale;
        _installedLocalPosition = transform.localPosition;
    }

    public void UpdateRootSprite(bool cableConnected)
    {
        if (_sr == null) return;
        _sr.sprite = cableConnected ? rootConnectedSprite : rootDisconnectedSprite;
    }

    public Vector3 InstalledLocalScale => _installedLocalScale;
    public Vector3 InstalledLocalPosition => _installedLocalPosition;
    public bool IsInstalledInSlot => GetComponentInParent<CPUSlotController>()?.IsHeatsinkInstalled ?? false;

    // Cable must be disconnected (via HeatsinkCableConnector slide gesture) before drag-out is allowed.
    public bool CanBeRemoved
    {
        get
        {
            HeatsinkCableConnector connector = GetComponentInChildren<HeatsinkCableConnector>(true);
            return connector == null || !connector.IsConnected;
        }
    }

    // True when in slot, fan cable is connected (slid up), and all screws are tightened.
    public bool IsFullyInstalled
    {
        get
        {
            if (!IsInstalledInSlot) return false;
            HeatsinkCableConnector connector = GetComponentInChildren<HeatsinkCableConnector>(true);
            if (connector != null && !connector.IsConnected) return false;
            foreach (var sc in GetComponentsInChildren<ScrewController>(true))
                if (!sc.IsScrewed()) return false;
            return true;
        }
    }

    // Called by DragPrefab.OnEndDrag � slot ref passed directly since Heatsink
    // may have already moved away from CPUSlot hierarchy by this point
    public void OnRemovedFromSlot(CPUSlotController slot)
    {
        CPUSlotController target = slot != null ? slot : _cpuSlot;
        if (target != null)
        {
            target.OnHeatsinkUninstalled();
            Debug.Log("[HeatsinkController] Notified CPUSlotController: heatsink uninstalled.");
        }
        else
        {
            Debug.LogWarning("[HeatsinkController] No CPUSlotController found � state not updated.");
        }
    }

    // Called by HardwareHolder.TryInstallInSlot when reinstalled from hardware area
    public void OnInstalledToSlot(CPUSlotController slot)
    {
        _cpuSlot = slot;
        _cpuSlot?.OnHeatsinkInstalled();
    }
}