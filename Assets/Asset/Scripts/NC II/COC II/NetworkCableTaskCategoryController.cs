/*
 * ================================================================
 *  UNITY SETUP GUIDE — NetworkCableTaskCategoryController
 * ================================================================
 *
 *  PURPOSE
 *    Mediates between the two task category managers (Network Cable
 *    and IP Configuration) and the shared task display panel.
 *    Owns the burger button + popup panel that lets the user switch
 *    which category's task list is visible.
 *
 *  HIERARCHY REQUIREMENT
 *    This script and the burger/popup UI must live in a SHARED
 *    HEADER that is NOT inside either category panel. If it were
 *    inside one panel, it would disappear when that panel is hidden.
 *
 *    Recommended scene hierarchy:
 *
 *    [TaskPanelRoot]
 *      ├── [CategoryHeader]        ← this script goes here
 *      │     ├── BurgerButton
 *      │     └── PopupPanel
 *      │           ├── NetworkCableBtn
 *      │           └── IPConfigBtn
 *      └── [CategoryContent]
 *            ├── [NetworkCablePanel]   ← categoryPanels[0]
 *            └── [IPConfigPanel]       ← categoryPanels[1]
 *
 *  INSPECTOR SETUP
 *    Category Managers
 *      networkCableManager → NetworkCableTaskManager in the scene
 *      ipConfigManager     → IPConfigTaskManager in the scene
 *    Category Panels
 *      categoryPanels[0]   → NetworkCablePanel  (shown when index 0 active)
 *      categoryPanels[1]   → IPConfigPanel      (shown when index 1 active)
 *    Popup UI
 *      burgerButton        → Button that opens/closes the popup
 *      popupPanel          → Panel containing the category buttons
 *      categoryButtons[0]  → Button for "Network Cable"
 *      categoryButtons[1]  → Button for "IP Configuration"
 *
 *  BUTTON OnClick WIRING
 *    BurgerButton          → no manual wiring needed (done in Start)
 *    NetworkCableBtn       → no manual wiring needed (done in Start)
 *    IPConfigBtn           → no manual wiring needed (done in Start)
 * ================================================================
 */

using System;
using UnityEngine;
using UnityEngine.UI;

public class NetworkCableTaskCategoryController : MonoBehaviour
{
    /// <summary>
    /// Fires when the active category changes OR when the active category's
    /// task list updates. NetworkCableTaskDisplay subscribes to this.
    /// </summary>
    public static event Action OnActiveCategoryUpdated;

    // ── Inspector ──────────────────────────────────────────────────────────────────────

    [Header("Category Managers")]
    [SerializeField] private NetworkCableTaskManager networkCableManager;
    [SerializeField] private IPConfigTaskManager     ipConfigManager;

    [Header("Category Panels")]
    [Tooltip("Index must match manager order: [0] = Network Cable, [1] = IP Config.")]
    [SerializeField] private GameObject[] categoryPanels;

    [Header("Popup UI")]
    [SerializeField] private Button     burgerButton;
    [SerializeField] private GameObject popupPanel;
    [Tooltip("Index must match manager order: [0] = Network Cable, [1] = IP Config.")]
    [SerializeField] private Button[]   categoryButtons;

    // ── State ──────────────────────────────────────────────────────────────────────────

    private ITaskCategory[] _managers;
    private int             _activeCategory;
    private bool            _isPopupOpen;

    // ── Lifecycle ──────────────────────────────────────────────────────────────────────

    private void Start()
    {
        _managers = new ITaskCategory[] { networkCableManager, ipConfigManager };

        if (burgerButton != null)
            burgerButton.onClick.AddListener(TogglePopup);

        for (int i = 0; i < categoryButtons.Length; i++)
        {
            int captured = i;
            if (categoryButtons[i] != null)
                categoryButtons[i].onClick.AddListener(() => SwitchCategory(captured));
        }

        if (popupPanel != null)
            popupPanel.SetActive(false);

        // Activate only the first panel; hide the rest.
        for (int i = 0; i < categoryPanels.Length; i++)
            if (categoryPanels[i] != null)
                categoryPanels[i].SetActive(i == 0);

        _activeCategory = 0;

        NetworkCableTaskManager.OnTasksUpdated += OnNetworkCableUpdated;
        IPConfigTaskManager.OnTasksUpdated     += OnIPConfigUpdated;
    }

    private void OnDestroy()
    {
        NetworkCableTaskManager.OnTasksUpdated -= OnNetworkCableUpdated;
        IPConfigTaskManager.OnTasksUpdated     -= OnIPConfigUpdated;
    }

    private void OnDisable()
    {
        // Force-close the popup if the task panel header is hidden externally.
        ForceClosePopup();
    }

    // ── Manager Event Relay ────────────────────────────────────────────────────────────

    private void OnNetworkCableUpdated()
    {
        if (_activeCategory == 0)
            OnActiveCategoryUpdated?.Invoke();
    }

    private void OnIPConfigUpdated()
    {
        if (_activeCategory == 1)
            OnActiveCategoryUpdated?.Invoke();
    }

    // ── Popup ─────────────────────────────────────────────────────────────────────────

    public void TogglePopup()
    {
        _isPopupOpen = !_isPopupOpen;
        if (popupPanel != null)
            popupPanel.SetActive(_isPopupOpen);
    }

    private void ForceClosePopup()
    {
        _isPopupOpen = false;
        if (popupPanel != null)
            popupPanel.SetActive(false);
    }

    // ── Category Switch ───────────────────────────────────────────────────────────────

    public void SwitchCategory(int index)
    {
        if (index < 0 || index >= _managers.Length) return;

        if (index == _activeCategory)
        {
            ForceClosePopup();
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsEditorOpen)
            GameManager.Instance.CloseEditor();

        if (_activeCategory < categoryPanels.Length && categoryPanels[_activeCategory] != null)
            categoryPanels[_activeCategory].SetActive(false);

        _managers[_activeCategory]?.HideCompletionBanner();

        _activeCategory = index;

        if (_activeCategory < categoryPanels.Length && categoryPanels[_activeCategory] != null)
            categoryPanels[_activeCategory].SetActive(true);

        ForceClosePopup();

        // Notify the display so it refreshes with the new category's task text.
        OnActiveCategoryUpdated?.Invoke();

        Debug.Log($"[NetworkCableTaskCategoryController] Switched to category index {_activeCategory}.");
    }

    // ── Public API (consumed by NetworkCableTaskDisplay and T568HintVisibility) ─────────

    public bool IsNetworkCableActive => _activeCategory == 0;

    // ── Public API (consumed by NetworkCableTaskDisplay) ──────────────────────────────

    public string GetActiveTaskText()
    {
        if (_managers == null || _activeCategory >= _managers.Length) return null;
        return _managers[_activeCategory]?.GetNextIncompleteTaskText();
    }

    public Color GetActiveDisplayColor(Color fallback)
    {
        if (_managers == null || _activeCategory >= _managers.Length) return fallback;
        return _managers[_activeCategory]?.GetDisplayColor(fallback) ?? fallback;
    }
}
