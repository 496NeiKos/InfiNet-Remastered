using System.Collections;
using System.Collections.Generic;
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
/// AutoFit()     — called by SimPanelLayoutManager after each panel toggle animation.
///                 Computes the ortho size that keeps every placed object visible inside
///                 the new workspace viewport AND centers the camera on their centroid.
///                 No hard clamp on ortho size so all objects are always reachable.
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

    [Header("Auto-Fit")]
    [Tooltip("Scale multiplier applied to the bounding box — adds breathing room around objects.")]
    [SerializeField] private float autoFitPadding = 1.25f;

    [Header("Animation")]
    [SerializeField] private float animDuration = 0.25f;

    private float _defaultOrthoSize;
    private Vector3 _defaultCameraPos;
    private float   _targetOrthoSize;

    private Coroutine _animCoroutine;

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
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll == 0f) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        if (!RectTransformUtility.RectangleContainsScreenPoint(workspaceRect, mousePos, GetUICamera()))
            return;

        float next = Mathf.Clamp(_targetOrthoSize - scroll * scrollStep, minOrthoSize, maxOrthoSize);
        // Scroll only changes zoom; camera position stays where it is.
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

    // ── Auto-fit (called by SimPanelLayoutManager) ────────────────────────────────────────────

    /// <summary>
    /// Zooms to fit every placed object inside the current workspace viewport and
    /// centers the camera on their bounding-box centroid. No hard upper clamp on
    /// ortho size so all objects are guaranteed to be visible.
    /// </summary>
    public void AutoFit()
    {
        if (workspaceCamera == null) return;

        List<Bounds> objectBounds = CollectPlacedObjectBounds();

        if (objectBounds.Count == 0)
        {
            SmoothZoom(_defaultOrthoSize, _defaultCameraPos);
            return;
        }

        // Merge all object bounds into one world-space AABB
        Bounds total = objectBounds[0];
        for (int i = 1; i < objectBounds.Count; i++)
            total.Encapsulate(objectBounds[i]);

        // Workspace size in screen pixels
        GetWorkspaceScreenSize(out float wsScreenW, out float wsScreenH);
        if (wsScreenW <= 0f || wsScreenH <= 0f) return;

        float aspect  = (float)Screen.width / Screen.height;
        float wsFracW = wsScreenW / Screen.width;
        float wsFracH = wsScreenH / Screen.height;

        // Camera ortho size s → visible through workspace:
        //   world height = 2s * wsFracH,  world width = 2s * aspect * wsFracW
        // Solve for s so padded bounds fit:
        float paddedH = total.size.y * autoFitPadding;
        float paddedW = total.size.x * autoFitPadding;

        float sizeForH = paddedH / (2f * wsFracH);
        float sizeForW = paddedW / (2f * aspect * wsFracW);

        // No clamp to maxOrthoSize here — must show all objects regardless
        float targetSize = Mathf.Max(sizeForH, sizeForW, minOrthoSize);

        // Center the camera on the objects
        Vector3 targetCamPos = new Vector3(
            total.center.x,
            total.center.y,
            workspaceCamera.transform.position.z);

        SmoothZoom(targetSize, targetCamPos);
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

    private static List<Bounds> CollectPlacedObjectBounds()
    {
        var result = new List<Bounds>();
        if (GameManager.Instance == null) return result;

        Transform container = GameManager.Instance.ActiveWorldContainer;
        if (container == null) return result;

        foreach (Transform child in container)
        {
            if (!child.gameObject.activeInHierarchy) continue;

            Bounds b     = new Bounds(child.position, Vector3.zero);
            bool   found = false;

            foreach (SpriteRenderer sr in child.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!sr.gameObject.activeInHierarchy) continue;
                if (!found) { b = sr.bounds; found = true; }
                else          b.Encapsulate(sr.bounds);
            }

            if (!found) b = new Bounds(child.position, Vector3.one * 0.5f);

            result.Add(b);
        }

        return result;
    }

    private void GetWorkspaceScreenSize(out float width, out float height)
    {
        float sf = _canvas != null ? _canvas.scaleFactor : 1f;

        Vector2 oMin = workspaceRect.offsetMin;
        Vector2 oMax = workspaceRect.offsetMax;

        float wsL = oMin.x * sf;
        float wsB = oMin.y * sf;
        float wsR = Screen.width  + oMax.x * sf;
        float wsT = Screen.height + oMax.y * sf;

        width  = Mathf.Max(0f, wsR - wsL);
        height = Mathf.Max(0f, wsT - wsB);
    }

    private Camera GetUICamera() => _canvas != null ? _canvas.worldCamera : null;
}
