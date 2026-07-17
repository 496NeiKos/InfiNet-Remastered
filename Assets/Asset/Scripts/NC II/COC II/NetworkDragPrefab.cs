using UnityEngine;
using UnityEngine.EventSystems;

public class NetworkDragPrefab : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] public NetworkHardwareHolder hardwareHolder;
    [SerializeField] private GameObject workspaceProxy;
    [Tooltip("Human-readable name shown in the activity log. Auto-derived from the GameObject name if left blank.")]
    [SerializeField] public string displayName;

    [Tooltip("When false the object always snaps back and can never be placed in the workspace.")]
    [SerializeField] public bool canPlaceInWorkspace = true;

    /// <summary>
    /// Invoked when the object snaps back to its original position (dropped outside workspace/hardware area
    /// and canPlaceInWorkspace is false). Used by RJ45HoldUninstall to re-install into the slot.
    /// </summary>
    public System.Action onSnappedBack;

    public string LogDisplayName =>
        !string.IsNullOrEmpty(displayName) ? displayName : SplitCamelCase(name);

    private static string SplitCamelCase(string s) =>
        System.Text.RegularExpressions.Regex.Replace(s,
            @"(?<=[a-z\d])(?=[A-Z])|(?<=[A-Z]+)(?=[A-Z][a-z])", " ").Trim();

    private RectTransform workspaceArea;
    private RectTransform hardwareArea;
    private Canvas _workspaceCanvas;
    private Vector3 _originalPos;
    private bool _isDragging = false;

    private SpriteRenderer _dragIndicator;

    private void Start()
    {
        workspaceArea = GameManager.Instance.workspaceArea;
        hardwareArea  = GameManager.Instance.hardwareArea;
        _workspaceCanvas = workspaceArea != null
            ? workspaceArea.GetComponentInParent<Canvas>()
            : null;
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        Transform worldContainer = GameManager.Instance.ActiveWorldContainer;
        bool inWorldRoot = worldContainer != null && transform.parent == worldContainer;

        if (workspaceProxy != null)
        {
            if (workspaceProxy.activeSelf != inWorldRoot)
                workspaceProxy.SetActive(inWorldRoot);

            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = !inWorldRoot;
        }
        else if (inWorldRoot)
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = true;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (GameManager.Instance.IsEditorOpen)
        {
            Debug.Log($"[NetworkDragPrefab:{name}] BLOCKED — editor open.");
            _isDragging = false;
            return;
        }

        Debug.Log($"[NetworkDragPrefab:{name}] Drag started.");
        _isDragging = true;
        _originalPos = transform.position;

        GameObject indicatorGO = new GameObject("DragIndicator");
        _dragIndicator = indicatorGO.AddComponent<SpriteRenderer>();
        _dragIndicator.sprite = GetComponent<SpriteRenderer>()?.sprite;
        _dragIndicator.sortingOrder = 999;
        indicatorGO.transform.localScale = transform.lossyScale;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f));
        worldPos.z = 0f;
        worldPos = ClampToWorkspace(worldPos, eventData.position, eventData.pressEventCamera);
        transform.position = worldPos;

        if (_dragIndicator != null)
            _dragIndicator.transform.position = worldPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragIndicator != null)
        {
            Destroy(_dragIndicator.gameObject);
            _dragIndicator = null;
        }

        if (!_isDragging) return;
        _isDragging = false;

        bool onHardwareArea = RectTransformUtility.RectangleContainsScreenPoint(
            hardwareArea, eventData.position, eventData.pressEventCamera);

        if (onHardwareArea)
        {
            ActivityLogManager.Log($"{LogDisplayName} returned to storage", ActivityLogManager.EntryType.Remove);
            SendToHolder();
            return;
        }

        bool onWorkspace = RectTransformUtility.RectangleContainsScreenPoint(
            workspaceArea, eventData.position, eventData.pressEventCamera);

        if (!onWorkspace || !canPlaceInWorkspace)
        {
            transform.position = _originalPos;
            onSnappedBack?.Invoke();
        }
    }

    private void SendToHolder()
    {
        if (hardwareHolder != null)
        {
            hardwareHolder.StoreHardware();
            return;
        }

        NetworkHardwareHolder[] allHolders = FindObjectsOfType<NetworkHardwareHolder>(true);
        foreach (NetworkHardwareHolder h in allHolders)
        {
            if (h.hardwarePrefab != null && h.hardwarePrefab.name == gameObject.name)
            {
                hardwareHolder = h;
                h.StoreHardware();
                return;
            }
        }

        Debug.LogWarning($"[NetworkDragPrefab] hardwareHolder not found for '{gameObject.name}' — deactivating in place.");
        gameObject.SetActive(false);
    }

    private Vector3 ClampToWorkspace(Vector3 worldPos, Vector2 cursorScreenPos, Camera eventCamera)
    {
        if (workspaceArea == null || Camera.main == null) return worldPos;

        float   ppu = Screen.height / (2f * Camera.main.orthographicSize);
        Vector2 ext = GetActiveColliderExtents();

        if (hardwareArea != null &&
            RectTransformUtility.RectangleContainsScreenPoint(hardwareArea, cursorScreenPos, eventCamera))
        {
            Canvas rootCanvas = hardwareArea.GetComponentInParent<Canvas>();
            if (rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;
            Camera uiCam = (rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceCamera)
                ? rootCanvas.worldCamera
                : null;

            Vector3[] hwCorners = new Vector3[4];
            hardwareArea.GetWorldCorners(hwCorners);
            Vector2 hwMin = RectTransformUtility.WorldToScreenPoint(uiCam, hwCorners[0]);
            Vector2 hwMax = RectTransformUtility.WorldToScreenPoint(uiCam, hwCorners[2]);

            Vector2 sp = Camera.main.WorldToScreenPoint(worldPos);
            float cx = Mathf.Clamp(sp.x, hwMin.x + ext.x * ppu, hwMax.x - ext.x * ppu);
            float cy = Mathf.Clamp(sp.y, hwMin.y + ext.y * ppu, hwMax.y - ext.y * ppu);

            Vector3 result = Camera.main.ScreenToWorldPoint(new Vector3(cx, cy, 10f));
            result.z = 0f;
            return result;
        }

        float   sf   = _workspaceCanvas != null ? _workspaceCanvas.scaleFactor : 1f;
        Vector2 oMin = workspaceArea.offsetMin;
        Vector2 oMax = workspaceArea.offsetMax;

        float wsL = oMin.x * sf;
        float wsB = oMin.y * sf;
        float wsR = Screen.width  + oMax.x * sf;
        float wsT = Screen.height + oMax.y * sf;

        Vector2 wsp = Camera.main.WorldToScreenPoint(worldPos);
        float cwx = Mathf.Clamp(wsp.x, wsL + ext.x * ppu, wsR - ext.x * ppu);
        float cwy = Mathf.Clamp(wsp.y, wsB + ext.y * ppu, wsT - ext.y * ppu);

        Vector3 res = Camera.main.ScreenToWorldPoint(new Vector3(cwx, cwy, 10f));
        res.z = 0f;
        return res;
    }

    private Vector2 GetActiveColliderExtents()
    {
        Collider2D col = workspaceProxy != null
            ? workspaceProxy.GetComponent<Collider2D>()
            : null;
        if (col == null) col = GetComponent<Collider2D>();
        if (col != null) return new Vector2(col.bounds.extents.x, col.bounds.extents.y);

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) return new Vector2(sr.bounds.extents.x, sr.bounds.extents.y);

        return Vector2.zero;
    }

    private void ApplyWorldScale(Vector3 targetWorldScale)
    {
        Vector3 ls = transform.lossyScale;
        transform.localScale = new Vector3(
            targetWorldScale.x / (ls.x == 0 ? 1 : ls.x),
            targetWorldScale.y / (ls.y == 0 ? 1 : ls.y),
            targetWorldScale.z / (ls.z == 0 ? 1 : ls.z));
    }
}
