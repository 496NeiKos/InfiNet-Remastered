using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls state for one cable end (CableEnd1 or CableEnd2).
///
/// Hierarchy expected:
///   CableEnd (this)
///   ├── CableEnd_Body        (SpriteRenderer — always enabled, sprite never changes)
///   ├── CableEnd_Head        (GameObject — starts enabled; disabled when stripped)
///   ├── CableEnd_Wires       (container — starts disabled; enabled when stripped)
///   └── CableEnd_RJ45Slot    (starts disabled; enabled when RJ45 installed)
///
/// State flow: Intact → Stripped → Trimmed → RJ45 Installed → Crimped
/// Reset (crimper on body) always returns to Intact from any state.
/// </summary>
public class NetworkCableEndController : MonoBehaviour
{
    [Header("Cable Body")]
    [Tooltip("SpriteRenderer on the cable body — always enabled, sprite never changes.")]
    [SerializeField] private SpriteRenderer cableBody;

    [Header("Cable Head")]
    [Tooltip("The jacket tip GO — enabled when intact, disabled when stripped.")]
    [SerializeField] private GameObject cableHead;

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
    [Tooltip("Local X positions for wire slots 0-7 relative to wiresContainer.")]
    [SerializeField] private float[] slotLocalXPositions = new float[] { -0.4f, -0.3f, -0.2f, -0.1f, 0f, 0.1f, 0.2f, 0.3f };

    public bool IsStripped      { get; private set; }
    public bool IsTrimmed       { get; private set; }
    public bool IsRJ45Installed { get; private set; }
    public bool IsCrimped       { get; private set; }

    /// <summary>
    /// Increments each time Expose() is called.
    /// Used by NetworkCableTaskManager to distinguish Phase-1 from Phase-2 wire state.
    /// </summary>
    public int StripCycleCount { get; private set; }

    /// <summary>World position of the RJ45 slot object — used for proximity snap/crimp detection.</summary>
    public Vector3 SlotWorldPosition =>
        rj45SlotObject != null ? rj45SlotObject.transform.position : transform.position;

    /// <summary>World position of the cable body — used by the crimping tool's reset detection.</summary>
    public Vector3 BodyWorldPosition =>
        cableBody != null ? cableBody.transform.position : transform.position;

    /// <summary>World position of the wires container — used by the crimping tool's trim detection.</summary>
    public Vector3 WiresWorldPosition =>
        wiresContainer != null ? wiresContainer.position : transform.position;

    public int GetWireColorAtSlot(int slotIndex)
    {
        if (_wires == null) return -1;
        foreach (var wire in _wires)
            if (wire != null && wire.CurrentSlotIndex == slotIndex)
                return wire.wireColorIndex;
        return -1;
    }

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
        IsTrimmed       = false;
        IsRJ45Installed = false;
        IsCrimped       = false;

        if (cableHead      != null) cableHead.SetActive(true);
        if (wiresContainer != null) wiresContainer.gameObject.SetActive(false);
        if (rj45SlotObject != null) rj45SlotObject.SetActive(false);
    }

    /// <summary>
    /// Called by wire stripper. Disables the cable head and exposes the inner wires (randomized).
    /// Does nothing if already stripped.
    /// </summary>
    public void Expose()
    {
        if (IsStripped) return;
        IsStripped = true;
        StripCycleCount++;

        if (cableHead      != null) cableHead.SetActive(false);
        if (wiresContainer != null) wiresContainer.gameObject.SetActive(true);

        ShuffleWires();
        NetworkCableTaskManager.CheckConditions();
    }

    /// <summary>
    /// Called by the crimping tool on the wires area. Marks wires as trimmed (equal length).
    /// Requires the cable to be stripped first. Does nothing if already trimmed.
    /// </summary>
    public void Trim()
    {
        if (!IsStripped || IsTrimmed) return;
        IsTrimmed = true;
        NetworkCableTaskManager.CheckConditions();
    }

    /// <summary>
    /// Fully resets the cable end back to its intact state.
    /// Called by the crimping tool on the cable body. Works from any state.
    /// </summary>
    public void ResetEnd()
    {
        IsCrimped = false;
        if (IsRJ45Installed) UninstallRJ45(notify: false);

        IsStripped = false;
        IsTrimmed  = false;

        if (cableHead      != null) cableHead.SetActive(true);
        if (wiresContainer != null) wiresContainer.gameObject.SetActive(false);
        if (rj45SlotObject != null) rj45SlotObject.SetActive(false);

        NetworkCableTaskManager.CheckConditions();
    }

    /// <summary>
    /// Locks the installed RJ45 in place. Requires RJ45 to be installed and not yet crimped.
    /// </summary>
    public void Crimp()
    {
        if (!IsRJ45Installed || IsCrimped) return;
        IsCrimped = true;
        NetworkCableTaskManager.CheckConditions();
    }

    /// <summary>
    /// Reparents the actual RJ45 hardware object into the slot and plays the slide-in animation.
    /// Requires both IsStripped and IsTrimmed.
    /// </summary>
    public void InstallRJ45(NetworkHardwareHolder source = null)
    {
        if (!IsStripped || !IsTrimmed || IsRJ45Installed) return;
        if (source == null || source.hardwarePrefab == null) return;

        IsRJ45Installed = true;
        _installedRJ45  = source.hardwarePrefab;

        rj45SlotObject.SetActive(true);

        Vector3 worldScale = _installedRJ45.transform.lossyScale;
        _installedRJ45.SetActive(true);
        _installedRJ45.transform.SetParent(rj45SlotObject.transform, false);
        RestoreWorldScale(_installedRJ45.transform, worldScale);

        _installedRJ45.transform.localPosition = new Vector3(0f, slideStartYOffset, 0f);
        if (_slideRoutine != null) StopCoroutine(_slideRoutine);
        _slideRoutine = StartCoroutine(SlideRJ45(_installedRJ45, Vector3.zero));

        _installedRJ45.GetComponent<RJ45HoldUninstall>()?.OnInstalled(this);
        SetWiresInteractable(false);
        ActivityLogManager.Log($"RJ45 connector installed — {gameObject.name}", ActivityLogManager.EntryType.Install);
        NetworkCableTaskManager.CheckConditions();
    }

    /// <summary>
    /// Installs an already-active RJ45 GameObject into this slot.
    /// Used by the drag-uninstall path when the player drops the RJ45 onto a valid slot.
    /// Requires both IsStripped and IsTrimmed.
    /// </summary>
    public void InstallRJ45(GameObject rj45)
    {
        if (!IsStripped || !IsTrimmed || IsRJ45Installed || rj45 == null) return;

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

    public bool IsWireOrderCorrect()
    {
        if (_wires == null || _wires.Length != 8) return false;

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
