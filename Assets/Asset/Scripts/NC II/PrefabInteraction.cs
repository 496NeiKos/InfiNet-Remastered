using UnityEngine;
using UnityEngine.InputSystem;

public class PrefabInteraction : MonoBehaviour
{
    // Resolved to SystemUnitController, MonitorController, AVRController, etc.
    private IHardwareController _controller;
    private MotherboardController _mbController;
    private MotherboardPhaseManager _phaseManager;
    private GameObject _detailedView;

    private SpriteRenderer _rootSprite;

    void Start()
    {
        _controller = GetComponent<IHardwareController>();
        _mbController = GetComponent<MotherboardController>();
        _phaseManager = GetComponent<MotherboardPhaseManager>();
        _rootSprite = GetComponent<SpriteRenderer>();

        foreach (Transform child in transform)
        {
            if (child.name.Contains("Detailed"))
            {
                _detailedView = child.gameObject;
                break;
            }
        }
    }

    void Update()
    {
        if (!Mouse.current.rightButton.wasPressedThisFrame) return;

        if (IsInstalledInSlot()) return;

        // Motherboard-level components (CPU, GPU, RAM, PSU, HDD, SSD, Heatsink) must
        // not open a detail view when loose in the workspace — only in the editing panel.
        if (IsMotherboardComponent() && IsInWorldRoot()) return;

        if (GameManager.Instance != null && GameManager.Instance.IsEditorOpen) return;

        if (!IsMouseOver()) return;

        if (_mbController != null)
        {
            if (!_mbController.IsPhase1Complete())
            {
                Debug.Log($"{name} -> Phase 1 incomplete: unscrew, detach cables, and drag motherboard out first.");
                return;
            }
            if (_phaseManager != null) _phaseManager.SetPhase2Interactive();
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OpenEditor(this);
        else
            ShowDetailCentered();
    }

    // Reactively set root alpha to 0 whenever this component is being shown in a
    // detail view — either because it was reparented to an editing layer (top-level
    // editor) or because its Detailed child is active (inner panel from within another
    // editor). Alpha returns to 1 the moment neither condition holds.
    void LateUpdate()
    {
        if (_rootSprite == null) return;

        float target = ShouldHideRoot() ? 0f : 1f;

        Color c = _rootSprite.color;
        if (!Mathf.Approximately(c.a, target))
        {
            c.a = target;
            _rootSprite.color = c;
        }
    }

    private bool ShouldHideRoot()
    {
        if (GameManager.Instance == null) return false;

        // Reparented to an editing layer → this is the active top-level editor
        Transform p = transform.parent;
        if (GameManager.Instance.firstLayer  != null && p == GameManager.Instance.firstLayer.transform)  return true;
        if (GameManager.Instance.secondLayer != null && p == GameManager.Instance.secondLayer.transform) return true;
        if (GameManager.Instance.thirdLayer  != null && p == GameManager.Instance.thirdLayer.transform)  return true;

        // Detailed child is active → inner panel opened from within another editor
        if (_detailedView != null && _detailedView.activeSelf) return true;

        // Bystander in worldRoot while another component's detail view is open
        if (GameManager.Instance.IsEditorOpen && IsInWorldRoot()) return true;

        return false;
    }

    private bool IsInstalledInSlot()
    {
        return GetComponentInParent<SlotContainer>() != null;
    }

    // True for components that live inside the motherboard (CPU, Heatsink, RAM, GPU,
    // PSU, HDD, SSD) — i.e. everything that is NOT SystemUnit/Monitor/AVR/Motherboard.
    // SystemUnit/Monitor/AVR implement IHardwareController; Motherboard has MotherboardController.
    private bool IsMotherboardComponent()
    {
        return _controller == null && _mbController == null;
    }

    private bool IsInWorldRoot()
    {
        return GameManager.Instance != null
            && transform.parent == GameManager.Instance.worldRoot;
    }

    private bool IsMouseOver()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        return hit.collider != null && hit.collider.gameObject == gameObject;
    }

    public void ShowDetailCentered()
    {
        if (_controller != null)
        {
            _controller.ShowDetailAtCenter();
            return;
        }

        // Fallback: Motherboard / plain hardware with a Detailed child
        if (_detailedView == null) return;

        GameObject layer = GameManager.Instance?.firstLayer;
        if (layer != null)
        {
            RectTransform rect = layer.GetComponent<RectTransform>();
            if (rect != null)
            {
                Vector3 panelCenter = rect.TransformPoint(
                    new Vector3(rect.rect.center.x, rect.rect.center.y, 0f));
                panelCenter.z = 0f;
                transform.position = panelCenter;
            }
        }

        _detailedView.SetActive(true);
    }

    public void OnEditorClosed()
    {
        if (_controller != null)
            _controller.HideDetail();
        else if (_detailedView != null)
            _detailedView.SetActive(false);
    }

    public void CloseEditor()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.CloseEditor();
        else
            OnEditorClosed();
    }
}
