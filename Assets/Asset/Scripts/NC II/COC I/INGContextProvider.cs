using UnityEngine;

/// <summary>
/// Attach to any panel or view GameObject. When this object is enabled (SetActive true),
/// it pushes its context onto the InlineNotificationGuide stack. When disabled, it pops.
/// The ING always shows the content for the deepest active context.
///
/// Usage: add this component to a panel, set the Context field in the inspector, done.
/// No script modifications needed — the lifecycle handles everything automatically.
/// </summary>
public class INGContextProvider : MonoBehaviour
{
    [SerializeField] private GuideContext context;

    // Guards against double-push and handles the initial-load race where
    // OnEnable fires before InlineNotificationGuide.Awake sets Instance.
    private bool _pushed;

    private void OnEnable()  => TryPush();
    private void Start()     => TryPush(); // catches the OnEnable null-Instance case

    private void OnDisable()
    {
        if (!_pushed) return;
        _pushed = false;
        InlineNotificationGuide.Instance?.PopContext(context);
    }

    private void TryPush()
    {
        if (_pushed) return;
        if (InlineNotificationGuide.Instance == null) return;
        _pushed = true;
        InlineNotificationGuide.Instance.PushContext(context);
    }
}
