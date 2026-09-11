using UnityEngine;

/// <summary>
/// On the RAMDetailedView child of RAM1 / RAM2.
/// Displays the RAM latch state (installed vs uninstalled sprite) and pushes the parent
/// RAM sprite and indicator behind the detail panel while it is open.
/// Gesture detection lives on the RAMLatchController children (LeftLatch, RightLatch).
/// </summary>
public class RAMDetailedView : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite installedSprite;
    [SerializeField] private Sprite uninstalledSprite;

    private SpriteRenderer _sr;
    private RAMController _ramController;

    private bool _ordersSaved;
    private int _rootOriginalOrder;
    private int _indicatorOriginalOrder;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _ramController = GetComponentInParent<RAMController>();
    }

    private void OnEnable()
    {
        SyncSprite();
        PushSortingOrders();
    }

    public void SyncSprite()
    {
        if (_sr == null || _ramController == null) return;
        bool bothLatched   = _ramController.IsLeftLatched && _ramController.IsRightLatched;
        bool bothUnlatched = !_ramController.IsLeftLatched && !_ramController.IsRightLatched;
        if      (bothLatched)   _sr.sprite = installedSprite;
        else if (bothUnlatched) _sr.sprite = uninstalledSprite;
        // mixed state — one latch still engaged, sprite unchanged
    }

    private void OnDisable()
    {
        RestoreSortingOrders();
    }

    private void PushSortingOrders()
    {
        SpriteRenderer rootSR      = transform.parent?.GetComponent<SpriteRenderer>();
        SpriteRenderer indicatorSR = FindSiblingIndicator()?.GetComponent<SpriteRenderer>();

        _rootOriginalOrder      = rootSR      != null ? rootSR.sortingOrder      : 0;
        _indicatorOriginalOrder = indicatorSR != null ? indicatorSR.sortingOrder : 0;
        _ordersSaved = true;

        if (rootSR      != null) rootSR.sortingOrder      = -1;
        if (indicatorSR != null) indicatorSR.sortingOrder = -1;
    }

    private void RestoreSortingOrders()
    {
        if (!_ordersSaved) return;
        _ordersSaved = false;

        SpriteRenderer rootSR      = transform.parent?.GetComponent<SpriteRenderer>();
        SpriteRenderer indicatorSR = FindSiblingIndicator()?.GetComponent<SpriteRenderer>();

        if (rootSR      != null) rootSR.sortingOrder      = _rootOriginalOrder;
        if (indicatorSR != null) indicatorSR.sortingOrder = _indicatorOriginalOrder;
    }

    private Transform FindSiblingIndicator()
    {
        if (transform.parent == null) return null;
        foreach (Transform sibling in transform.parent)
            if (sibling != transform && sibling.name.Contains("Indicator"))
                return sibling;
        return null;
    }
}
