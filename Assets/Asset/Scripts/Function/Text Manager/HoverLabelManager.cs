using UnityEngine;
using TMPro;

public class HoverLabelManager : MonoBehaviour
{
    public static HoverLabelManager Instance;

    [Header("Assign your TMP Text here")]
    public TextMeshProUGUI hoverLabel;

    private void Awake()
    {
        Instance = this;

        if (hoverLabel == null)
            hoverLabel = GetComponentInChildren<TextMeshProUGUI>();

        gameObject.SetActive(false); // hide panel at start
    }

    public void ShowLabel(string itemName)
    {
        hoverLabel.text = itemName;
        gameObject.SetActive(true);
    }

    public void HideLabel()
    {
        gameObject.SetActive(false);
    }

    public void FollowMouse(Vector2 screenPos)
    {
        if (gameObject.activeSelf)
        {
            transform.position = screenPos + new Vector2(30f, 50f);
        }
    }
}
