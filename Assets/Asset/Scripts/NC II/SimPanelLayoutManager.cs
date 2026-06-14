using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Collapsible tray system for the COC I simulation.
///
/// HardwareArea (left)   - RectTransform is resized; collapses to zero width (fully hidden).
///                         Workspace AND Terminal left edges expand to fill the freed space.
///
/// Terminal (bottom)     - Root CanvasGroup is toggled (entire panel fades out including background).
///                         Workspace bottom edge expands downward. Toggle button lives as a Canvas
///                         sibling so it remains visible when Terminal is hidden.
///
/// TaskListPanel (right) - Root CanvasGroup is toggled. Workspace already extends to the right
///                         edge behind it, so no layout change needed. Toggle button is a Canvas
///                         sibling as well.
///
/// Panel resize rules (what actually changes SIZE, not just position):
///   HardwareArea OFF  → HardwareArea collapses (width → 0)
///                     → Workspace left edge moves LEFT  (width grows)
///                     → Terminal  left edge moves LEFT  (width grows)
///   Terminal OFF      → Terminal hidden via CanvasGroup (RectTransform unchanged)
///                     → Workspace bottom edge moves DOWN (height grows)
///   Both OFF          → All three effects combine.
///
/// Background logic continues running while panels are hidden:
///   - ActivityLogManager keeps appending to its string; logText shows the latest on re-open.
///   - NCIITaskListManager keeps evaluating task conditions; the panel reflects current progress
///     the moment it becomes visible again.
///
/// ── SCENE SETUP ────────────────────────────────────────────────────────────────────────────────
///
/// HardwareArea
///   1. Create an empty child "ContentGroup" under HardwareArea.
///      Set its RectTransform: AnchorMin=(0,0), AnchorMax=(1,1), all offsets = 0.
///      Move all four existing children (BurgerButton, CategoryDropdown, icon containers)
///      into ContentGroup.
///      Add a CanvasGroup component to ContentGroup → assign to hardwareAreaContent.
///   2. Add a Button child "HW_Toggle" directly under HardwareArea (sibling of ContentGroup).
///      RectTransform: AnchorMin=(1,0.5), AnchorMax=(1,0.5), Pivot=(0,0.5),
///      AnchoredPos=(0,0), Size=(30,60).  This pins it to HardwareArea's right edge.
///      Add an Image child "Arrow" to the button with a left-pointing (◄) sprite.
///      Assign the Arrow's RectTransform → hardwareAreaArrow.
///      Wire Button.onClick → SimPanelLayoutManager.ToggleHardwareArea()
///
/// Terminal
///   1. Add a CanvasGroup component to the Terminal root GameObject.
///      Assign it → terminalRootGroup.
///   2. Create a Button as a direct child of the CANVAS (sibling of Terminal, not child).
///      Name it "Terminal_Toggle".
///      RectTransform: position it at Terminal's top-left edge so it is visible
///      when Terminal collapses (e.g., AnchorMin=(0,0), AnchoredPos=(600,307), Size=(80,30)).
///      Add an Image child "Arrow" with an up-pointing (▲) sprite.
///      Assign the Arrow's RectTransform → terminalArrow.
///      Wire Button.onClick → SimPanelLayoutManager.ToggleTerminal()
///
/// TaskListPanel
///   1. Add a CanvasGroup component to the TaskListPanel root GameObject.
///      Assign it → taskListRootGroup.
///   2. Create a Button as a direct child of the CANVAS (sibling of TaskListPanel).
///      Name it "TaskList_Toggle".
///      Position at TaskListPanel's left edge (e.g., AnchoredPos=(1645,800), Size=(30,60)).
///      Add an Image child "Arrow" with a right-pointing (►) sprite.
///      Assign the Arrow's RectTransform → taskListArrow.
///      Wire Button.onClick → SimPanelLayoutManager.ToggleTaskListPanel()
///
/// Manager
///   Create an empty "SimPanelManager" GameObject anywhere in the scene.
///   Attach this script and assign all Inspector fields.
/// ───────────────────────────────────────────────────────────────────────────────────────────────
/// </summary>
public class SimPanelLayoutManager : MonoBehaviour
{
    public static SimPanelLayoutManager Instance { get; private set; }

    [Header("Panel RectTransforms")]
    [SerializeField] private RectTransform hardwareAreaRect;
    [SerializeField] private RectTransform workspaceRect;
    [Tooltip("Terminal root RectTransform. Resized horizontally when HardwareArea collapses.")]
    [SerializeField] private RectTransform terminalRect;
    [Tooltip("Used only to read the initial width at Start; never resized.")]
    [SerializeField] private RectTransform taskListPanelRect;

    [Header("Content Groups")]
    [Tooltip("CanvasGroup on the ContentGroup CHILD of HardwareArea (not the root). " +
             "Hides icons and buttons; the root background strip stays visible.")]
    [SerializeField] private CanvasGroup hardwareAreaContent;

