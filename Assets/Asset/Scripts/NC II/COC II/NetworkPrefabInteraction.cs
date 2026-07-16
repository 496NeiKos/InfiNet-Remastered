using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkPrefabInteraction : MonoBehaviour
{
    private SpriteRenderer _rootSprite;
    private GameObject _detailView;

    private void Start()
    {
        _rootSprite = GetComponent<SpriteRenderer>();

        foreach (Transform child in transform)
        {
            if (child.name.Contains("Detail"))
            {
                _detailView = child.gameObject;
                break;
            }
        }
    }

    private void Update()
    {
        if (!Mouse.current.rightButton.wasPressedThisFrame) return;
        if (GameManager.Instance == null || GameManager.Instance.IsEditorOpen) return;
        if (!IsInWorldRoot()) return;
        if (!IsMouseOver()) return;

        GameManager.Instance.OpenNetworkEditor(this);
    }

    // Hides this object's sprite while it is the active editor (reparented to firstLayer),
    // and hides all bystanders in the workspace while any editor is open.
    private void LateUpdate()
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

        // This object is the active editor — hide the root sprite so only the detail view shows
        if (GameManager.Instance.firstLayer != null &&
            transform.parent == GameManager.Instance.firstLayer.transform) return true;

        // Bystander in workspace while another object's editor is open
        if (GameManager.Instance.IsEditorOpen && IsInWorldRoot()) return true;

        return false;
    }

    // Called by GameManager.OpenNetworkEditor — centers this object in firstLayer
    // and activates its detail child (e.g. RouterDetail, SwitchDetail).
    public void ShowDetailCentered()
    {
        GameObject layer = GameManager.Instance?.firstLayer;
        if (layer == null) return;

        RectTransform rect = layer.GetComponent<RectTransform>();
        if (rect != null)
        {
            Vector3 center = rect.TransformPoint(
                new Vector3(rect.rect.center.x, rect.rect.center.y, 0f));
            center.z = 0f;
            transform.position = center;
        }

        if (_detailView != null)
            _detailView.SetActive(true);
    }

    // Called by GameManager.CloseEditor — hides the detail child.
    public void OnEditorClosed()
    {
        if (_detailView != null)
            _detailView.SetActive(false);
    }

    private bool IsInWorldRoot()
    {
        if (GameManager.Instance == null) return false;
        Transform worldRoot = GameManager.Instance.worldRoot;
        Transform active    = GameManager.Instance.ActiveWorldContainer;
        return (worldRoot != null && transform.parent == worldRoot) ||
               (active   != null && transform.parent == active);
    }

    private bool IsMouseOver()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        return hit.collider != null && hit.collider.gameObject == gameObject;
    }
}
