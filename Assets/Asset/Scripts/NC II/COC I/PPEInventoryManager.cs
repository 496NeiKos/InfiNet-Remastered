using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PPEInventoryManager : MonoBehaviour
{
    public static PPEInventoryManager Instance { get; private set; }

    [Header("Inventory Window")]
    [SerializeField] private GameObject inventoryPanel;

    [Header("PPE Items")]
    [Tooltip("All PPEItemSlot components in the inventory panel, in display order.")]
    [SerializeField] private PPEItemSlot[] ppeItems;

    [Header("Equipment Slots")]
    [Tooltip("All PPEEquipmentSlot components on the avatar panel.")]
    [SerializeField] private PPEEquipmentSlot[] equipmentSlots;

    [Header("Overview Panel")]
    [SerializeField] private Image overviewImage;
    [SerializeField] private TextMeshProUGUI overviewName;
    [SerializeField] private TextMeshProUGUI overviewDescription;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        inventoryPanel.SetActive(false);

        if (ppeItems != null && ppeItems.Length > 0)
            UpdateOverviewPanel(ppeItems[0]);
    }

    public bool IsOpen => inventoryPanel != null && inventoryPanel.activeSelf;

    public void OpenInventory()  => inventoryPanel.SetActive(true);
    public void CloseInventory() => inventoryPanel.SetActive(false);

    // ── Called by PPEItemSlot ─────────────────────────────────────────────

    public void OnItemClicked(PPEItemSlot item)
    {
        UpdateOverviewPanel(item);
    }

    public void OnItemDoubleClicked(PPEItemSlot item)
    {
        // Unequippable items: simple placed/unplaced toggle, no slot involved.
        if (item.IsUnequippable)
        {
            item.SetActive(!item.IsActive);
            NCIITaskListManager.CheckConditions();
            return;
        }

        if (item.IsActive)
        {
            // Find and release the slot holding this item.
            foreach (PPEEquipmentSlot slot in equipmentSlots)
            {
                if (slot.Occupant == item)
                {
                    slot.Release();
                    item.SetActive(false);
                    NCIITaskListManager.CheckConditions();
                    return;
                }
            }
            // Fallback: state mismatch — just unequip.
            item.SetActive(false);
            NCIITaskListManager.CheckConditions();
        }
        else
        {
            // Find the first available slot that accepts this item's tag.
            foreach (PPEEquipmentSlot slot in equipmentSlots)
            {
                if (!slot.IsOccupied && slot.CanAccept(item.SlotTag))
                {
                    slot.TryOccupy(item);
                    item.SetActive(true);
                    NCIITaskListManager.CheckConditions();
                    return;
                }
            }

            // All matching slots are full — shake the item and every matching slot.
            item.Shake();
            foreach (PPEEquipmentSlot slot in equipmentSlots)
            {
                if (slot.CanAccept(item.SlotTag))
                    slot.Shake();
            }
        }
    }

    // ── Overview Panel ────────────────────────────────────────────────────

    private void UpdateOverviewPanel(PPEItemSlot item)
    {
        if (item == null) return;
        if (overviewImage != null)       overviewImage.sprite   = item.ItemSprite;
        if (overviewName != null)        overviewName.text       = item.ItemName;
        if (overviewDescription != null) overviewDescription.text = item.ItemDescription;
    }
}
