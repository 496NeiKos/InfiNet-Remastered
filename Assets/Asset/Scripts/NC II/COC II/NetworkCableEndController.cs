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
    [Header("Sprite References")]
    [SerializeField] private GameObject defaultSprite;
    [SerializeField] private GameObject strippedSprite;

    [Header("Wires")]
    [SerializeField] private Transform wiresContainer;

    [Header("RJ45 Slot")]
    [SerializeField] private GameObject rj45SlotObject;

    [Header("Slot Layout")]
    [Tooltip("World-space X positions for wire slots 0-7 (relative to wiresContainer parent).")]
    [SerializeField] private float[] slotLocalXPositions = new float[] { -1.75f, -1.25f, -0.75f, -0.25f, 0.25f, 0.75f, 1.25f, 1.75f };

    public bool IsStripped { get; private set; }
    public bool IsRJ45Installed { get; private set; }

    private WireController[] _wires;

    private void Awake()
    {
        if (wiresContainer != null)
            _wires = wiresContainer.GetComponentsInChildren<WireController>(true);
    }

    private void Start()
    {
        if (strippedSprite != null) strippedSprite.SetActive(false);
        if (wiresContainer != null) wiresContainer.gameObject.SetActive(false);
        if (rj45SlotObject != null) rj45SlotObject.SetActive(false);
    }

    /// <summary>Called by wire stripper on a fresh cable end. Enables stripped view and shuffles wires.</summary>
    public void Expose()
    {
        if (IsStripped) return;
        IsStripped = true;

        if (defaultSprite != null) defaultSprite.SetActive(false);
        if (strippedSprite != null) strippedSprite.SetActive(true);
        if (wiresContainer != null) wiresContainer.gameObject.SetActive(true);

        ShuffleWires();
        NetworkCableTaskManager.CheckConditions();
    }

    /// <summary>Called by wire stripper on an already-stripped cable end. Uninstalls RJ45 and re-shuffles wires.</summary>
    public void ResetEnd()
    {
        if (IsRJ45Installed) UninstallRJ45(notify: false);

        ShuffleWires();
        NetworkCableTaskManager.CheckConditions();
    }

    public void InstallRJ45()
    {
        if (!IsStripped || IsRJ45Installed) return;
        IsRJ45Installed = true;
        if (rj45SlotObject != null) rj45SlotObject.SetActive(true);
        NetworkCableTaskManager.CheckConditions();
    }

    public void UninstallRJ45(bool notify = true)
    {
        IsRJ45Installed = false;
        if (rj45SlotObject != null) rj45SlotObject.SetActive(false);
        if (notify) NetworkCableTaskManager.CheckConditions();
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
