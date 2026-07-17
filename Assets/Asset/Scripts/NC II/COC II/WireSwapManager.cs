using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Singleton that manages click-to-swap wire selection.
/// First click selects a wire; second click on a different wire swaps them.
/// Second click on same wire deselects.
/// </summary>
public class WireSwapManager : MonoBehaviour
{
    public static WireSwapManager Instance { get; private set; }

    [Tooltip("Duration of the swap animation in seconds.")]
    [SerializeField] private float swapDuration = 0.3f;

    [Header("Selection Indicator")]
    [Tooltip("TMP text placed in a screen corner that shows the currently selected wire name.")]
    [SerializeField] private TMP_Text selectionLabel;
    [Tooltip("How long the swapped/cancelled message stays visible before fading out (seconds).")]
    [SerializeField] private float resultMessageDuration = 2f;

    private WireController _selected;
    private Coroutine _hideLabelRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        HideLabel();
    }

    private void Update()
    {
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        // OverlapPointAll returns every collider at the click point regardless of draw order.
        // We then pick the WireController with the highest sorting order (topmost visual layer).
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);

        WireController wire = null;
        int highestOrder = int.MinValue;
        foreach (Collider2D col in hits)
        {
            if (!col.gameObject.activeInHierarchy) continue;
            WireController w = col.GetComponent<WireController>();
            if (w == null) continue;
            SpriteRenderer sr = w.GetComponent<SpriteRenderer>();
            int order = sr != null ? sr.sortingOrder : 0;
            if (order > highestOrder) { highestOrder = order; wire = w; }
        }

        if (wire == null) return;
        OnWireClicked(wire);
    }

    public void OnWireClicked(WireController wire)
    {
        if (_selected == null)
        {
            Select(wire);
            return;
        }

        if (_selected == wire)
        {
            string cancelledName = ResolveName(_selected.wireName);
            Deselect();
            ShowTemporary($"Cancelled: {cancelledName}");
            return;
        }

        string nameA = ResolveName(_selected.wireName);
        string nameB = ResolveName(wire.wireName);
        Swap(_selected, wire);
        Deselect();
        ShowTemporary($"{nameA} and {nameB} swapped.");
    }

    private void Select(WireController wire)
    {
        _selected = wire;
        wire.SetHighlight(true);
        ShowPersistent($"Selected: {ResolveName(wire.wireName)}");
    }

    private void Deselect()
    {
        if (_selected != null) _selected.SetHighlight(false);
        _selected = null;
    }

    /// <summary>Clears selection and hides the label. Called externally when interaction is interrupted (e.g. RJ45 installed).</summary>
    public void ForceDeselect()
    {
        Deselect();
        HideLabel();
    }

    // Stays visible until the next action replaces or hides it.
    private void ShowPersistent(string message)
    {
        if (_hideLabelRoutine != null) StopCoroutine(_hideLabelRoutine);
        _hideLabelRoutine = null;

        if (selectionLabel == null) return;
        selectionLabel.text = message;
        selectionLabel.gameObject.SetActive(true);
    }

    // Visible for resultMessageDuration seconds, then hides.
    private void ShowTemporary(string message)
    {
        if (selectionLabel == null) return;
        selectionLabel.text = message;
        selectionLabel.gameObject.SetActive(true);

        if (_hideLabelRoutine != null) StopCoroutine(_hideLabelRoutine);
        _hideLabelRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(resultMessageDuration);
        HideLabel();
    }

    private void HideLabel()
    {
        if (selectionLabel == null) return;
        selectionLabel.gameObject.SetActive(false);
    }

    private static string ResolveName(string wireName) =>
        string.IsNullOrEmpty(wireName) ? "Wire" : wireName;

    private void Swap(WireController a, WireController b)
    {
        Vector3 posA = a.transform.localPosition;
        Vector3 posB = b.transform.localPosition;
        int slotA = a.CurrentSlotIndex;
        int slotB = b.CurrentSlotIndex;

        a.CurrentSlotIndex = slotB;
        b.CurrentSlotIndex = slotA;

        a.AnimateTo(posB, swapDuration);
        b.AnimateTo(posA, swapDuration);

        NetworkCableTaskManager.CheckConditions();
    }
}
