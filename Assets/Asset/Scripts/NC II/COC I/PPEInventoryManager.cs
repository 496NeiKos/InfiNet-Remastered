using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PPEInventoryManager : MonoBehaviour
{
    public static PPEInventoryManager Instance { get; private set; }

    [Header("Inventory Window")]
    [SerializeField] private GameObject inventoryPanel;

    [Header("PPE Item Buttons (7)")]
    [Tooltip("Order: ESD Strap, ESD Mat, Safety Glasses, Protective Gloves, Safety Shoes, Dust Mask, Protective Clothing")]
    [SerializeField] private Button[] ppeButtons;
    [Tooltip("Border overlay Image child on each button — enabled when equipped.")]
    [SerializeField] private Image[] ppeBorderImages;
    [Tooltip("Status text overlay child on each button — shows 'Equipped' or 'Placed'.")]
    [SerializeField] private TextMeshProUGUI[] ppeStatusTexts;

    [Header("Avatar Equipment Slot Images (6)")]
    [Tooltip("Order: ESD Strap, Safety Glasses, Protective Gloves, Safety Shoes, Dust Mask, Protective Clothing")]
    [SerializeField] private Image[] avatarSlotImages;

    [Header("Visual Settings")]
    [SerializeField] private Color equippedBorderColor   = new Color(0.2f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color unequippedButtonColor = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] private Color slotEquippedTint      = Color.white;
    [SerializeField] private Color slotUnequippedTint    = new Color(0.35f, 0.35f, 0.35f, 1f);

    // ESD Mat (index 1) has no avatar slot and uses "Placed" instead of "Equipped".
    private const int EsdMatIndex = 1;

    // PPE index → avatar slot index; -1 means no slot (ESD Mat).
    // 0=ESD Strap→0, 1=ESD Mat→-1, 2=Safety Glasses→1, 3=Gloves→2,
    // 4=Safety Shoes→3, 5=Dust Mask→4, 6=Protective Clothing→5
    private static readonly int[] SlotMapping = { 0, -1, 1, 2, 3, 4, 5 };

    private bool[] _equipped;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _equipped = new bool[7];
    }

    private void Start()
    {
        for (int i = 0; i < ppeButtons.Length; i++)
        {
            int captured = i;
            ppeButtons[i].onClick.AddListener(() => OnPPEButtonClicked(captured));
        }

        RefreshAll();
        inventoryPanel.SetActive(false);
    }

    public void OpenInventory()  => inventoryPanel.SetActive(true);
    public void CloseInventory() => inventoryPanel.SetActive(false);

    public bool AreAllPPEEquipped()
    {
        for (int i = 0; i < _equipped.Length; i++)
            if (!_equipped[i]) return false;
        return true;
    }

    private void OnPPEButtonClicked(int index)
    {
        _equipped[index] = !_equipped[index];
        RefreshButton(index);

        int slot = SlotMapping[index];
        if (slot >= 0)
            avatarSlotImages[slot].color = _equipped[index] ? slotEquippedTint : slotUnequippedTint;

        NCIITaskListManager.CheckConditions();
    }

    private void RefreshButton(int index)
    {
        bool on = _equipped[index];

        // Color-swap instead of enabled toggle — keeps the button background always visible.
        // If ppeBorderImages points to a dedicated child border overlay, swap to enabled/disabled instead.
        ppeBorderImages[index].color = on ? equippedBorderColor : unequippedButtonColor;

        ppeStatusTexts[index].text = (index == EsdMatIndex) ? "Placed" : "Equipped";
        ppeStatusTexts[index].gameObject.SetActive(on);
    }

    private void RefreshAll()
    {
        for (int i = 0; i < 7; i++)
            RefreshButton(i);

        for (int i = 0; i < avatarSlotImages.Length; i++)
            avatarSlotImages[i].color = slotUnequippedTint;
    }
}
