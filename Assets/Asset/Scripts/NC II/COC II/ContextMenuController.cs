/*
 * ================================================================
 *  UNITY SETUP GUIDE — ContextMenuController
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add ONE instance of this script to the GameObject that should
 *    be RIGHT-CLICKED to open its dropdown menu.
 *    Use this for BOTH:
 *      • NetworkBtn (desktop Network icon) — opens Dropdown_Option_Network
 *      • Ethernet button (adapter icon inside Network Connections Panel)
 *        — opens Dropdown_Option_Ethernet
 *
 *  HIERARCHY EXAMPLES
 *    Apps > NetworkBtn  (this script here)
 *      targetDropdown → Dropdown_Option_Network
 *
 *    Network Connections Panel > GridLayoutGroupPanel > Ethernet  (this script here)
 *      targetDropdown → Dropdown_Option_Ethernet
 *
 *  INSPECTOR ASSIGNMENTS
 *    targetDropdown   → the Dropdown_Option_* panel to show on right-click
 *    blocker          → a full-screen transparent Button under the same Canvas
 *                       (Image component, alpha=0, Raycast Target ON, starts INACTIVE)
 *                       Wire its OnClick: ContextMenuController.CloseMenu() on THIS object
 *
 *  HOW IT WORKS
 *    Right-click (via IPointerClickHandler) opens targetDropdown and activates the blocker.
 *    Any click on the blocker (i.e., anywhere outside the dropdown) closes the menu.
 *    Only one dropdown can be open at a time.
 *
 *  IMPLEMENTATION GUIDE (what to add in the Editor)
 *    Create ONE shared "Blocker" GameObject as a child of the Canvas (not under any panel):
 *      • RectTransform anchored to fill the full canvas
 *      • Image component, Color alpha = 0
 *      • Button component, OnClick → ContextMenuController.CloseMenu() on the target
 *      • Starts INACTIVE
 *    Assign this same Blocker to both ContextMenuController instances (NetworkBtn + Ethernet).
 * ================================================================
 */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ContextMenuController : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject targetDropdown;
    [Tooltip("Full-screen transparent Button that closes the menu on outside-click. Starts INACTIVE.")]
    [SerializeField] private GameObject blocker;

    [Header("Properties Button (optional)")]
    [Tooltip("The PropertiesBtn inside this dropdown — auto-wired to open propertiesTarget.")]
    [SerializeField] private Button propertiesBtn;
    [Tooltip("Panel to activate when PropertiesBtn is clicked.")]
    [SerializeField] private GameObject propertiesTarget;

    private static ContextMenuController _current;

    private void Start()
    {
        if (propertiesBtn != null && propertiesTarget != null)
        {
            propertiesBtn.onClick.AddListener(() => { CloseMenu(); propertiesTarget.SetActive(true); });
            Debug.Log($"[ContextMenuController] PropertiesBtn wired on {gameObject.name} → {propertiesTarget.name}.");
        }
        else if (propertiesBtn != null || propertiesTarget != null)
        {
            Debug.LogWarning($"[ContextMenuController] PropertiesBtn wiring incomplete on {gameObject.name}: " +
                             $"propertiesBtn={propertiesBtn}, propertiesTarget={propertiesTarget}.");
        }
    }

    // ----------------------------------------------------------------
    //  IPointerClickHandler — right-click on this UI element
    // ----------------------------------------------------------------

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        ToggleMenu();
    }

    // ----------------------------------------------------------------
    //  Public
    // ----------------------------------------------------------------

    public void ToggleMenu()
    {
        if (targetDropdown == null) return;

        bool willOpen = !targetDropdown.activeSelf;

        if (_current != null && _current != this)
            _current.CloseMenu();

        if (willOpen) OpenMenu();
        else          CloseMenu();
    }

    public void CloseMenu()
    {
        targetDropdown?.SetActive(false);
        blocker?.SetActive(false);
        if (_current == this) _current = null;
        Debug.Log($"[ContextMenuController] Menu closed on {gameObject.name}.");
    }

    // ----------------------------------------------------------------
    //  Private
    // ----------------------------------------------------------------

    private void OpenMenu()
    {
        targetDropdown.SetActive(true);
        if (blocker != null)
        {
            // Blocker must sit BEHIND all dropdowns in the Canvas hierarchy.
            // Moving it to index 0 ensures every sibling (Windows Desktop and its
            // children, including the dropdown) renders on top and receives clicks first.
            blocker.transform.SetAsFirstSibling();

            // Re-wire the blocker so it always closes THIS controller, not whichever
            // one was hardcoded in the Inspector.  RemoveAllListeners only clears
            // runtime listeners; the Inspector-persistent call on NetworkBtn still
            // fires but is harmless (its dropdown is already closed).
            var blockerBtn = blocker.GetComponent<Button>();
            if (blockerBtn != null)
            {
                blockerBtn.onClick.RemoveAllListeners();
                blockerBtn.onClick.AddListener(CloseMenu);
            }

            blocker.SetActive(true);
        }
        _current = this;
        Debug.Log($"[ContextMenuController] Menu opened on {gameObject.name}.");
    }
}
