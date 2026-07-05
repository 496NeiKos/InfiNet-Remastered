using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Generic view controller for hardware with multiple angles (Top, Front, Side, Back).
/// Attach to the hardware root. Assign only the views that exist — null views are skipped.
/// Builds a sequential list at start: the first assigned view maps to key 1, the second to
/// key 2, and so on. This means System Unit (Front/Side/Back, no Top) uses keys 1/2/3.
/// </summary>
public class HardwareViewController : MonoBehaviour
{
    [Header("Views (assign only those that exist)")]
    [SerializeField] private GameObject topView;
    [SerializeField] private GameObject frontView;
    [SerializeField] private GameObject sideView;
    [SerializeField] private GameObject backView;

    private static readonly Key[] NumberKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };

    private HardwareAngleIndicator Indicator => GameManager.Instance?.angleIndicator;

    private readonly List<(string label, GameObject view)> _views = new();
    private int _activeIndex;

    private void Start()
    {
        BuildViewList();
        foreach (var (_, view) in _views)
            view.SetActive(false);
    }

    private void BuildViewList()
    {
        _views.Clear();
        if (topView   != null) _views.Add(("Top",   topView));
        if (frontView != null) _views.Add(("Front", frontView));
        if (sideView  != null) _views.Add(("Side",  sideView));
        if (backView  != null) _views.Add(("Back",  backView));
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsEditorOpen) return;
        if (GameManager.Instance.firstLayer == null ||
            !transform.IsChildOf(GameManager.Instance.firstLayer.transform)) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;
        if (kb.shiftKey.isPressed) return; // Shift+number is reserved for content group shortcuts

        for (int i = 0; i < _views.Count && i < NumberKeys.Length; i++)
        {
            if (kb[NumberKeys[i]].wasPressedThisFrame)
            {
                ShowViewAt(i);
                break;
            }
        }
    }

    /// <summary>Called by hardware controllers when the detail panel opens.</summary>
    public void ShowLastActive()
    {
        SetupIndicator();
        ShowViewAt(_activeIndex);
    }

    /// <summary>
    /// Hides the angle indicator. Call from HideDetail() in hardware controllers.
    /// </summary>
    public void HideIndicator() => Indicator?.Hide();

    /// <summary>
    /// Kept for API compatibility — the first view in the list is always the default.
    /// </summary>
    public void SetDefaultIfNone(GameObject defaultFirstView) { }

    public void ShowView(GameObject view)
    {
        int idx = _views.FindIndex(v => v.view == view);
        if (idx >= 0) ShowViewAt(idx);
    }

    private void ShowViewAt(int index)
    {
        if (index < 0 || index >= _views.Count) return;

        foreach (var (_, view) in _views)
            view.SetActive(false);

        _views[index].view.SetActive(true);
        _activeIndex = index;
        CenterView(_views[index].view);
        Indicator?.SetActive(index);
    }

    private void SetupIndicator()
    {
        if (Indicator == null) return;
        var labels = new string[_views.Count];
        for (int i = 0; i < _views.Count; i++)
            labels[i] = _views[i].label;
        Indicator.Setup(labels);
    }

    private void CenterView(GameObject view)
    {
        if (view == null || GameManager.Instance?.firstLayer == null) return;
        RectTransform rect = GameManager.Instance.firstLayer.GetComponent<RectTransform>();
        if (rect == null) return;
        Vector3 center = rect.TransformPoint(new Vector3(rect.rect.center.x, rect.rect.center.y, 0f));
        center.z = 0f;
        view.transform.position = center;
    }
}