    [Tooltip("CanvasGroup on the Terminal ROOT (hides the whole panel including background). " +
             "Workspace expands to cover the freed area.")]
    [SerializeField] private CanvasGroup terminalRootGroup;

    [Tooltip("CanvasGroup on the TaskListPanel ROOT (hides the whole panel). " +
             "Workspace is already behind it so no layout change is needed.")]
    [SerializeField] private CanvasGroup taskListRootGroup;

    [Header("Arrow Images  (z-rotation: 0 = expanded direction, 180 = collapsed direction)")]
    [Tooltip("Assign the HW_Toggle button RectTransform (or its arrow Image child).")]
    [SerializeField] private RectTransform hardwareAreaArrow;
    [Tooltip("Assign the Terminal_Toggle button ROOT RectTransform. " +
             "The script will automatically add CanvasGroup(ignoreParentGroups=true) " +
             "so the button stays visible when Terminal is hidden.")]
    [SerializeField] private RectTransform terminalArrow;
    [Tooltip("Assign the TaskList_Toggle button ROOT RectTransform. " +
             "Same IgnoreParentGroups treatment as Terminal_Toggle.")]
    [SerializeField] private RectTransform taskListArrow;

    [Header("Settings")]
    [SerializeField] private float animDuration = 0.2f;

    // Offsets captured at Start (the 'expanded' state)
    private Vector2 _hwMinExp,   _hwMaxExp;
    private Vector2 _termMinExp, _termMaxExp;
    private Vector2 _wsMinExp,   _wsMaxExp;

    // Pixel dimensions captured at Start
    private float _hwExpandedWidth;
    private float _termExpandedHeight;

    private bool _hwCollapsed;
    private bool _termCollapsed;
    private bool _tlCollapsed;

    private Coroutine _anim;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        // Wait one frame so the Screen Space Camera canvas has applied its viewport size.
        // rect.width / rect.height return 0 on the first frame for this canvas mode.
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        // Capture the expanded offsets for all resizable panels
        _hwMinExp = hardwareAreaRect.offsetMin;
        _hwMaxExp = hardwareAreaRect.offsetMax;

        _wsMinExp = workspaceRect.offsetMin;
        _wsMaxExp = workspaceRect.offsetMax;

        if (terminalRect != null)
        {
            _termMinExp = terminalRect.offsetMin;
            _termMaxExp = terminalRect.offsetMax;
        }

        // Derive panel pixel dimensions from offsets + canvas size.
        // For a full-stretch rect (AnchorMin=0,0 / AnchorMax=1,1):
        //   width  = canvasW + offsetMax.x - offsetMin.x   (offsetMax.x is ≤ 0)
        //   height = canvasH + offsetMax.y - offsetMin.y   (offsetMax.y is ≤ 0)
        // This is reliable at any point in the frame unlike rect.width / rect.height.
        var canvasRT = hardwareAreaRect.parent as RectTransform;
        float canvasW = canvasRT != null ? canvasRT.rect.width  : Screen.width;
        float canvasH = canvasRT != null ? canvasRT.rect.height : Screen.height;

        _hwExpandedWidth = canvasW + _hwMaxExp.x - _hwMinExp.x;

        if (terminalRect != null)
            _termExpandedHeight = canvasH + _termMaxExp.y - _termMinExp.y;

        // HW_Toggle is a child of HardwareArea whose pivot shifts during the collapse animation,
        // dragging the button off-screen. Reparenting it to Canvas root keeps it stationary.
        ReparentToCanvas(hardwareAreaArrow, hardwareAreaRect);

