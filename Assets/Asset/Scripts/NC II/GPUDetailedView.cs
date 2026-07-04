using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On GPUDetailed — manages switching between the top-view and side-view sub-panels.
/// Keyboard 1/2 switches views. ApplyHardwareInteractable() is public so GPULatchSideView
/// can call it after latch state changes.
/// </summary>
public class GPUDetailedView : MonoBehaviour
{
    [Header("Sub-Views")]
    [SerializeField] private GameObject topView;
    [SerializeField] private GameObject sideView;

    private static readonly string[] Labels = { "Top", "Side" };

    private HardwareAngleIndicator Indicator => GameManager.Instance?.angleIndicator;

    private GPUController _gpuController;
    private int _activeIndex;

    private void Awake()
    {
        _gpuController = GetComponentInParent<GPUController>();
    }

    private void OnEnable()
    {
        Indicator?.Setup(Labels);

        ShowViewAt(_activeIndex);
        ApplyHardwareInteractable();
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsEditorOpen) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) ShowViewAt(0);
        else if (kb.digit2Key.wasPressedThisFrame) ShowViewAt(1);
    }

    private void OnDisable()
    {
        topView?.SetActive(false);
        sideView?.SetActive(false);
        Indicator?.Hide();
        _gpuController?.RefreshCableSprite();
    }

    public void ApplyHardwareInteractable()
    {
        if (_gpuController == null) return;
        bool interactable = _gpuController.IsLatched;

        foreach (var sc in _gpuController.GetComponentsInChildren<ScrewController>(true))
        {
            sc.enabled = interactable;
            foreach (Collider2D col in sc.GetComponents<Collider2D>())
                col.enabled = interactable;
        }
    }

    private void ShowViewAt(int index)
    {
        topView?.SetActive(false);
        sideView?.SetActive(false);

        bool showSide = index == 1;
        if (!showSide && topView != null)
        {
            topView.SetActive(true);
            _activeIndex = 0;
        }
        else if (showSide && sideView != null)
        {
            sideView.SetActive(true);
            _activeIndex = 1;
        }

        Indicator?.SetActive(_activeIndex);
        _gpuController?.SetCableIndicatorForView(showSide);
    }
}
