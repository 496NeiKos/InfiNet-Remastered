using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Manages the hardware area category system.
/// - Burger icon toggles category dropdown
/// - Selecting a category shows only that category's icons
/// - Only one category visible at a time
/// - Shift+1 through Shift+5 select categories by index (no editor open required)
///
/// Setup:
/// 1. Attach to HardwareArea
/// 2. Assign burger button and dropdown panel
/// 3. Add category entries with their icon containers
/// </summary>
public class HardwareAreaCategoryManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The burger menu icon button")]
    [SerializeField] private Button burgerButton;

    [Tooltip("The dropdown panel that contains category buttons (starts hidden)")]
    [SerializeField] private GameObject categoryDropdown;

    [Header("Categories")]
    [SerializeField] private List<CategoryEntry> categories = new List<CategoryEntry>();

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

    private bool _isDropdownOpen = false;
    private int _activeCategoryIndex = 0;

    // Digit keys 1-5 mapped in order so index 0 = Digit1Key, etc.
    private static readonly Key[] DigitKeys =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5
    };

    private void Start()
    {
        // Ensure the hardware area always renders and receives input above detail panel layers,
        // which sit later in the canvas sibling order and would otherwise block events here.
        // Mirrors the same pattern used for toggle buttons in SimPanelLayoutManager.
        Canvas overrideCanvas = GetComponent<Canvas>();
        if (overrideCanvas == null) overrideCanvas = gameObject.AddComponent<Canvas>();
        overrideCanvas.overrideSorting = true;
        overrideCanvas.sortingOrder = 50;
        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        // Wire burger button
        if (burgerButton != null)
            burgerButton.onClick.AddListener(ToggleDropdown);

        // Wire each category button
        for (int i = 0; i < categories.Count; i++)
        {
            int index = i; // capture for closure
            if (categories[i].categoryButton != null)
                categories[i].categoryButton.onClick.AddListener(() => SelectCategory(index));
        }

        // Hide dropdown at start
        if (categoryDropdown != null)
            categoryDropdown.SetActive(false);

        // Show default category (first one)
        if (categories.Count > 0)
            SelectCategory(0);
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Require Shift held. Works regardless of whether a detail panel is open.
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

    /// <summary>
    /// Toggle the category dropdown visibility.
    /// </summary>
    private void ToggleDropdown()
    {
        _isDropdownOpen = !_isDropdownOpen;

        if (categoryDropdown != null)
            categoryDropdown.SetActive(_isDropdownOpen);
    }

    /// <summary>
    /// Select a category. Hides all other categories and shows only the selected one.
    /// Also closes the dropdown.
    /// </summary>
    private void SelectCategory(int index)
    {
        if (index < 0 || index >= categories.Count) return;

        _activeCategoryIndex = index;

        // Hide all icon containers
        for (int i = 0; i < categories.Count; i++)
        {
            if (categories[i].iconContainer != null)
                categories[i].iconContainer.SetActive(false);
        }

        // Show selected category's icons
        if (categories[_activeCategoryIndex].iconContainer != null)
            categories[_activeCategoryIndex].iconContainer.SetActive(true);

        // Close dropdown
        _isDropdownOpen = false;
        if (categoryDropdown != null)
            categoryDropdown.SetActive(false);

        Debug.Log($"[HardwareAreaCategoryManager] Selected category: {categories[_activeCategoryIndex].categoryName}");
    }
}