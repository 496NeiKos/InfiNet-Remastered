using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls state for one cable end (CableEnd1 or CableEnd2).
/// Manages sprite toggling, wire shuffle, and RJ45 slot install/uninstall.
///
/// Hierarchy expected:
///   CableEnd (this)
///   ├── CableEnd_DefaultSprite   (SpriteRenderer, active initially)
///   ├── CableEnd_StrippedSprite  (SpriteRenderer, disabled initially)
///   ├── CableEnd_Wires           (container, disabled initially — 8 WireController children)
///   └── CableEnd_RJ45Slot        (empty slot object, disabled initially)
/// </summary>
public class NetworkCableEndController : MonoBehaviour
{
    [Header("Cable Body")]
    [Tooltip("SpriteRenderer on Cable1Body (the single merged body object).")]
    [SerializeField] private SpriteRenderer cableBody;
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite strippedSprite;

    [Header("Wires")]
    [SerializeField] private Transform wiresContainer;

    [Header("RJ45 Slot")]
    [SerializeField] private GameObject rj45SlotObject;

    [Header("RJ45 Install Animation")]
    [Tooltip("How far above the slot's resting position the RJ45 starts before sliding down (local Y units).")]
    [SerializeField] private float slideStartYOffset = 1f;
    [Tooltip("Duration of the slide-down animation in seconds.")]
    [SerializeField] private float slideDuration = 0.3f;

    [Header("Slot Layout")]
    [Tooltip("Local X positions for wire slots 0-7 relative to wiresContainer. Match the editor wire spacing.")]
    [SerializeField] private float[] slotLocalXPositions = new float[] { -0.4f, -0.3f, -0.2f, -0.1f, 0f, 0.1f, 0.2f, 0.3f };

    public bool IsStripped      { get; private set; }
    public bool IsRJ45Installed { get; private set; }
    public bool IsCrimped       { get; private set; }

    /// <summary>
    /// Increments each time Expose() is called.
    /// 0 = never stripped, 1 = Phase-1 strip, 2 = Phase-2 re-strip, etc.
    /// Used by NetworkCableTaskManager to distinguish Phase-1 from Phase-2 wire state.
    /// </summary>
    public int StripCycleCount { get; private set; }

    /// <summary>World position of the RJ45 slot object — used for proximity snap detection.</summary>
    public Vector3 SlotWorldPosition =>
        rj45SlotObject != null ? rj45SlotObject.transform.position : transform.position;

    /// <summary>World position of the cable body — used by the crimping tool's cut/reset detection.</summary>
    public Vector3 BodyWorldPosition =>
        cableBody != null ? cableBody.transform.position : transform.position;

    /// <summary>
    /// Returns the wireColorIndex of whichever wire currently occupies the given slot (0–7),
    /// or -1 if no wire is assigned there. Used by LanTesterLEDDisplay for per-pin LED checking.
    /// </summary>
    public int GetWireColorAtSlot(int slotIndex)
    {
        if (_wires == null) return -1;
        foreach (var wire in _wires)
            if (wire != null && wire.CurrentSlotIndex == slotIndex)
                return wire.wireColorIndex;
        return -1;
    }

    /// <summary>
    /// Reverse lookup — returns the slot index currently occupied by the wire with the given color,
    /// or -1 if no wire with that color exists. Used by LanTesterLEDDisplay remote panel to trace
    /// which pin on this end receives the signal the master is sending on a given pin.
    /// </summary>
    public int GetSlotForWireColor(int colorIndex)
    {
        if (_wires == null || colorIndex < 0) return -1;
        foreach (var wire in _wires)
            if (wire != null && wire.wireColorIndex == colorIndex)
                return wire.CurrentSlotIndex;
        return -1;
    }

    private WireController[] _wires;
    private GameObject _installedRJ45;
    private Coroutine _slideRoutine;

    private void Awake()
    {
        if (wiresContainer != null)
            _wires = wiresContainer.GetComponentsInChildren<WireController>(true);
    }

    private void Start()
    {
        IsStripped      = false;
        IsRJ45Installed = false;
        IsCrimped       = false;
        if (cableBody      != null) cableBody.sprite = defaultSprite;
        if (wiresContainer != null) wiresContainer.gameObject.SetActive(false);
        if (rj45SlotObject != null) rj45SlotObject.SetActive(false);
    }

