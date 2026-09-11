using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adaptive side-panel guide. Content changes automatically as the user navigates
/// views and panels. Attach to a persistent HUD GameObject in the COC I scene.
///
/// Visibility rules:
///   - panelRoot (toggle button + body) is ONLY shown when an editor is open AND the
///     current context has a filled-in content entry. If a panel has no entry in the
///     content table, the ING hides entirely — it will not open, and if it was open
///     it closes. The user's toggle preference is preserved for when content returns.
///   - panelBody is controlled by the user toggle only. The system never forces it open.
///
/// Content rules:
///   - Each panel/view has an INGContextProvider that calls PushContext / PopContext.
///   - PushContext silently ignores any context that has no entry in the content table.
///     That panel is treated as invisible to the ING — no stack change, no visibility change.
///   - MotherboardPhaseManager calls PushContext / PopContext directly (replace pattern).
/// </summary>
public class InlineNotificationGuide : MonoBehaviour
{
    public static InlineNotificationGuide Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Root that holds both the toggle button and the panel body.")]
    [SerializeField] private GameObject panelRoot;

    [Tooltip("The content panel body. Toggled by the user only.")]
    [SerializeField] private GameObject panelBody;

    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Tooltip("Button that shows/hides the panel body.")]
    [SerializeField] private Button toggleButton;

    [Header("Content Table")]
    [Tooltip("Fill title and description only for contexts you want to support. " +
             "Contexts with no entry here are invisible to the ING.")]
    [SerializeField] private GuideContextEntry[] entries;

    private readonly List<GuideContext> _stack = new();
    private bool _bodyVisible;
    private bool _editorActive;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Start()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleBody);
    }

    // ── Called by GameManager when any detail editor opens ───────────

    /// <summary>
    /// Marks the editor as active. panelRoot does NOT show immediately — it only
    /// appears once a valid context (one with a content entry) is pushed.
    /// </summary>
    public void Show()
    {
        _editorActive = true;
    }

    // ── Called by GameManager at the terminal close path ─────────────

    public void Hide()
    {
        _editorActive = false;
        _stack.Clear();
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    // ── Stack operations ──────────────────────────────────────────────

    /// <summary>
    /// Pushes a context. If the context has no entry in the content table it is
    /// silently ignored — the stack and visibility are left unchanged.
    /// </summary>
    public void PushContext(GuideContext context)
    {
        if (context == GuideContext.None) return;
        if (!HasEntry(context)) return;
        _stack.Add(context);
        RefreshDisplay();
    }

    /// <summary>
    /// Pops a context. If it was never pushed (no content entry, or already removed)
    /// this is a safe no-op.
    /// </summary>
    public void PopContext(GuideContext context)
    {
        if (context == GuideContext.None) return;
        for (int i = _stack.Count - 1; i >= 0; i--)
        {
            if (_stack[i] != context) continue;
            _stack.RemoveAt(i);
            break;
        }
        RefreshDisplay();
    }

    // ── Internal ─────────────────────────────────────────────────────

    private void ToggleBody()
    {
        _bodyVisible = !_bodyVisible;
        if (panelBody != null) panelBody.SetActive(_bodyVisible);
    }

    /// <summary>
    /// Updates content text and panelRoot visibility based on current stack state.
    /// panelRoot only shows when the editor is active AND the stack has content.
    /// Body visibility follows the user's last toggle preference.
    /// </summary>
    private void RefreshDisplay()
    {
        if (!_editorActive || _stack.Count == 0)
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            return;
        }

        ApplyContent(_stack[_stack.Count - 1]);
        if (panelRoot != null) panelRoot.SetActive(true);
        if (panelBody != null) panelBody.SetActive(_bodyVisible);
    }

    private void ApplyContent(GuideContext context)
    {
        foreach (var entry in entries)
        {
            if (entry.context != context) continue;
            if (titleText != null)       titleText.text       = entry.title;
            if (descriptionText != null) descriptionText.text = entry.description;
            return;
        }
    }

    private bool HasEntry(GuideContext context)
    {
        foreach (var entry in entries)
            if (entry.context == context) return true;
        return false;
    }
}
