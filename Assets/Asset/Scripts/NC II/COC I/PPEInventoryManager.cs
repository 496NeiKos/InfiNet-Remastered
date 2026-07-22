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
    [Tooltip("PPE_Slot_BG Image on each button's parent — sprite swaps when equipped.")]
    [SerializeField] private Image[] ppeSlotBGImages;
    [Tooltip("Status text overlay child on each button — shows 'Equipped' or 'Placed'.")]
    [SerializeField] private TextMeshProUGUI[] ppeStatusTexts;

    [Header("Avatar Equipment Slot Images (6)")]
    [Tooltip("Order: ESD Strap, Safety Glasses, Protective Gloves, Safety Shoes, Dust Mask, Protective Clothing")]
    [SerializeField] private Image[] avatarSlotImages;
    [Tooltip("PPE_EquipmentSlot_BG Image in each avatar slot — sprite swaps when equipped.")]
    [SerializeField] private Image[] avatarEquipmentSlotBGImages;

    [Header("Slot BG Sprites")]
    [SerializeField] private Sprite slotBGEquippedSprite;
    [SerializeField] private Sprite equipSlotBGEquippedSprite;

    [Header("Visual Settings")]
    [SerializeField] private Color slotEquippedTint   = Color.white;
    [SerializeField] private Color slotUnequippedTint = new Color(0.35f, 0.35f, 0.35f, 1f);

    // ESD Mat (index 1) has no avatar slot and uses "Placed" instead of "Equipped".
    private const int EsdMatIndex = 1;

    // PPE index → avatar slot index; -1 means no slot (ESD Mat).
    // 0=ESD Strap→0, 1=ESD Mat→-1, 2=Safety Glasses→1, 3=Gloves→2,
    // 4=Safety Shoes→3, 5=Dust Mask→4, 6=Protective Clothing→5
    private static readonly int[] SlotMapping = { 0, -1, 1, 2, 3, 4, 5 };

    private bool[] _equipped;
    private Sprite[] _defaultSlotBGSprites;
    private Sprite[] _defaultEquipSlotBGSprites;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _equipped = new bool[7];
    }

    private void Start()
    {
        _defaultSlotBGSprites = new Sprite[ppeSlotBGImages.Length];
        for (int i = 0; i < ppeSlotBGImages.Length; i++)
            _defaultSlotBGSprites[i] = ppeSlotBGImages[i].sprite;

        _defaultEquipSlotBGSprites = new Sprite[avatarEquipmentSlotBGImages.Length];
        for (int i = 0; i < avatarEquipmentSlotBGImages.Length; i++)
            _defaultEquipSlotBGSprites[i] = avatarEquipmentSlotBGImages[i].sprite;

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
        {
            avatarSlotImages[slot].color = _equipped[index] ? slotEquippedTint : slotUnequippedTint;
            avatarEquipmentSlotBGImages[slot].sprite = _equipped[index]
                ? equipSlotBGEquippedSprite
                : _defaultEquipSlotBGSprites[slot];
        }

        NCIITaskListManager.CheckConditions();
    }

    private void RefreshButton(int index)
    {
        bool on = _equipped[index];

        ppeSlotBGImages[index].sprite = on ? slotBGEquippedSprite : _defaultSlotBGSprites[index];

        ppeStatusTexts[index].text = (index == EsdMatIndex) ? "Placed" : "Equipped";
        ppeStatusTexts[index].gameObject.SetActive(on);
    }

    private void RefreshAll()
    {
        for (int i = 0; i < 7; i++)
            RefreshButton(i);

        for (int i = 0; i < avatarSlotImages.Length; i++)
        {
            avatarSlotImages[i].color = slotUnequippedTint;
            avatarEquipmentSlotBGImages[i].sprite = _defaultEquipSlotBGSprites[i];
        }
    }
}
