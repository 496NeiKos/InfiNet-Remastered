using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (workspaceCamera != null)
        {
            _defaultOrthoSize  = workspaceCamera.orthographicSize;
            _defaultCameraPos  = workspaceCamera.transform.position;
            _targetOrthoSize   = _defaultOrthoSize;
        }
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

            // If pointer is over a UI element (buttons, etc.) skip pan
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            // If cursor is over a 3D physics object, let DragPrefab handle it
            Vector2 worldPos = workspaceCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0f));
            if (Physics2D.Raycast(worldPos, Vector2.zero).collider != null) return;

            // Blank space — start pan; stop any running zoom/fit animation
            if (_animCoroutine != null)
            {
                StopCoroutine(_animCoroutine);
                _animCoroutine = null;
                // Snap target to current so next ZoomTo reads the right value
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

            Vector2 delta = mousePos - _panLastScreenPos;
            _panLastScreenPos = mousePos;

            if (delta.sqrMagnitude < 0.001f) return;

            // Convert screen-pixel delta to world-unit camera shift.
            // Dragging right (delta.x > 0) should make objects appear to move right,
            // so the camera must move left (subtract delta).
            float aspect = (float)Screen.width / Screen.height;
            float wpu_x  = 2f * workspaceCamera.orthographicSize * aspect / Screen.width;
            float wpu_y  = 2f * workspaceCamera.orthographicSize          / Screen.height;

            Vector3 shift = new Vector3(-delta.x * wpu_x, -delta.y * wpu_y, 0f);
            workspaceCamera.transform.position += shift;
        }
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
