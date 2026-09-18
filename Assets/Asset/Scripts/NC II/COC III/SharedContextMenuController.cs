/*
 * ================================================================
 *  UNITY SETUP GUIDE — SharedContextMenuController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "ContextMenu" GameObject (child of Group Policy Management Panel,
 *    starts INACTIVE). Must be the LAST sibling in the panel so it renders
 *    on top of everything including GPO Editor Panel.
 *
 *  HIERARCHY
 *    ContextMenu (Image panel)            ← this script here (starts INACTIVE)
 *          anchor: top-left, pivot (0,1)  ← positioned at cursor by script
 *          size: ~220 × auto
 *          VerticalLayoutGroup — spacing:0, padding:4
 *          ContentSizeFitter — Vertical: Preferred Size
 *          ├── BtnCreateGPO (Button)      → btnCreateGPO
 *          │     TMP_Text: "Create a GPO in this domain, and Link it here..."
 *          │     LayoutElement preferredHeight:28
 *          ├── BtnEditGPO  (Button)       → btnEditGPO
 *          │     TMP_Text: "Edit"
 *          ├── BtnEnforced (Button)       → btnEnforced
 *          │     TMP_Text                 → btnEnforcedLabel
 *          └── BtnProperties (Button)    → btnProperties
 *                TMP_Text: "Properties"
 *
 *  INSPECTOR ASSIGNMENTS
 *    btnCreateGPO    → BtnCreateGPO
 *    btnEditGPO      → BtnEditGPO
 *    btnEnforced     → BtnEnforced
 *    btnEnforcedLabel→ TMP_Text child of BtnEnforced
 *    btnProperties   → BtnProperties
 *    uiCamera        → leave None for Screen Space Overlay
 *
 *  HOW IT WORKS
 *    Show…() hides all buttons, shows only relevant ones, positions the panel
 *    at the cursor, then activates it. Each button click fires its callback
 *    and calls Hide(). No backdrop — call Hide() externally when needed.
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SharedContextMenuController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button   btnCreateGPO;
    [SerializeField] private Button   btnEditGPO;
    [SerializeField] private Button   btnEnforced;
    [SerializeField] private TMP_Text btnEnforcedLabel;
    [SerializeField] private Button   btnProperties;

    [Header("Canvas")]
    [SerializeField] private Camera uiCamera; // leave None for Screen Space Overlay

    private System.Action _onCreateGPO;
    private System.Action _onEditGPO;
    private System.Action _onEnforced;
    private System.Action _onProperties;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        btnCreateGPO ?.onClick.AddListener(() => { Hide(); _onCreateGPO?.Invoke();  });
        btnEditGPO   ?.onClick.AddListener(() => { Hide(); _onEditGPO?.Invoke();    });
        btnEnforced  ?.onClick.AddListener(() => { Hide(); _onEnforced?.Invoke();   });
        btnProperties?.onClick.AddListener(() => { Hide(); _onProperties?.Invoke(); });

        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void ShowForOU(Vector2 screenPos, RectTransform parentRect,
                          System.Action onCreateGPO)
    {
        _onCreateGPO  = onCreateGPO;
        _onEditGPO    = null;
        _onEnforced   = null;
        _onProperties = null;

        SetVisible(btnCreateGPO,  true);
        SetVisible(btnEditGPO,    false);
        SetVisible(btnEnforced,   false);
        SetVisible(btnProperties, false);

        PositionAndShow(screenPos, parentRect);
    }

    public void ShowForGPO(Vector2 screenPos, RectTransform parentRect,
                           bool isCurrentlyEnforced,
                           System.Action onEdit, System.Action onEnforced)
    {
        _onCreateGPO  = null;
        _onEditGPO    = onEdit;
        _onEnforced   = onEnforced;
        _onProperties = null;

        SetVisible(btnCreateGPO,  false);
        SetVisible(btnEditGPO,    true);
        SetVisible(btnEnforced,   true);
        SetVisible(btnProperties, false);

        if (btnEnforcedLabel != null)
            btnEnforcedLabel.text = isCurrentlyEnforced ? "✓ Enforced" : "Enforced";

        PositionAndShow(screenPos, parentRect);
    }

    public void ShowForFolderRow(Vector2 screenPos, RectTransform parentRect,
                                 System.Action onProperties)
    {
        _onCreateGPO  = null;
        _onEditGPO    = null;
        _onEnforced   = null;
        _onProperties = onProperties;

        SetVisible(btnCreateGPO,  false);
        SetVisible(btnEditGPO,    false);
        SetVisible(btnEnforced,   false);
        SetVisible(btnProperties, true);

        PositionAndShow(screenPos, parentRect);
    }

    public void Hide() => gameObject.SetActive(false);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void PositionAndShow(Vector2 screenPos, RectTransform parentRect)
    {
        gameObject.SetActive(true);
        var self = transform as RectTransform;
        if (self != null && parentRect != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, screenPos, uiCamera, out Vector2 localPos);
            self.anchoredPosition = localPos;
        }
        Canvas.ForceUpdateCanvases();
    }

    private static void SetVisible(Button btn, bool visible)
    {
        if (btn != null) btn.gameObject.SetActive(visible);
    }
}
