using TMPro;
using UnityEngine;

/// <summary>
/// Small UI panel that shows available angles for the current hardware detail and highlights
/// which one is active. Place this inside firstLayer (or secondLayer for GPU).
/// Assign up to 4 TMP_Text slots in the Inspector — one per possible angle.
/// Call Setup() when a hardware detail opens, SetActive() whenever the angle changes.
/// </summary>
public class HardwareAngleIndicator : MonoBehaviour
{
    [SerializeField] private TMP_Text[] slots = new TMP_Text[4];

    [Header("Appearance")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private int _count;

    /// <summary>
    /// Shows the panel and labels each slot. Slots beyond the labels array are hidden.
    /// Labels are prefixed with their key number: "[1] Front", "[2] Side", etc.
    /// </summary>
    public void Setup(string[] labels)
    {
        _count = Mathf.Min(labels.Length, slots.Length);
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            bool visible = i < _count;
            slots[i].gameObject.SetActive(visible);
            if (visible)
            {
                slots[i].text = $"[{i + 1}] {labels[i]}";
                slots[i].color = inactiveColor;
            }
        }
        gameObject.SetActive(true);
    }

    /// <summary>Highlights the slot at the given index; dims all others.</summary>
    public void SetActive(int index)
    {
        for (int i = 0; i < _count && i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].color = i == index ? activeColor : inactiveColor;
        }
    }

    public void Hide() => gameObject.SetActive(false);
}
