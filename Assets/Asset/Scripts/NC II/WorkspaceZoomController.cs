using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Zoom and pan controller for the COC I workspace camera.
///
/// Scroll wheel  — zooms in/out while the mouse is over the workspace panel.
///                 Camera position is NOT changed; only orthographic size.
///
/// Left-click drag on blank space — pans the camera (moves the view).
///                 Detected by Physics2D.Raycast: if no 3D collider is under the cursor
///                 the press is treated as a pan, otherwise DragPrefab handles it.
///
/// ClampObjectsToWorkspace() — called by SimPanelLayoutManager after each panel toggle.
///                 Moves any placed object whose collider falls outside the new workspace
///                 boundary to the nearest edge. Camera position and zoom are unchanged.
///
/// SCENE SETUP
///   Attach to the SimPanelManager GameObject (or any persistent GO).
///   workspaceCamera — Camera that renders the 3D hardware objects.
///   workspaceRect   — Workspace RectTransform (same ref used by SimPanelLayoutManager).
/// </summary>
public class WorkspaceZoomController : MonoBehaviour
{
    public static WorkspaceZoomController Instance { get; private set; }

    [Header("References")]
    [Tooltip("Camera that renders the 3D hardware objects in the workspace.")]
    [SerializeField] private Camera workspaceCamera;
    [Tooltip("RectTransform of the Workspace UI panel.")]
    [SerializeField] private RectTransform workspaceRect;

    [Header("Scroll Zoom")]
    [Tooltip("Orthographic size change per scroll unit.")]
    [SerializeField] private float scrollStep = 0.3f;
    [SerializeField] private float minOrthoSize = 1f;
    [SerializeField] private float maxOrthoSize = 30f;

    [Header("Animation")]
    [SerializeField] private float animDuration = 0.25f;

    private float   _defaultOrthoSize;
    private Vector3 _defaultCameraPos;
    private float   _targetOrthoSize;
    private Coroutine _animCoroutine;

    // Detail-panel save/restore
    private float   _savedOrthoSize;
    private Vector3 _savedCameraPos;
    private bool    _isInDetailPanel;

    // Pan state
    private bool    _isPanning;
    private Vector2 _panLastScreenPos;

