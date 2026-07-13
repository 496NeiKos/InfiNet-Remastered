using UnityEngine;

/// <summary>
/// Drives a "Reset Scene" confirmation popup.
///
/// Setup in the Inspector:
///   1. Assign confirmationPanel — the root GameObject of the popup (disabled at start).
///   2. Wire the reset button's onClick → ShowConfirmation()
///   3. Wire the Yes button's onClick  → ConfirmReset()
///   4. Wire the No button's onClick   → CancelReset()
/// </summary>
public class SceneResetManager : MonoBehaviour
{
    [SerializeField] private GameObject confirmationPanel;

    private void Start()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }

    public void ShowConfirmation()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsEditorOpen)
            GameManager.Instance.CloseEditor();

        if (confirmationPanel != null)
            confirmationPanel.SetActive(true);
    }

    public void ConfirmReset()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);

        SceneController.Instance?.ReloadScene();
    }

    public void CancelReset()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }
}
