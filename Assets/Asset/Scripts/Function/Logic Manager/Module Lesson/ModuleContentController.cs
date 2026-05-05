using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ModuleContentController : MonoBehaviour
{
    [Header("UI References")]
    public Image moduleImage;          // Assign the Image in Content
    public TMP_Text descriptionText;   // Assign the TMP_Text in Content

    [Header("Content Data")]
    public Sprite[] moduleImages;      // Array of images for each button
    [TextArea(3, 10)] public string[] descriptions; // Array of descriptions

    public void ShowContent(int index)
    {
        if (index >= 0 && index < descriptions.Length)
        {
            // Update text
            descriptionText.text = descriptions[index];

            // Update image
            if (index < moduleImages.Length && moduleImages[index] != null)
            {
                moduleImage.sprite = moduleImages[index];
                moduleImage.gameObject.SetActive(true);
            }
            else
            {
                moduleImage.gameObject.SetActive(false); // hide if no image
            }

            Debug.Log($"[ModuleContentController] Showing content for button {index}");

            // Reset scroll to top
            ScrollRect scrollRect = descriptionText.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }
    }
}
