using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Small toast panel that appears on the right side of the screen after a crimping tool action.
/// Attach to the popup panel GameObject. Wire panelBody, messageText, and closeButton in the inspector.
/// </summary>
public class CrimpingFeedbackPopup : MonoBehaviour
{
    public static CrimpingFeedbackPopup Instance { get; private set; }

    [SerializeField] private GameObject panelBody;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button closeButton;
    [SerializeField] private float displayDuration = 3f;

    private Coroutine _dismissCoroutine;

    private void Awake()
    {
        Instance = this;
        if (panelBody != null) panelBody.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    public void ShowMessage(string text)
    {
        if (messageText != null) messageText.text = text;
        if (panelBody != null) panelBody.SetActive(true);

        if (_dismissCoroutine != null) StopCoroutine(_dismissCoroutine);
        _dismissCoroutine = StartCoroutine(DismissAfterDelay());
    }

    public void Close()
    {
        if (_dismissCoroutine != null) { StopCoroutine(_dismissCoroutine); _dismissCoroutine = null; }
        if (panelBody != null) panelBody.SetActive(false);
    }

    private IEnumerator DismissAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        if (panelBody != null) panelBody.SetActive(false);
        _dismissCoroutine = null;
    }
}
