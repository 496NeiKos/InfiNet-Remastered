using UnityEngine;

// Replaced by HardwareInfoPanel + HardwareInfoData.
// This stub keeps the scene from showing a missing-script error on the old tooltip GameObject.
// Delete the HardwareAreaTooltip GameObject from the scene in the Inspector, then delete this file.
public class HardwareAreaTooltip : MonoBehaviour
{
    public static HardwareAreaTooltip Instance { get; private set; }
    private void Awake() { Instance = this; gameObject.SetActive(false); }
    public void Show(string text) { }
    public void Hide() { }
}
