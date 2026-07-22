using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the ROOT of each connectable device (Computer, Router, PatchPanel, Switch)
/// alongside NetworkDevicePort.
///
/// Manages the sequential queue of Phase2 cable installs for this device.
/// Each time a LogicalCableNetwork (Phase1) connects to this device, a new Phase2
/// cable prefab is instantiated inside the Detail view. Only the current queue head
/// is visible and draggable — all others are hidden until their turn arrives.
///
/// Queue rules (strict sequential):
///   • [0] must be installed before [1] becomes active.
///   • Uninstalling [i] from its socket returns [i] to the anchor and hides [i+1..n].
///   • Removing Phase1 cable (Disconnect) is blocked while any Phase2 is installed;
///     call IsPhase2InstalledFor() first to enforce that constraint.
/// </summary>
public class NetworkDevicePhase2Manager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("This device's NetworkDevicePort — used for display name resolution.")]
    [SerializeField] private NetworkDevicePort ownerPort;

    [Header("Phase2 Prefab")]
    [Tooltip("Prefab with NetworkLogicalCablePhase2, SpriteRenderer, Collider2D, and a TMP label child.")]
    [SerializeField] private GameObject phase2Prefab;

    [Header("Detail View")]
    [Tooltip("The Detail child GameObject (the one NetworkPrefabInteraction activates on right-click). Phase2 instances are parented here.")]
    [SerializeField] private Transform detailViewParent;

    [Tooltip("Transform inside the Detail view where idle Phase2 cables appear. Position this yourself in the scene.")]
    [SerializeField] private Transform anchorTransform;

    [Header("Shared Label (Screen Space - Camera Canvas)")]
    [Tooltip("TextMeshProUGUI on a Screen Space - Camera canvas in the scene. Updated to show which connection the active Phase2 cable represents.")]
    [SerializeField] private TextMeshProUGUI sharedLabel;

    // ----------------------------------------------------------------
    //  Internal queue entry
    // ----------------------------------------------------------------

    private class Phase2Entry
    {
        public NetworkLogicalCable        Cable;
        public NetworkDevicePort          OtherPort;
        public NetworkLogicalCablePhase2  Instance;
        public bool                       IsInstalled;
        public string                     LabelText;
    }

    private readonly List<Phase2Entry> _entries      = new List<Phase2Entry>();
    private          int               _currentIndex = 0;
    private          bool              _detailWasOpen = false;

    // ----------------------------------------------------------------
    //  Update — track detail view open/close transitions for shared label
    // ----------------------------------------------------------------

    private void Update()
    {
        if (sharedLabel == null) return;

        bool detailOpen = detailViewParent != null && detailViewParent.gameObject.activeInHierarchy;

        if (detailOpen == _detailWasOpen) return; // no state change
        _detailWasOpen = detailOpen;

        if (detailOpen)
            UpdateSharedLabel();   // restore label visibility based on queue
        else
            sharedLabel.gameObject.SetActive(false); // panel closed — hide unconditionally
    }

    // ----------------------------------------------------------------
    //  Public query — used by NetworkLogicalCable.Disconnect to gate Phase1 removal
    // ----------------------------------------------------------------

    public bool IsPhase2InstalledFor(NetworkLogicalCable cable)
    {
        foreach (Phase2Entry e in _entries)
            if (e.Cable == cable && e.IsInstalled) return true;
        return false;
    }

    // ----------------------------------------------------------------
    //  Called by NetworkLogicalCable.AttachSecondEnd
    // ----------------------------------------------------------------

    public void RegisterCable(NetworkLogicalCable cable, NetworkDevicePort otherPort)
    {
        if (phase2Prefab == null || detailViewParent == null || anchorTransform == null)
        {
            Debug.LogWarning($"[NetworkDevicePhase2Manager:{name}] Missing prefab/detailViewParent/anchor — skipping Phase2 for this cable.");
            return;
        }

        GameObject go = Instantiate(phase2Prefab, detailViewParent);
        var phase2 = go.GetComponent<NetworkLogicalCablePhase2>();
        if (phase2 == null)
        {
            Debug.LogWarning($"[NetworkDevicePhase2Manager:{name}] Prefab is missing NetworkLogicalCablePhase2 component.");
            Destroy(go);
            return;
        }

        string ownerName = GetDisplayName(ownerPort != null ? ownerPort.gameObject : gameObject);
        string otherName = GetDisplayName(otherPort != null ? otherPort.gameObject : null);
        string labelText = $"{ownerName} → {otherName}";

        phase2.Initialize(this, cable, anchorTransform, detailViewParent);

        var entry = new Phase2Entry
        {
            Cable       = cable,
            OtherPort   = otherPort,
            Instance    = phase2,
            IsInstalled = false,
            LabelText   = labelText
        };
        _entries.Add(entry);

        // Only visible if it is the current queue head.
        go.SetActive(_entries.Count - 1 == _currentIndex);
    }

    // ----------------------------------------------------------------
    //  Called by NetworkLogicalCable.Disconnect (Phase1 removal path)
    //  Only reached when Phase2 is NOT installed (Disconnect blocks otherwise).
    // ----------------------------------------------------------------

    public void UnregisterCable(NetworkLogicalCable cable)
    {
        int idx = _entries.FindIndex(e => e.Cable == cable);
        if (idx < 0) return;

        Phase2Entry entry = _entries[idx];
        if (entry.Instance != null)
            Destroy(entry.Instance.gameObject);

        _entries.RemoveAt(idx);

        // Keep currentIndex consistent after removal.
        if (idx < _currentIndex)
            _currentIndex--;
        else if (idx == _currentIndex)
            _currentIndex = Mathf.Clamp(_currentIndex, 0, _entries.Count);

        RefreshQueue();
    }

    // ----------------------------------------------------------------
    //  Called by NetworkLogicalCablePhase2 after snapping to a socket
    // ----------------------------------------------------------------

    public void OnPhase2Installed(NetworkLogicalCablePhase2 instance)
    {
        int idx = _entries.FindIndex(e => e.Instance == instance);
        if (idx < 0) return;

        _entries[idx].IsInstalled = true;
        _currentIndex = idx + 1;

        ActivityLogManager.Log(
            $"Port cable installed on {GetDisplayName(gameObject)} ({_entries[idx].OtherPort?.name ?? "device"})",
            ActivityLogManager.EntryType.Install);

        RefreshQueue();
    }

    // ----------------------------------------------------------------
    //  Called by NetworkLogicalCablePhase2 after socket hold-uninstall
    // ----------------------------------------------------------------

    public void OnPhase2Uninstalled(NetworkLogicalCablePhase2 instance)
    {
        int idx = _entries.FindIndex(e => e.Instance == instance);
        if (idx < 0) return;

        _entries[idx].IsInstalled = false;

        // Strict sequential: hide everything after this index.
        for (int i = idx + 1; i < _entries.Count; i++)
        {
            if (!_entries[i].IsInstalled && _entries[i].Instance != null)
                _entries[i].Instance.gameObject.SetActive(false);
        }

        _currentIndex = idx;

        ActivityLogManager.Log(
            $"Port cable removed from {GetDisplayName(gameObject)} ({_entries[idx].OtherPort?.name ?? "device"})",
            ActivityLogManager.EntryType.Remove);

        RefreshQueue();
    }

    // ----------------------------------------------------------------
    //  Visibility control
    // ----------------------------------------------------------------

    private void RefreshQueue()
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            Phase2Entry e = _entries[i];
            if (e.Instance == null) continue;

            if (e.IsInstalled)
            {
                // Installed cables stay visible in their sockets.
                e.Instance.gameObject.SetActive(true);
            }
            else if (i == _currentIndex)
            {
                // Current pending — show at anchor.
                e.Instance.ReturnToAnchor();
                e.Instance.gameObject.SetActive(true);
            }
            else
            {
                // Not yet active — hide.
                e.Instance.gameObject.SetActive(false);
            }
        }

        UpdateSharedLabel();
    }

    private void UpdateSharedLabel()
    {
        if (sharedLabel == null) return;

        bool detailOpen = detailViewParent != null && detailViewParent.gameObject.activeInHierarchy;
        if (!detailOpen)
        {
            sharedLabel.gameObject.SetActive(false);
            return;
        }

        // Show the label only when there is a pending (non-installed) active entry.
        if (_currentIndex < _entries.Count && !_entries[_currentIndex].IsInstalled)
        {
            sharedLabel.text = _entries[_currentIndex].LabelText;
            sharedLabel.gameObject.SetActive(true);
        }
        else
        {
            sharedLabel.gameObject.SetActive(false);
        }
    }

    // ----------------------------------------------------------------
    //  Helpers
    // ----------------------------------------------------------------

    private static string GetDisplayName(GameObject go)
    {
        if (go == null) return "Unknown";
        var drag = go.GetComponent<NetworkDragPrefab>();
        return drag != null ? drag.LogDisplayName : go.name;
    }
}