    // Cached canvas (same canvas for every frame; set in Start)
    private Canvas _canvas;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (workspaceCamera != null)
        {
            _defaultOrthoSize = workspaceCamera.orthographicSize;
            _defaultCameraPos = workspaceCamera.transform.position;
            _targetOrthoSize  = _defaultOrthoSize;
        }
        _canvas = workspaceRect != null ? workspaceRect.GetComponentInParent<Canvas>() : null;
    }

    private void Update()
    {
        if (workspaceCamera == null || workspaceRect == null) return;
        if (Mouse.current == null) return;

        HandleScroll();
        HandlePan();
    }

    // ── Scroll zoom ───────────────────────────────────────────────────────────────────────────

    private void HandleScroll()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsEditorOpen) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll == 0f) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        if (!RectTransformUtility.RectangleContainsScreenPoint(workspaceRect, mousePos, GetUICamera()))
            return;

        float next = Mathf.Clamp(_targetOrthoSize - scroll * scrollStep, minOrthoSize, maxOrthoSize);
        next = ClampOrthoSizeForObjects(next);
        SmoothZoom(next, workspaceCamera.transform.position);
    }

    // ── Workspace pan ─────────────────────────────────────────────────────────────────────────

    private void HandlePan()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsEditorOpen) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (!_isPanning)
        {
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;
            if (!RectTransformUtility.RectangleContainsScreenPoint(workspaceRect, mousePos, GetUICamera())) return;

            // If cursor is over a 3D physics object, let DragPrefab handle it
            Vector2 worldPos2D = workspaceCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0f));
            if (Physics2D.Raycast(worldPos2D, Vector2.zero).collider != null) return;

            // Blank space — start pan; stop any running zoom/fit animation
            if (_animCoroutine != null)
            {
                StopCoroutine(_animCoroutine);
                _animCoroutine = null;
                _targetOrthoSize = workspaceCamera.orthographicSize;
            }

            _isPanning = true;
            _panLastScreenPos = mousePos;
        }
        else
        {
            if (!Mouse.current.leftButton.isPressed)
            {
                _isPanning = false;
                return;
            }

            Vector2 screenDelta = mousePos - _panLastScreenPos;
            _panLastScreenPos = mousePos;   // always update so there's no snap on constraint release

            if (screenDelta.sqrMagnitude < 0.001f) return;

            // Clamp the screen delta so no currently-visible placed object exits the workspace.
            screenDelta = ClampPanToKeepObjectsInWorkspace(screenDelta);

            if (screenDelta.sqrMagnitude < 0.001f) return;

            // Dragging right (delta.x > 0) → objects move right → camera moves left.
            float aspect = (float)Screen.width / Screen.height;
            float wpu_x  = 2f * workspaceCamera.orthographicSize * aspect / Screen.width;
            float wpu_y  = 2f * workspaceCamera.orthographicSize          / Screen.height;

            workspaceCamera.transform.position += new Vector3(-screenDelta.x * wpu_x, -screenDelta.y * wpu_y, 0f);
        }
    }

    /// <summary>
    /// Returns the minimum orthographic size that keeps every placed object's collider
    /// inside the workspace boundary at the current camera position.
    ///
    /// Math: at ortho size s, PPU = Screen.height / (2*s).
    /// An object at world dx from camera maps to screen x = scx + dx * PPU.
    /// Right-edge constraint:  scx + (dx + ex) * PPU ≤ wsR
    ///   → s ≥ Screen.height * (dx + ex) / (2 * (wsR - scx))   [when dx+ex > 0]
    /// Left-edge constraint:   scx + (dx - ex) * PPU ≥ wsL
    ///   → s ≥ Screen.height * (ex - dx) / (2 * (scx - wsL))   [when ex-dx > 0]
    /// Same logic applies on the Y axis.  Taking the max across all objects and sides
    /// gives the tightest zoom-in limit.
    /// </summary>
    private float ClampOrthoSizeForObjects(float targetSize)
    {
        if (GameManager.Instance == null) return targetSize;
        Transform container = GameManager.Instance.ActiveWorldContainer;
        if (container == null || container.childCount == 0) return targetSize;

        float sf = _canvas != null ? _canvas.scaleFactor : 1f;
        Vector2 oMin = workspaceRect.offsetMin;
        Vector2 oMax = workspaceRect.offsetMax;
        float wsL = oMin.x * sf;
        float wsB = oMin.y * sf;
        float wsR = Screen.width  + oMax.x * sf;
        float wsT = Screen.height + oMax.y * sf;

        // Camera world position maps to the screen centre.
        float scx = Screen.width  * 0.5f;
        float scy = Screen.height * 0.5f;
        Vector3 camPos = workspaceCamera.transform.position;

        float minSize = minOrthoSize;

        foreach (Transform child in container)
        {
            if (!child.gameObject.activeInHierarchy) continue;

            // World-space extents from the first enabled collider, fall back to sprite bounds.
            Vector2 extW = Vector2.zero;
            foreach (Collider2D col in child.GetComponentsInChildren<Collider2D>())
            {
                if (!col.enabled) continue;
                extW = new Vector2(col.bounds.extents.x, col.bounds.extents.y);
                break;
            }
            if (extW == Vector2.zero)
            {
                SpriteRenderer sr = child.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) extW = new Vector2(sr.bounds.extents.x, sr.bounds.extents.y);
            }

            float dx = child.position.x - camPos.x;
            float dy = child.position.y - camPos.y;

            // Right edge
            float rOff = dx + extW.x;
            if (rOff > 0f && wsR > scx)
                minSize = Mathf.Max(minSize, Screen.height * rOff / (2f * (wsR - scx)));

            // Left edge
            float lOff = extW.x - dx;
            if (lOff > 0f && scx > wsL)
                minSize = Mathf.Max(minSize, Screen.height * lOff / (2f * (scx - wsL)));

            // Top edge
            float tOff = dy + extW.y;
            if (tOff > 0f && wsT > scy)
                minSize = Mathf.Max(minSize, Screen.height * tOff / (2f * (wsT - scy)));

            // Bottom edge
            float bOff = extW.y - dy;
            if (bOff > 0f && scy > wsB)
                minSize = Mathf.Max(minSize, Screen.height * bOff / (2f * (scy - wsB)));
        }

        // minSize is the zoom-in floor; if targetSize is larger (zoom-out) it passes through unchanged.
        return Mathf.Max(targetSize, minSize);
    }

    /// <summary>
    /// Clamps a proposed screen-pixel pan delta so that no placed object whose centre
    /// is currently inside the workspace exits the workspace boundary after the pan.
    ///
    /// After panning by (dx, dy) screen pixels an object at screen position (sx, sy)
    /// moves to (sx + dx, sy + dy).  The constraint per axis per object is:
    ///   wsL + extPx  <=  sx + dx  <=  wsR - extPx
    ///   => wsL + extPx - sx  <=  dx  <=  wsR - extPx - sx
    /// Taking the intersection of all objects gives the tightest allowed delta.
    /// </summary>
    private Vector2 ClampPanToKeepObjectsInWorkspace(Vector2 screenDelta)
    {
        if (GameManager.Instance == null) return screenDelta;
        Transform container = GameManager.Instance.ActiveWorldContainer;
        if (container == null || container.childCount == 0) return screenDelta;

        // Workspace screen-pixel bounds (same formula used in DragPrefab)
        float sf   = _canvas != null ? _canvas.scaleFactor : 1f;
        Vector2 oMin = workspaceRect.offsetMin;
        Vector2 oMax = workspaceRect.offsetMax;
        float wsL = oMin.x * sf;
        float wsB = oMin.y * sf;
        float wsR = Screen.width  + oMax.x * sf;
        float wsT = Screen.height + oMax.y * sf;

        // Pixels per world unit for this ortho size
        float ppu = Screen.height / (2f * workspaceCamera.orthographicSize);

        float minDx = float.NegativeInfinity, maxDx = float.PositiveInfinity;
        float minDy = float.NegativeInfinity, maxDy = float.PositiveInfinity;

        foreach (Transform child in container)
        {
            if (!child.gameObject.activeInHierarchy) continue;

            Vector2 sp = workspaceCamera.WorldToScreenPoint(child.position);

            // Skip objects already outside the workspace centre — don't force the camera to
            // drag them back in; they should not exist in this state after the drag-clamp fix.
            if (sp.x < wsL || sp.x > wsR || sp.y < wsB || sp.y > wsT) continue;

            // Find the first enabled collider (root collider may be disabled when proxy is active)
            Vector2 extPx = Vector2.zero;
            foreach (Collider2D col in child.GetComponentsInChildren<Collider2D>())
            {
                if (!col.enabled) continue;
                extPx = new Vector2(col.bounds.extents.x * ppu, col.bounds.extents.y * ppu);
                break;
            }
            if (extPx == Vector2.zero)
            {
                SpriteRenderer sr = child.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                    extPx = new Vector2(sr.bounds.extents.x * ppu, sr.bounds.extents.y * ppu);
            }

            minDx = Mathf.Max(minDx, wsL + extPx.x - sp.x);
            maxDx = Mathf.Min(maxDx, wsR - extPx.x - sp.x);
            minDy = Mathf.Max(minDy, wsB + extPx.y - sp.y);
            maxDy = Mathf.Min(maxDy, wsT - extPx.y - sp.y);
        }

        return new Vector2(
            Mathf.Clamp(screenDelta.x, minDx, maxDx),
            Mathf.Clamp(screenDelta.y, minDy, maxDy));
    }

    // ── Object clamping (called by SimPanelLayoutManager after panel toggle) ─────────────────────

    /// <summary>
    /// After a panel toggle changes the workspace size, moves any placed object whose
    /// collider would be outside the new workspace boundary to the nearest edge.
    /// The camera position and zoom are not changed.
    /// </summary>
    public void ClampObjectsToWorkspace()
    {
        if (workspaceCamera == null || workspaceRect == null) return;
        if (GameManager.Instance == null) return;

        Transform container = GameManager.Instance.ActiveWorldContainer;
        if (container == null) return;

        float sf   = _canvas != null ? _canvas.scaleFactor : 1f;
        Vector2 oMin = workspaceRect.offsetMin;
        Vector2 oMax = workspaceRect.offsetMax;
        float wsL = oMin.x * sf;
        float wsB = oMin.y * sf;
        float wsR = Screen.width  + oMax.x * sf;
        float wsT = Screen.height + oMax.y * sf;

        float ppu = Screen.height / (2f * workspaceCamera.orthographicSize);

        foreach (Transform child in container)
        {
            if (!child.gameObject.activeInHierarchy) continue;

            Vector2 sp = workspaceCamera.WorldToScreenPoint(child.position);

            // Collect extents from the first enabled collider, fall back to SpriteRenderer
            Vector2 extPx = Vector2.zero;
            foreach (Collider2D col in child.GetComponentsInChildren<Collider2D>())
            {
                if (!col.enabled) continue;
                extPx = new Vector2(col.bounds.extents.x * ppu, col.bounds.extents.y * ppu);
                break;
            }
            if (extPx == Vector2.zero)
            {
                SpriteRenderer sr = child.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                    extPx = new Vector2(sr.bounds.extents.x * ppu, sr.bounds.extents.y * ppu);
            }

            float cx = Mathf.Clamp(sp.x, wsL + extPx.x, wsR - extPx.x);
            float cy = Mathf.Clamp(sp.y, wsB + extPx.y, wsT - extPx.y);

            if (Mathf.Abs(cx - sp.x) > 0.5f || Mathf.Abs(cy - sp.y) > 0.5f)
            {
                Vector3 newWorld = workspaceCamera.ScreenToWorldPoint(new Vector3(cx, cy, 10f));
                newWorld.z = 0f;
                child.position = newWorld;
            }
        }
    }

    // ── Detail-panel zoom save/restore ───────────────────────────────────────────────────────────

    /// <summary>
    /// Called by GameManager when any detail panel opens.
    /// Saves the current viewport and snaps the camera to its authored default zoom so objects
    /// always appear at the same size inside the detail panel regardless of workspace zoom level.
    /// </summary>
    public void EnterDetailPanel()
    {
        if (_isInDetailPanel || workspaceCamera == null) return;
        _isInDetailPanel = true;

        // Save the user's current viewport
        _savedOrthoSize = workspaceCamera.orthographicSize;
        _savedCameraPos = workspaceCamera.transform.position;

        // Stop any running animation and snap to the authored default zoom
        if (_animCoroutine != null) { StopCoroutine(_animCoroutine); _animCoroutine = null; }
        workspaceCamera.orthographicSize   = _defaultOrthoSize;
        workspaceCamera.transform.position = _defaultCameraPos;
        _targetOrthoSize = _defaultOrthoSize;

        _isPanning = false; // cancel any in-progress pan
    }

    /// <summary>
    /// Called by GameManager when the detail panel fully closes.
    /// Restores the viewport the user had before opening the panel.
    /// </summary>
    public void ExitDetailPanel()
    {
        if (!_isInDetailPanel || workspaceCamera == null) return;
        _isInDetailPanel = false;

        if (_animCoroutine != null) { StopCoroutine(_animCoroutine); _animCoroutine = null; }
        workspaceCamera.orthographicSize   = _savedOrthoSize;
        workspaceCamera.transform.position = _savedCameraPos;
        _targetOrthoSize = _savedOrthoSize;
    }

    // ── Internals ─────────────────────────────────────────────────────────────────────────────

    private void SmoothZoom(float targetSize, Vector3 targetCamPos)
    {
        _targetOrthoSize = targetSize;
        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(Animate(targetSize, targetCamPos));
    }

    private IEnumerator Animate(float targetSize, Vector3 targetCamPos)
    {
        float   startSize   = workspaceCamera.orthographicSize;
        Vector3 startCamPos = workspaceCamera.transform.position;
        float   elapsed     = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / animDuration));

            workspaceCamera.orthographicSize    = Mathf.LerpUnclamped(startSize,   targetSize,   t);
            workspaceCamera.transform.position  = Vector3.LerpUnclamped(startCamPos, targetCamPos, t);

            yield return null;
        }

        workspaceCamera.orthographicSize   = targetSize;
        workspaceCamera.transform.position = targetCamPos;
        _animCoroutine = null;
    }

    private Camera GetUICamera() => _canvas != null ? _canvas.worldCamera : null;
}
