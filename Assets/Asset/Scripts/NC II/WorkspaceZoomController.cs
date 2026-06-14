using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Zoom controller for the COC I workspace camera.
///
/// Two modes:
///   Scroll wheel  — zooms in/out while the mouse is over the workspace panel.
///   AutoFit()     — called by SimPanelLayoutManager after a panel toggle animation
///                   completes; computes the orthographic size needed to keep every
///                   placed object visible inside the current workspace viewport and
///                   animates to it.
///
/// SCENE SETUP
///   1. Attach this script to the same SimPanelManager GameObject (or any persistent GO).
///   2. workspaceCamera — the Camera that renders the 3D hardware objects.
///   3. workspaceRect   — the RectTransform of the Workspace UI panel (same reference
///                        already used by SimPanelLayoutManager).
/// </summary>
public class WorkspaceZoomController : MonoBehaviour
{
    public static WorkspaceZoomController Instance { get; private set; }

    [Header("References")]
    [Tooltip("The Camera that renders the 3D hardware objects in the workspace.")]
    [SerializeField] private Camera workspaceCamera;
    [Tooltip("RectTransform of the Workspace UI panel – used to determine which " +
             "fraction of the screen the workspace occupies.")]
    [SerializeField] private RectTransform workspaceRect;

    [Header("Scroll Zoom")]
    [Tooltip("Orthographic size change per scroll unit.")]
    [SerializeField] private float scrollStep = 0.3f;
    [SerializeField] private float minOrthoSize = 1f;
    [SerializeField] private float maxOrthoSize = 14f;

    [Header("Auto-Fit")]
    [Tooltip("Multiplier applied to the bounding box before fitting – adds breathing room.")]
    [SerializeField] private float autoFitPadding = 1.25f;

    [Header("Animation")]
    [SerializeField] private float animDuration = 0.25f;

    private float _defaultOrthoSize;
    private float _targetOrthoSize;
    private Coroutine _zoomAnim;

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
            _targetOrthoSize  = _defaultOrthoSize;
        }
    }

    private void Update()
    {
        if (workspaceCamera == null || workspaceRect == null) return;
        if (Mouse.current == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll == 0f) return;

        // Only respond when the mouse is actually over the workspace panel
        Vector2 mousePos = Mouse.current.position.ReadValue();
        if (!RectTransformUtility.RectangleContainsScreenPoint(workspaceRect, mousePos, GetUICamera()))
            return;

        float next = Mathf.Clamp(_targetOrthoSize - scroll * scrollStep, minOrthoSize, maxOrthoSize);
        ZoomTo(next);
    }

    // ── Called by SimPanelLayoutManager at the end of every panel toggle animation ─────────

    /// <summary>
    /// Computes the smallest orthographic size that keeps every placed object visible
    /// inside the current workspace viewport, then animates to it.
    /// Falls back to the default size if no objects are placed.
    /// </summary>
    public void AutoFit()
    {
        if (workspaceCamera == null) return;

        List<Bounds> objectBounds = CollectPlacedObjectBounds();

        if (objectBounds.Count == 0)
        {
            ZoomTo(_defaultOrthoSize);
            return;
        }

        // Combine all object bounds into one world-space AABB
        Bounds total = objectBounds[0];
        for (int i = 1; i < objectBounds.Count; i++)
            total.Encapsulate(objectBounds[i]);

        // Get workspace panel screen-pixel dimensions
        GetWorkspaceScreenSize(out float wsScreenW, out float wsScreenH);
        if (wsScreenW <= 0f || wsScreenH <= 0f) return;

        float screenW = Screen.width;
        float screenH = Screen.height;
        float aspect  = screenW / screenH;

        // Fractions of the full screen that the workspace occupies
        float wsFracW = wsScreenW / screenW;
        float wsFracH = wsScreenH / screenH;

        // The camera's orthographic size (s) controls how many world units fit on screen:
        //   full-screen world height = 2s
        //   full-screen world width  = 2s * aspect
        // Through the workspace viewport:
        //   workspace world height   = 2s * wsFracH
        //   workspace world width    = 2s * aspect * wsFracW
        //
        // We need both padded dimensions of the bounding box to fit, so:
        //   s >= (total.size.y * padding) / (2 * wsFracH)          [height constraint]
        //   s >= (total.size.x * padding) / (2 * aspect * wsFracW) [width  constraint]

        float paddedH = total.size.y * autoFitPadding;
        float paddedW = total.size.x * autoFitPadding;

        float sizeForHeight = paddedH / (2f * wsFracH);
        float sizeForWidth  = paddedW / (2f * aspect * wsFracW);

        float target = Mathf.Max(sizeForHeight, sizeForWidth);
        target = Mathf.Clamp(target, minOrthoSize, maxOrthoSize);

        ZoomTo(target);
    }

    // ── Internals ────────────────────────────────────────────────────────────────────────────

    private void ZoomTo(float target)
    {
        _targetOrthoSize = target;
        if (_zoomAnim != null) StopCoroutine(_zoomAnim);
        _zoomAnim = StartCoroutine(AnimateZoom(target));
    }

    private IEnumerator AnimateZoom(float target)
    {
        float start   = workspaceCamera.orthographicSize;
        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / animDuration));
            workspaceCamera.orthographicSize = Mathf.LerpUnclamped(start, target, t);
            yield return null;
        }

        workspaceCamera.orthographicSize = target;
        _zoomAnim = null;
    }

    /// <summary>
    /// Collects the world-space Bounds of every active child of ActiveWorldContainer.
    /// Uses SpriteRenderer bounds if available, otherwise falls back to a minimal
    /// point-bounds at the object's position.
    /// </summary>
    private static List<Bounds> CollectPlacedObjectBounds()
    {
        var result = new List<Bounds>();
        if (GameManager.Instance == null) return result;

        Transform container = GameManager.Instance.ActiveWorldContainer;
        if (container == null) return result;

        foreach (Transform child in container)
        {
            if (!child.gameObject.activeInHierarchy) continue;

            Bounds b = new Bounds(child.position, Vector3.zero);
            bool found = false;

            foreach (SpriteRenderer sr in child.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!sr.gameObject.activeInHierarchy) continue;
                if (!found) { b = sr.bounds; found = true; }
                else          b.Encapsulate(sr.bounds);
            }

            if (!found)
                b = new Bounds(child.position, Vector3.one * 0.5f);

            result.Add(b);
        }

        return result;
    }

    /// <summary>
    /// Returns the workspace panel's current size in screen pixels.
    /// Works correctly for Screen Space – Camera canvases by projecting
    /// GetWorldCorners() through the UI camera.
    /// </summary>
    private void GetWorkspaceScreenSize(out float width, out float height)
    {
        Vector3[] corners = new Vector3[4];
        workspaceRect.GetWorldCorners(corners);

        Camera uiCam = GetUICamera();
        Vector2 screenBL = RectTransformUtility.WorldToScreenPoint(uiCam, corners[0]);
        Vector2 screenTR = RectTransformUtility.WorldToScreenPoint(uiCam, corners[2]);

        width  = Mathf.Abs(screenTR.x - screenBL.x);
        height = Mathf.Abs(screenTR.y - screenBL.y);
    }

    private Camera GetUICamera()
    {
        Canvas canvas = workspaceRect != null
            ? workspaceRect.GetComponentInParent<Canvas>()
            : null;
        return canvas != null ? canvas.worldCamera : null;
    }
}
