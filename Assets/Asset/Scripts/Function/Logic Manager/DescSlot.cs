using UnityEngine;
using UnityEngine.UI;

public class DescSlot : MonoBehaviour
{
    [Header("UI Output")]
    public Text terminalText;

    private void Awake()
    {
        if (terminalText != null)
        {
            terminalText.text = "";
            Debug.Log("[DescSlot] Awake: terminalText cleared.");
        }
        else
        {
            Debug.LogWarning("[DescSlot] Awake: terminalText not assigned!");
        }
    }

    /// Show description immediately when an item is dragged
    public void ShowDescription(DragItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("[DescSlot] ShowDescription called with null item.");
            return;
        }
        if (terminalText == null)
        {
            Debug.LogWarning("[DescSlot] ShowDescription called but terminalText not assigned.");
            return;
        }

        ComponentItem compItem = item.GetComponent<ComponentItem>();
        if (compItem != null)
        {
            terminalText.color = Color.white;
            terminalText.text = compItem.GetDescription();
            Debug.Log($"[DescSlot] Showing description for {item.name} → {compItem.GetDescription()}");
        }
        else
        {
            Debug.LogWarning($"[DescSlot] {item.name} has no ComponentItem script attached.");
        }
    }

    /// Clear description when drag stops
    public void ClearDescription()
    {
        
    }
}
