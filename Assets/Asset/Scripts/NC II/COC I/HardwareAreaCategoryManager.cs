using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Manages the hardware area category system.
/// - Category dropdown is always visible (no burger toggle)
/// - Selecting a category shows only that category's icon container
/// - Hardware area background image swaps to categorySprites[index]
/// - Shift+1 through Shift+5 select categories by index
///
/// Setup:
/// 1. Attach to HardwareArea
/// 2. Assign categoryDropdown (must be enabled in scene), hardwareAreaImage
/// 3. Add category entries with their icon containers
/// 4. Fill categorySprites in parallel with categories list (index 0 = default)
/// </summary>
public class HardwareAreaCategoryManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The dropdown panel that contains category buttons (always visible)")]
    [SerializeField] private GameObject categoryDropdown;

    [Tooltip("The Image that displays the hardware area background sprite")]
    [SerializeField] private Image hardwareAreaImage;

    [Header("Categories")]
    [SerializeField] private List<CategoryEntry> categories = new List<CategoryEntry>();

    [Tooltip("Sprites indexed in parallel with categories — categorySprites[i] shown when categories[i] is selected")]
    [SerializeField] private Sprite[] categorySprites;

    [System.Serializable]
    public class CategoryEntry
    {
        [Tooltip("Display name for this category")]
        public string categoryName;

        [Tooltip("The button in the dropdown for this category")]
        public Button categoryButton;

        [Tooltip("The container holding all icons for this category")]
        public GameObject iconContainer;
    }

    private int _activeCategoryIndex = 0;

    private static readonly Key[] DigitKeys =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5
    };

    private void Start()
    {
        // Ensure the hardware area always renders and receives input above detail panel layers.
        Canvas overrideCanvas = GetComponent<Canvas>();
        if (overrideCanvas == null) overrideCanvas = gameObject.AddComponent<Canvas>();
        overrideCanvas.overrideSorting = true;
        overrideCanvas.sortingOrder = 50;
        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        // Wire each category button
        for (int i = 0; i < categories.Count; i++)
        {
            int index = i;
            if (categories[i].categoryButton != null)
                categories[i].categoryButton.onClick.AddListener(() => SelectCategory(index));
        }

        // Activate default category
        if (categories.Count > 0)
            SelectCategory(0);
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (!kb.shiftKey.isPressed) return;

        for (int i = 0; i < DigitKeys.Length; i++)
        {
            if (kb[DigitKeys[i]].wasPressedThisFrame)
            {
                SelectCategory(i);
                break;
            }
        }
    }

    private void SelectCategory(int index)
    {
        if (index < 0 || index >= categories.Count) return;

        _activeCategoryIndex = index;

        for (int i = 0; i < categories.Count; i++)
        {
            if (categories[i].iconContainer != null)
                categories[i].iconContainer.SetActive(i == _activeCategoryIndex);
        }

        if (hardwareAreaImage != null && categorySprites != null && index < categorySprites.Length && categorySprites[index] != null)
            hardwareAreaImage.sprite = categorySprites[index];

        Debug.Log($"[HardwareAreaCategoryManager] Selected category: {categories[_activeCategoryIndex].categoryName}");
    }
}