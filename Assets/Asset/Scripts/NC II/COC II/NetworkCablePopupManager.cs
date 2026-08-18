using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Manages the cable connection popup panel.
/// All visual layout is set up manually in the Unity scene — this script only
/// populates content, positions the panel, and handles show/hide.
/// </summary>
public class NetworkCablePopupManager : MonoBehaviour
{
    // ----------------------------------------------------------------
    //  Singleton
    // ----------------------------------------------------------------

    public static NetworkCablePopupManager Instance { get; private set; }

    /// <summary>True while the popup is visible. Suppresses cable and holder input.</summary>
    public static bool IsOpen { get; private set; }

    // ----------------------------------------------------------------
    //  Inspector references — all assigned manually in the scene
    // ----------------------------------------------------------------

    [Header("Canvas")]
    [Tooltip("The Canvas this popup lives on. Needed to convert screen position into canvas local space.")]
    [SerializeField] private Canvas canvas;

    [Header("Popup Root")]
    [Tooltip("Full-screen transparent overlay button — dismisses the popup on outside click.")]
    [SerializeField] private GameObject overlay;

    [Tooltip("The popup panel root RectTransform. Pivot AND Anchor must both be top-left (0, 1).")]
    [SerializeField] private RectTransform popupPanel;

    [Header("Panel Children")]
    [Tooltip("TextMeshProUGUI that shows the clicked device name as the title.")]
    [SerializeField] private TextMeshProUGUI titleLabel;

    [Tooltip("Parent Transform where CablePopupRow instances are spawned. Needs a VerticalLayoutGroup.")]
    [SerializeField] private Transform contentContainer;

    [Header("Row Prefab")]
    [Tooltip("Prefab with a CablePopupRow component — one spawned per connected cable.")]
    [SerializeField] private GameObject rowPrefab;

    // ----------------------------------------------------------------
    //  Lifecycle
    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        IsOpen   = false;   // static — always reset on Awake for editor re-play safety

        // Push the canvas plane very close to the camera so it renders in front of all
        // world-space objects (LineRenderers, sprites at z=0). Screen Space - Camera
        // canvases draw order relative to world geometry is controlled by Plane Distance,
        // not by sortingOrder. At Plane Distance 1, the canvas plane sits between the
        // camera and the scene, guaranteeing it draws on top.
        if (canvas != null)
            canvas.planeDistance = 1f;

        // Wire overlay dismiss in code so the Inspector doesn't need it.
        if (overlay != null)
        {
            var btn = overlay.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(Hide);
        }

        overlay?.SetActive(false);
        popupPanel?.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; IsOpen = false; }
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Hide();
    }

    // ----------------------------------------------------------------
    //  Public API
    // ----------------------------------------------------------------

    /// <summary>
    /// Populates and shows the popup for all Connected cables at <paramref name="port"/>.
    /// <paramref name="screenPos"/> is the raw mouse position in screen pixels (Input System).
    /// </summary>
    public void Show(NetworkDevicePort port, Vector2 screenPos)
    {
        // Collect only fully-connected cables.
        var cables = new List<NetworkLogicalCable>();
        foreach (var cable in port.ConnectedCables)
            if (cable.GetOtherPort(port) != null)
                cables.Add(cable);

        if (cables.Count == 0) return;

        // Title.
        if (titleLabel != null)
            titleLabel.text = GetDisplayName(port.gameObject) + "  —  Connections";

        // Clear previous rows immediately — Destroy() is deferred and would inflate
        // the ContentSizeFitter calculation that runs right after spawning new rows.
        for (int i = contentContainer.childCount - 1; i >= 0; i--)
            DestroyImmediate(contentContainer.GetChild(i).gameObject);

        // Spawn one row per cable.
        foreach (var cable in cables)
        {
            GameObject rowGO = Instantiate(rowPrefab, contentContainer);
            var row = rowGO.GetComponent<CablePopupRow>();
            if (row == null) continue;

            bool   moveBlocked   = cable.IsMoveEndBlocked(port);
            bool   removeBlocked = cable.IsRemoveCableBlocked();
            string otherName     = cable.GetOtherPortDisplayName(port);

            row.Setup(otherName, moveBlocked, removeBlocked);

            if (!moveBlocked)
            {
                var localCable = cable;
                var localPort  = port;
                row.MoveEndButton.onClick.AddListener(() => { Hide(); localCable.DetachEnd(localPort); });
            }

            if (!removeBlocked)
            {
                var localCable = cable;
                row.RemoveCableButton.onClick.AddListener(() => { Hide(); localCable.Disconnect(); });
            }
        }

        // Activate before layout rebuild — ForceRebuildLayoutImmediate needs active objects
        // to correctly compute ContentSizeFitter-driven dimensions.
        overlay?.SetActive(true);
        popupPanel.gameObject.SetActive(true);

        LayoutRebuilder.ForceRebuildLayoutImmediate(popupPanel);

        // Position AFTER layout so popupPanel.rect.width/height are populated.
        PositionPanel(screenPos);

        IsOpen = true;
    }

    public void Hide()
    {
        overlay?.SetActive(false);
        popupPanel?.gameObject.SetActive(false);
        IsOpen = false;
    }

    // ----------------------------------------------------------------
    //  Positioning
    // ----------------------------------------------------------------

    private void PositionPanel(Vector2 screenPos)
    {
        if (canvas == null || popupPanel == null) return;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        // Convert screen-pixel position to canvas local space.
        // canvas.worldCamera is the camera assigned to a Screen Space - Camera canvas.
        // For Screen Space - Overlay canvases it returns null, which is also correct.
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, canvas.worldCamera, out Vector2 localPoint);

        // Place the panel's pivot (top-left) at the cursor's canvas-local position.
        popupPanel.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);

        // Clamp so the panel never bleeds off the canvas edges.
        float canvasW = canvasRect.rect.width;
        float canvasH = canvasRect.rect.height;
        float panelW  = popupPanel.rect.width;
        float panelH  = popupPanel.rect.height;

        // Canvas local space: x in [-canvasW/2, canvasW/2], y in [-canvasH/2, canvasH/2].
        // Panel pivot is top-left: it extends right (x+panelW) and down (y-panelH).
        Vector3 pos = popupPanel.localPosition;
        pos.x = Mathf.Clamp(pos.x, -canvasW * 0.5f,          canvasW * 0.5f - panelW);
        pos.y = Mathf.Clamp(pos.y, -canvasH * 0.5f + panelH, canvasH * 0.5f);
        popupPanel.localPosition = pos;
    }

    // ----------------------------------------------------------------
    //  Helpers
    // ----------------------------------------------------------------

    private static string GetDisplayName(GameObject go)
    {
        if (go == null) return "Unknown";
        var drag = go.GetComponentInParent<NetworkDragPrefab>();
        return drag != null ? drag.LogDisplayName : go.name;
    }
}