        // Stamp all three toggle buttons so they ignore any parent CanvasGroup going to alpha=0.
        StampIgnoreParentGroup(hardwareAreaArrow);
        StampIgnoreParentGroup(terminalArrow);
        StampIgnoreParentGroup(taskListArrow);
    }

    // Moves a button to Canvas level (sibling of the panels) while keeping its visual position.
    // After this the button is no longer a child of the panel and won't move when the panel animates.
    private static void ReparentToCanvas(RectTransform button, RectTransform anyPanelChild)
    {
        if (button == null || anyPanelChild == null) return;
        var canvasRT = anyPanelChild.parent as RectTransform;
        if (canvasRT == null || button.parent == canvasRT) return;
        button.SetParent(canvasRT, worldPositionStays: true);
    }

    // Adds (or finds) a CanvasGroup on the given RectTransform's GameObject and marks it
    // as ignoring parent CanvasGroups so the button stays visible when its panel is hidden.
    private static void StampIgnoreParentGroup(RectTransform rt)
    {
        if (rt == null) return;
        var cg = rt.GetComponent<CanvasGroup>();
        if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
        cg.ignoreParentGroups = true;
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    // ── Public toggle methods – wire each to its Button.onClick in the Inspector ──────────────

    public void ToggleHardwareArea()
    {
        _hwCollapsed = !_hwCollapsed;
        SetGroup(hardwareAreaContent, !_hwCollapsed);
        SetArrow(hardwareAreaArrow, _hwCollapsed);
        Animate();
    }

    public void ToggleTerminal()
    {
        _termCollapsed = !_termCollapsed;
        SetGroup(terminalRootGroup, !_termCollapsed);
        SetArrow(terminalArrow, _termCollapsed);
        Animate();
    }

    public void ToggleTaskListPanel()
    {
        _tlCollapsed = !_tlCollapsed;
        SetGroup(taskListRootGroup, !_tlCollapsed);
        SetArrow(taskListArrow, _tlCollapsed);
        // TaskListPanel is already behind the Workspace so no layout change is needed,
        // but we still call Animate so any in-flight HW animation is properly restarted.
        Animate();
    }

    // ── Internals ────────────────────────────────────────────────────────────────────────────

    private static void SetGroup(CanvasGroup cg, bool visible)
    {
        if (cg == null) return;
        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }

    private static void SetArrow(RectTransform arrow, bool collapsed)
    {
        if (arrow == null) return;
        arrow.localEulerAngles = new Vector3(0f, 0f, collapsed ? 180f : 0f);
    }

    private void Animate()
    {
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(RunAnimation());
    }

    /// <summary>
    /// Calculates target offsetMin/offsetMax for HardwareArea, Workspace, and Terminal.
    ///
    /// HardwareArea OFF → HardwareArea width collapses to zero.
    ///                  → Workspace  left edge moves LEFT  (width grows to fill freed space).
    ///                  → Terminal   left edge moves LEFT  (width grows to fill freed space).
    /// Terminal OFF     → Terminal hidden via CanvasGroup; its RectTransform is NOT resized.
    ///                  → Workspace  bottom edge moves DOWN (height grows to fill freed space).
    /// </summary>
    private void ComputeTargets(
        out Vector2 hwMin,   out Vector2 hwMax,
        out Vector2 wsMin,   out Vector2 wsMax,
        out Vector2 termMin, out Vector2 termMax)
    {
        float hwDelta   = _hwExpandedWidth;
        float termDelta = _termExpandedHeight;

        // HardwareArea: right edge moves LEFT when collapsed (shrinks until hidden)
        hwMin = _hwMinExp;
        hwMax = new Vector2(
            _hwMaxExp.x - (_hwCollapsed ? hwDelta : 0f),
            _hwMaxExp.y);

        // Workspace:
        //   left edge  → moves LEFT when HW area is hidden  (width  grows leftward)
        //   bottom edge → moves DOWN when Terminal is hidden (height grows downward)
        wsMin = new Vector2(
            _wsMinExp.x - (_hwCollapsed   ? hwDelta   : 0f),
            _wsMinExp.y - (_termCollapsed ? termDelta : 0f));
        wsMax = _wsMaxExp;

        // Terminal:
        //   left edge → moves LEFT when HW area is hidden (width grows leftward)
        //   height stays the same (Terminal is hidden via CanvasGroup, not by resizing)
        termMin = new Vector2(
            _termMinExp.x - (_hwCollapsed ? hwDelta : 0f),
            _termMinExp.y);
        termMax = _termMaxExp;
    }

    private IEnumerator RunAnimation()
    {
        ComputeTargets(
            out var hwTMin, out var hwTMax,
            out var wsTMin, out var wsTMax,
            out var termTMin, out var termTMax);

        // Snapshot starting values for the lerp
        var hwSMin   = hardwareAreaRect.offsetMin;
        var hwSMax   = hardwareAreaRect.offsetMax;
        var wsSMin   = workspaceRect.offsetMin;
        var wsSMax   = workspaceRect.offsetMax;
        var termSMin = terminalRect != null ? terminalRect.offsetMin : default;
        var termSMax = terminalRect != null ? terminalRect.offsetMax : default;

        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            // Use unscaled time so the animation survives Time.timeScale = 0
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / animDuration));

            hardwareAreaRect.offsetMin = Vector2.LerpUnclamped(hwSMin, hwTMin, t);
            hardwareAreaRect.offsetMax = Vector2.LerpUnclamped(hwSMax, hwTMax, t);
            workspaceRect.offsetMin    = Vector2.LerpUnclamped(wsSMin, wsTMin, t);
            workspaceRect.offsetMax    = Vector2.LerpUnclamped(wsSMax, wsTMax, t);

            if (terminalRect != null)
            {
                terminalRect.offsetMin = Vector2.LerpUnclamped(termSMin, termTMin, t);
                terminalRect.offsetMax = Vector2.LerpUnclamped(termSMax, termTMax, t);
            }

            yield return null;
        }

        // Snap to exact targets
        hardwareAreaRect.offsetMin = hwTMin;
        hardwareAreaRect.offsetMax = hwTMax;
        workspaceRect.offsetMin    = wsTMin;
        workspaceRect.offsetMax    = wsTMax;

        if (terminalRect != null)
        {
            terminalRect.offsetMin = termTMin;
            terminalRect.offsetMax = termTMax;
        }

        // After the workspace has its final size, push any objects outside the new boundary to its edge.
        WorkspaceZoomController.Instance?.ClampObjectsToWorkspace();
    }
}
