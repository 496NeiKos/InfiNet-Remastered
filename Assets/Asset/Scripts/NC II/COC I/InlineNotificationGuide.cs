using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adaptive side-panel guide. Content changes automatically as the user navigates
/// views and panels. Attach to a persistent HUD GameObject in the COC I scene.
///
/// Visibility rules:
///   - Workspace contexts (Workspace_Topic1/2/3): shown whenever one is active,
///     even without an editor open. Multiple workspace contexts can coexist on the
///     workspace stack (e.g. if topics start active in the scene); the last-pushed
///     one shows. They are stored separately from the editor stack and survive Hide().
///   - Editor contexts: shown when an editor is open AND the stack has a filled-in
///     entry. Editor contexts take priority over the workspace context.
///   - Empty editor stack (current view has no entry) hides the ING — the workspace
///     context never bleeds through while an editor is open.
///   - panelBody is controlled by the user toggle only. The system never forces it open.
///
/// Content rules:
///   - Each panel/view has an INGContextProvider that calls PushContext / PopContext.
///   - Workspace contexts are routed to _workspaceStack (not the editor stack) and
///     persist across editor open/close cycles.
///   - MotherboardPhaseManager calls PushContext / PopContext directly (replace pattern).
///
/// IMPORTANT — panelRoot wiring:
///   Do NOT wire panelRoot to this same GameObject. If panelRoot == gameObject,
///   SetActive(false) would disable this script itself. Wire panelRoot to a dedicated
///   child that contains the toggle button and panel body.
/// </summary>
public class InlineNotificationGuide : MonoBehaviour
{
    public static InlineNotificationGuide Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Root that holds both the toggle button and the panel body. Must NOT be this same GameObject.")]
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

    private readonly List<GuideContext> _stack          = new(); // editor contexts
    private readonly List<GuideContext> _workspaceStack = new(); // workspace contexts, survive Hide()
    private bool _bodyVisible;
    private bool _editorActive;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // Guard: never call SetActive(false) on ourselves — that would kill Start() and all
        // coroutines. panelRoot should always be a dedicated child, not this GameObject.
        if (panelRoot != null && panelRoot != gameObject)
            panelRoot.SetActive(false);
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

    // ── Called by GameManager when the editor closes ──────────────────

    public void Hide()
    {
        _editorActive = false;
        _stack.Clear();
        RefreshDisplay(); // workspace context may still be active — re-evaluate
    }

    // ── Stack operations ──────────────────────────────────────────────

    /// <summary>
    /// Pushes a context. Workspace contexts go onto the workspace stack (LIFO, survive Hide).
    /// All other contexts go onto the editor stack. Silently ignored if the context has no
    /// entry in the content table.
    /// </summary>
    public void PushContext(GuideContext context)
    {
        if (context == GuideContext.None) return;
        if (!HasEntry(context)) return;
        if (IsWorkspaceContext(context))
        {
            if (!_workspaceStack.Contains(context))
                _workspaceStack.Add(context);
            RefreshDisplay();
            return;
        }
        _stack.Add(context);
        RefreshDisplay();
    }

    /// <summary>
    /// Pops a context. Workspace contexts are removed from the workspace stack.
    /// Editor contexts are removed from the editor stack. Safe no-op if never pushed.
    /// </summary>
    public void PopContext(GuideContext context)
    {
        if (context == GuideContext.None) return;
        if (IsWorkspaceContext(context))
        {
            _workspaceStack.Remove(context);
            RefreshDisplay();
            return;
        }
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
    /// Updates content text and panelRoot visibility.
    /// Editor stack takes priority. Workspace stack shows when no editor is open.
    /// Empty editor stack hides the ING — workspace never bleeds through.
    /// </summary>
    private void RefreshDisplay()
    {
        if (_editorActive)
        {
            if (_stack.Count > 0)
            {
                ApplyContent(_stack[_stack.Count - 1]);
                if (panelRoot != null) panelRoot.SetActive(true);
                if (panelBody != null) panelBody.SetActive(_bodyVisible);
            }
            else
            {
                if (panelRoot != null) panelRoot.SetActive(false);
            }
            return;
        }

        if (_workspaceStack.Count > 0)
        {
            ApplyContent(_workspaceStack[_workspaceStack.Count - 1]);
            if (panelRoot != null) panelRoot.SetActive(true);
            if (panelBody != null) panelBody.SetActive(_bodyVisible);
            return;
        }

        if (panelRoot != null) panelRoot.SetActive(false);
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

    private static bool IsWorkspaceContext(GuideContext context) =>
        context == GuideContext.Workspace_Topic1 ||
        context == GuideContext.Workspace_Topic2 ||
        context == GuideContext.Workspace_Topic3 ||
        // COC II–IV contexts are standalone panels with no GameManager Show/Hide wiring,
        // so they behave like workspace contexts — show whenever the panel is active.
        context == GuideContext.COCII_UTPCableDetailView;
}