    /// <summary>Called by wire stripper on a fresh cable end. Enables stripped view and shuffles wires.</summary>
    public void Expose()
    {
        if (IsStripped) return;
        IsStripped = true;
        StripCycleCount++;

        if (cableBody      != null) cableBody.sprite = strippedSprite;
        if (wiresContainer != null) wiresContainer.gameObject.SetActive(true);

        ShuffleWires();
        NetworkCableTaskManager.CheckConditions();
    }

    /// <summary>
    /// Cuts the cable end back to its original unstripped state.
    /// Called by the crimping tool when aimed at the cable body (not the RJ45 slot).
    /// Works even if the RJ45 is already crimped — the connector is returned to hardware storage.
    /// The wire stripper must be used again to re-strip and get new randomized wires.
    /// </summary>
    public void ResetEnd()
    {
        IsCrimped = false; // Clear before UninstallRJ45 so its guard doesn't block.
        if (IsRJ45Installed) UninstallRJ45(notify: false);

        IsStripped = false;
        if (cableBody      != null) cableBody.sprite = defaultSprite;
        if (wiresContainer != null) wiresContainer.gameObject.SetActive(false);
        if (rj45SlotObject != null) rj45SlotObject.SetActive(false);

        NetworkCableTaskManager.CheckConditions();
    }

    /// <summary>
    /// Locks the installed RJ45 in place. Requires wire stripper to reset.
    /// Called by the crimping tool after a successful snap-install.
    /// </summary>
    public void Crimp()
    {
        if (!IsRJ45Installed || IsCrimped) return;
        IsCrimped = true;
        NetworkCableTaskManager.CheckConditions();
    }

    /// <summary>
    /// Reparents the actual RJ45 hardware object into the slot and plays the slide-in animation.
    /// Called by NetworkHardwareHolder.SnapToSlot.
    /// </summary>
    public void InstallRJ45(NetworkHardwareHolder source = null)
    {
        if (!IsStripped || IsRJ45Installed) return;
        if (source == null || source.hardwarePrefab == null) return;

        IsRJ45Installed = true;
        _installedRJ45  = source.hardwarePrefab;

        rj45SlotObject.SetActive(true);

        // Preserve world scale across the reparent
        Vector3 worldScale = _installedRJ45.transform.lossyScale;
        _installedRJ45.SetActive(true);
        _installedRJ45.transform.SetParent(rj45SlotObject.transform, false);
        RestoreWorldScale(_installedRJ45.transform, worldScale);

        // Slide from above (slideStartYOffset) down to rest (0,0,0) in slot-local space
        _installedRJ45.transform.localPosition = new Vector3(0f, slideStartYOffset, 0f);
        if (_slideRoutine != null) StopCoroutine(_slideRoutine);
        _slideRoutine = StartCoroutine(SlideRJ45(_installedRJ45, Vector3.zero));

        _installedRJ45.GetComponent<RJ45HoldUninstall>()?.OnInstalled(this);
        SetWiresInteractable(false);
        NetworkCableTaskManager.CheckConditions();
    }

    /// <summary>
    /// Uninstalls the RJ45 from the slot and returns it to hardware storage,
    /// restoring the icon proxy. Called by the wire-stripper reset path.
    /// </summary>
    public void UninstallRJ45(bool notify = true)
    {
        if (IsCrimped) return;
        IsRJ45Installed = false;

        if (_slideRoutine != null) { StopCoroutine(_slideRoutine); _slideRoutine = null; }

        if (_installedRJ45 != null)
        {
            _installedRJ45.GetComponent<RJ45HoldUninstall>()?.StoreImmediately();
            _installedRJ45 = null;
        }

        rj45SlotObject.SetActive(false);
        SetWiresInteractable(true);
        if (notify) NetworkCableTaskManager.CheckConditions();
    }

    /// <summary>
    /// Marks the slot as uninstalled and disables the slot object WITHOUT touching the RJ45
    /// GameObject — the caller (RJ45HoldUninstall) has already reparented it to the world root
    /// so disabling rj45SlotObject will not hide it.
    /// Returns false if the slot is crimped (uninstall blocked).
    /// </summary>
    public bool DetachRJ45ForDrag()
    {
        if (IsCrimped || !IsRJ45Installed) return false;

        IsRJ45Installed = false;
        if (_slideRoutine != null) { StopCoroutine(_slideRoutine); _slideRoutine = null; }

        _installedRJ45 = null;
        rj45SlotObject.SetActive(false);
        SetWiresInteractable(true);
        NetworkCableTaskManager.CheckConditions();
        return true;
    }

