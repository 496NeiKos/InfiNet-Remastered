using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NetworkHardwareHolder : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
                                     IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hardware Reference")]
    public GameObject hardwarePrefab;

    [Header("Spawn in Workspace")]
    [Tooltip("When true the prefab starts active in the workspace and the holder stays hidden.")]
    [SerializeField] private bool startInWorkspace = false;

    [Header("Slot Install Proximity (world units)")]
    public float slotInstallRadius = 1.5f;

    [Header("Info Panel")]
    [SerializeField] private Sprite infoImage;
    [SerializeField] private string infoName;
    [TextArea(3, 6)]
    [SerializeField] private string infoDescription;

    private Coroutine _hoverCoroutine;
    private GameObject _dragIndicator;
    private bool _isDragging = false;
    private Vector3 _worldScale;

    private void Start()
    {
        if (hardwarePrefab != null)
        {
            _worldScale = hardwarePrefab.transform.lossyScale;

            if (!startInWorkspace)
                hardwarePrefab.SetActive(false);
        }

        bool prefabInactive = hardwarePrefab == null || !hardwarePrefab.activeSelf;
        gameObject.SetActive(prefabInactive);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(infoName)) return;
        _hoverCoroutine = StartCoroutine(ShowInfoAfterDelay());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CancelHover();
    }

    private IEnumerator ShowInfoAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        HardwareInfoPanel.Instance?.Show(infoImage, infoName, infoDescription);
        _hoverCoroutine = null;
    }

    private void CancelHover()
    {
        if (_hoverCoroutine != null)
        {
            StopCoroutine(_hoverCoroutine);
            _hoverCoroutine = null;
        }
    }

    public bool IsAvailable() => hardwarePrefab != null && !hardwarePrefab.activeSelf;

    public void StoreHardware()
    {
        if (hardwarePrefab == null) return;
        hardwarePrefab.transform.SetParent(GameManager.Instance.ActiveHardwareStorageContainer, true);
        hardwarePrefab.SetActive(false);
        gameObject.SetActive(true);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsAvailable()) return;

        CancelHover();
        HardwareInfoPanel.Instance?.Hide();

        _isDragging = true;

        _dragIndicator = new GameObject("DragIndicator");
        SpriteRenderer sr = _dragIndicator.AddComponent<SpriteRenderer>();
        sr.sprite = hardwarePrefab.GetComponent<SpriteRenderer>()?.sprite;
        sr.sortingOrder = 999;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f));
        worldPos.z = 0f;
        _dragIndicator.transform.position = worldPos;
        _dragIndicator.transform.localScale = _worldScale;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || _dragIndicator == null) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f));
        worldPos.z = 0f;
        _dragIndicator.transform.position = worldPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragIndicator != null) { Destroy(_dragIndicator); _dragIndicator = null; }
        if (!_isDragging) return;
        _isDragging = false;

        bool onWorkspace = RectTransformUtility.RectangleContainsScreenPoint(
            GameManager.Instance.workspaceArea, eventData.position, eventData.pressEventCamera);
        if (!onWorkspace) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f));
        worldPos.z = 0f;

        hardwarePrefab.transform.SetParent(GameManager.Instance.ActiveWorldContainer, false);
        hardwarePrefab.transform.position = worldPos;
        ApplyWorldScale(hardwarePrefab.transform, _worldScale);
        hardwarePrefab.SetActive(true);

        NetworkDragPrefab dp = hardwarePrefab.GetComponent<NetworkDragPrefab>();
        if (dp != null) dp.enabled = true;

        gameObject.SetActive(false);
    }

    private void ApplyWorldScale(Transform t, Vector3 targetWorldScale)
    {
        t.localScale = Vector3.one;
        Vector3 ls = t.lossyScale;
        t.localScale = new Vector3(
            targetWorldScale.x / ls.x,
            targetWorldScale.y / ls.y,
            targetWorldScale.z / ls.z);
    }
}
