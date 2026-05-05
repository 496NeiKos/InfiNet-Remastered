using UnityEngine;
using UnityEngine.InputSystem;

public class VGACable : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public Collider2D workbenchArea;
    public Collider2D itemArea;

    private Collider2D col;
    private Vector3 originalEditorPosition;
    private Vector3 startDragPosition;
    private bool isDragging = false;

    private InputAction mouseClickAction;
    private InputAction mousePositionAction;

    private bool connectedToSystemUnit = false;
    private bool connectedToMonitor = false;

    private void Awake()
    {
        col = GetComponent<Collider2D>();

        mouseClickAction = new InputAction("MouseClick", binding: "<Mouse>/leftButton");
        mousePositionAction = new InputAction("MousePosition", binding: "<Mouse>/position");

        mouseClickAction.performed += context => OnMouseClickPerformed();
        mouseClickAction.canceled += context => StopDrag();

        mouseClickAction.Enable();
        mousePositionAction.Enable();

        originalEditorPosition = transform.position;
    }

    private void OnDestroy()
    {
        mouseClickAction.Disable();
        mousePositionAction.Disable();
    }

    private void OnMouseClickPerformed()
    {
        AttemptStartDrag();
    }

    private void AttemptStartDrag()
    {
        if (isDragging) return;

        Vector2 mousePos = mousePositionAction.ReadValue<Vector2>();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = transform.position.z;

        RaycastHit2D[] hits = Physics2D.RaycastAll(worldPos, Vector2.zero);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == col)
            {
                isDragging = true;
                startDragPosition = transform.position;

                if (HoverLabelManager.Instance != null)
                {
                    HoverLabelManager.Instance.ShowLabel(gameObject.name.Replace("(Clone)", ""));
                }
                break;
            }
        }
    }

    private void StopDrag()
    {
        isDragging = false;

        if (HoverLabelManager.Instance != null)
        {
            HoverLabelManager.Instance.HideLabel();
        }

        Collider2D[] colliders = Physics2D.OverlapAreaAll(col.bounds.min, col.bounds.max);

        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("SystemUnit"))
            {
                AudioClip sfx = SoundManager.instance.dropSFX;
                SoundManager.instance.PlaySFX(sfx);
                connectedToSystemUnit = !connectedToSystemUnit;
                TroubleshootManager.Instance.ShowMessage(
                    connectedToSystemUnit ? "VGA connected to System Unit." : "VGA disconnected from System Unit.",
                    !connectedToSystemUnit
                );

                UpdateTask3(); // ✅ Task 3 check
                ReturnToOriginal();
                return;
            }

            if (collider.CompareTag("Monitor"))
            {
                AudioClip sfx = SoundManager.instance.dropSFX;
                SoundManager.instance.PlaySFX(sfx);
                connectedToMonitor = !connectedToMonitor;
                TroubleshootManager.Instance.ShowMessage(
                    connectedToMonitor ? "VGA connected to Monitor." : "VGA disconnected from Monitor.",
                    !connectedToMonitor
                );

                UpdateTask3(); // ✅ Task 3 check
                ReturnToOriginal();
                return;
            }
        }

        ReturnToOriginal();
    }

    public bool IsConnectedToSystemUnit()
    {
        return connectedToSystemUnit;
    }

    public bool IsConnectedToMonitor()
    {
        return connectedToMonitor;
    }

    private void ReturnToOriginal()
    {
        transform.position = originalEditorPosition;
        transform.SetParent(null);
    }

    void Update()
    {
        if (isDragging)
        {
            transform.position = GetMousePositionInWorldSpace();

            Vector2 mousePos = mousePositionAction.ReadValue<Vector2>();
            if (HoverLabelManager.Instance != null)
            {
                HoverLabelManager.Instance.FollowMouse(mousePos);
            }
        }
    }

    private Vector3 GetMousePositionInWorldSpace()
    {
        Vector2 mousePos = mousePositionAction.ReadValue<Vector2>();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = 0f;
        return worldPos;
    }

    public bool IsFullyConnected()
    {
        return connectedToSystemUnit && connectedToMonitor;
    }

    // ✅ Task 3 logic
    private void UpdateTask3()
    {
        if (IsFullyConnected())
        {
            Debug.Log("Task 3 complete: VGA cable fully connected.");
            TaskListManager.Instance.SetTaskCompleted(2, true);
        }
        else
        {
            Debug.Log("Task 3 undone: VGA cable not fully connected.");
            TaskListManager.Instance.SetTaskCompleted(2, false);
        }
    }
}