    /// <summary>
    /// Installs an already-active RJ45 GameObject into this slot.
    /// Used by the drag-uninstall path when the player drops the RJ45 onto a valid slot
    /// or snaps it back to its home slot — no NetworkHardwareHolder needed.
    /// </summary>
    public void InstallRJ45(GameObject rj45)
    {
        if (!IsStripped || IsRJ45Installed || rj45 == null) return;

        IsRJ45Installed = true;
        _installedRJ45  = rj45;

        rj45SlotObject.SetActive(true);

        Vector3 worldScale = rj45.transform.lossyScale;
        rj45.SetActive(true);
        rj45.transform.SetParent(rj45SlotObject.transform, false);
        RestoreWorldScale(rj45.transform, worldScale);

        rj45.transform.localPosition = new Vector3(0f, slideStartYOffset, 0f);
        if (_slideRoutine != null) StopCoroutine(_slideRoutine);
        _slideRoutine = StartCoroutine(SlideRJ45(rj45, Vector3.zero));

        rj45.GetComponent<RJ45HoldUninstall>()?.OnInstalled(this);
        SetWiresInteractable(false);
        NetworkCableTaskManager.CheckConditions();
    }

private IEnumerator SlideRJ45(GameObject rj45, Vector3 restLocalPos)
    {
        Vector3 start   = rj45.transform.localPosition;
        float   elapsed = 0f;
        while (elapsed < slideDuration)
        {
            if (rj45 == null) yield break;
            elapsed += Time.deltaTime;
            rj45.transform.localPosition = Vector3.Lerp(start, restLocalPos,
                Mathf.SmoothStep(0f, 1f, elapsed / slideDuration));
            yield return null;
        }
        if (rj45 != null) rj45.transform.localPosition = restLocalPos;
        _slideRoutine = null;
    }

    private static void RestoreWorldScale(Transform t, Vector3 targetWorldScale)
    {
        t.localScale = Vector3.one;
        Vector3 ls = t.lossyScale;
        t.localScale = new Vector3(
            targetWorldScale.x / (ls.x != 0f ? ls.x : 1f),
            targetWorldScale.y / (ls.y != 0f ? ls.y : 1f),
            targetWorldScale.z / (ls.z != 0f ? ls.z : 1f));
    }

    private void SetWiresInteractable(bool interactable)
    {
        if (_wires == null) return;
        foreach (var wire in _wires)
        {
            Collider2D col = wire.GetComponent<Collider2D>();
            if (col != null) col.enabled = interactable;
        }
        if (!interactable)
            WireSwapManager.Instance?.ForceDeselect();
    }

    /// <summary>
    /// Returns true if the wires match either T568A or T568B standard.
    /// T568A slot order by wireColorIndex: 0,1,2,3,4,5,6,7
    /// T568B slot order by wireColorIndex: 2,5,0,3,4,1,6,7
    /// </summary>
    public bool IsWireOrderCorrect()
    {
        if (_wires == null || _wires.Length != 8) return false;

        // Build a map: slotIndex → wireColorIndex
        int[] slotToColor = new int[8];
        foreach (var w in _wires)
            slotToColor[w.CurrentSlotIndex] = w.wireColorIndex;

        int[] t568a = { 0, 1, 2, 3, 4, 5, 6, 7 };
        int[] t568b = { 2, 5, 0, 3, 4, 1, 6, 7 };

        return Matches(slotToColor, t568a) || Matches(slotToColor, t568b);
    }

    private bool Matches(int[] actual, int[] expected)
    {
        for (int i = 0; i < 8; i++)
            if (actual[i] != expected[i]) return false;
        return true;
    }

    private void ShuffleWires()
    {
        if (_wires == null || _wires.Length == 0) return;

        // Fisher-Yates shuffle of slot assignments
        List<int> slots = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };
        for (int i = slots.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (slots[i], slots[j]) = (slots[j], slots[i]);
        }

        for (int i = 0; i < _wires.Length && i < slots.Count; i++)
        {
            _wires[i].CurrentSlotIndex = slots[i];
            float x = slots[i] < slotLocalXPositions.Length ? slotLocalXPositions[slots[i]] : 0f;
            _wires[i].transform.localPosition = new Vector3(x, _wires[i].transform.localPosition.y, _wires[i].transform.localPosition.z);
        }
    }
}
