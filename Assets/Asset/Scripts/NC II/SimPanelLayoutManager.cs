using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SimPanelLayoutManager : MonoBehaviour
{
    public static SimPanelLayoutManager Instance { get; private set; }

    [Header("Panel RectTransforms")]
    [SerializeField] private RectTransform workspaceRect;
    [Tooltip("Terminal root RectTransform. Height read at Start to know how much workspace expands.")]
    [SerializeField] private RectTransform terminalRect;

    [Header("Content Groups")]
    [Tooltip("CanvasGroup on the Terminal ROOT (hides the whole panel including background). " +
             "Workspace bottom edge expands downward when hidden.")]
    [SerializeField] private CanvasGroup terminalRootGroup;

    [Tooltip("CanvasGroup on the TaskListPanel ROOT (hides the whole panel). " +
             "Workspace is already behind it so no layout change is needed.")]
    [SerializeField] private CanvasGroup taskListRootGroup;

    [Header("Arrow Images  (z-rotation: 0 = expanded, 180 = collapsed)")]
    [Tooltip("Terminal_Toggle button ROOT RectTransform.")]
    [SerializeField] private RectTransform terminalArrow;
    [Tooltip("TaskList_Toggle button ROOT RectTransform.")]
    [SerializeField] private RectTransform taskListArrow;

    [Header("Detail Panel Layers (sync with workspace bounds)")]
    [Tooltip("Assign the outer container RectTransforms (direct Canvas children). " +
             "They are resized to match the workspace whenever a tray is toggled.")]
    [SerializeField] private RectTransform[] detailLayerRects;

    [Header("Settings")]
    [SerializeField] private float animDuration = 0.2f;

    private Vector2 _wsMinExp, _wsMaxExp;
    private float   _termExpandedHeight;

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
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        _wsMinExp = workspaceRect.offsetMin;
        _wsMaxExp = workspaceRect.offsetMax;

        if (terminalRect != null)
            _termExpandedHeight = terminalRect.rect.height;

        // Override Canvas sort order so these buttons always render and receive input
        // above detail panel layers, regardless of sibling order.
        StampToggleButton(terminalArrow);
        StampToggleButton(taskListArrow);

        SyncDetailLayersNow();
    }

    // Ensures a toggle button stays visible when its parent panel is hidden AND renders
    // on top of any detail panel overlay.
    private static void StampToggleButton(RectTransform rt)
    {
        if (rt == null) return;

        var cg = rt.GetComponent<CanvasGroup>();
        if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
        cg.ignoreParentGroups = true;
        cg.alpha             = 1f;
        cg.interactable      = true;
        cg.blocksRaycasts    = true;

        // Override Canvas so this button sorts above detail panels (sortingOrder > 0).
        var canvas = rt.GetComponent<Canvas>();
        if (canvas == null) canvas = rt.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder    = 100;

        // A nested override Canvas needs its own GraphicRaycaster to receive clicks.
        if (rt.GetComponent<GraphicRaycaster>() == null)
            rt.gameObject.AddComponent<GraphicRaycaster>();
    }

    // ── Public toggle methods – wire each to its Button.onClick in the Inspector ──────────────

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
        // but Animate is called so any in-flight animation is properly restarted.
        Animate();
    }

    // ── Internals ────────────────────────────────────────────────────────────────────────────

    private static void SetGroup(CanvasGroup cg, bool visible)
    {
        if (cg == null) return;
        cg.alpha          = visible ? 1f : 0f;
        cg.interactable   = visible;
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

    // Terminal OFF → workspace bottom edge moves DOWN (height grows to fill freed space).
    private void ComputeTargets(out Vector2 wsMin, out Vector2 wsMax)
    {
        wsMin = new Vector2(
            _wsMinExp.x,
            _wsMinExp.y - (_termCollapsed ? _termExpandedHeight : 0f));
        wsMax = _wsMaxExp;
    }

    private IEnumerator RunAnimation()
    {
        ComputeTargets(out var wsTMin, out var wsTMax);

        var wsSMin = workspaceRect.offsetMin;
        var wsSMax = workspaceRect.offsetMax;

        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / animDuration));

            workspaceRect.offsetMin = Vector2.LerpUnclamped(wsSMin, wsTMin, t);
            workspaceRect.offsetMax = Vector2.LerpUnclamped(wsSMax, wsTMax, t);
            SyncDetailLayersNow();

            yield return null;
        }

        workspaceRect.offsetMin = wsTMin;
        workspaceRect.offsetMax = wsTMax;
        SyncDetailLayersNow();

        // Skip clamping while the detail panel is open — the workspace camera is parked at its
        // default position (not the user's pan/zoom), so WorldToScreenPoint projects workspace
        // objects to wrong screen coordinates and collapses them all onto the same edge.
        // CloseEditor() calls ClampObjectsToWorkspace() after restoring the real camera.
        if (GameManager.Instance == null || !GameManager.Instance.IsEditorOpen)
            WorkspaceZoomController.Instance?.ClampObjectsToWorkspace();
        GameManager.Instance?.RecenterActiveEditor();
    }

    /// <summary>
    /// Resizes every rect in detailLayerRects to cover exactly the same canvas area as
    /// workspaceRect. Called by GameManager when a detail panel opens and after each
    /// animation frame so the detail view always fills the correct available area.
    /// </summary>
    public void SyncDetailLayersNow()
    {
        if (detailLayerRects == null || workspaceRect == null) return;

        var canvasRT = workspaceRect.parent as RectTransform;
        if (canvasRT == null) return;

        float canvasW = canvasRT.rect.width;
        float canvasH = canvasRT.rect.height;
        if (canvasW <= 0f || canvasH <= 0f) return;

        float wsL = workspaceRect.offsetMin.x;
        float wsB = workspaceRect.offsetMin.y;
        float wsR = canvasW + workspaceRect.offsetMax.x;
        float wsT = canvasH + workspaceRect.offsetMax.y;

        foreach (RectTransform r in detailLayerRects)
        {
            if (r == null) continue;

            if (r.parent != canvasRT)
            {
                Debug.LogWarning($"[SimPanelLayoutManager] '{r.name}' is not a direct Canvas child " +
                                 "and was skipped by SyncDetailLayersNow. " +
                                 "Assign the OUTER container rect (e.g. DetailPanel), not an inner child.", r);
                continue;
            }

            float anchorL = r.anchorMin.x * canvasW;
            float anchorB = r.anchorMin.y * canvasH;
            float anchorR = r.anchorMax.x * canvasW;
            float anchorT = r.anchorMax.y * canvasH;

            r.offsetMin = new Vector2(wsL - anchorL, wsB - anchorB);
            r.offsetMax = new Vector2(wsR - anchorR, wsT - anchorT);
        }
    }
}
