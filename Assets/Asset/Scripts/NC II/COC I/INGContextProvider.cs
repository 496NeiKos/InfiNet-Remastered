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

    private void OnEnable()
    {
        InlineNotificationGuide.Instance?.PushContext(context);
    }

    private void OnDisable()
    {
        InlineNotificationGuide.Instance?.PopContext(context);
    }
}
