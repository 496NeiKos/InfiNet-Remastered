using UnityEngine;

[CreateAssetMenu(fileName = "HardwareInfoData", menuName = "InfiNet/Hardware Info Data")]
public class HardwareInfoData : ScriptableObject
{
    public Sprite[] images;
    public string itemName;
    [TextArea(3, 6)] public string description;
}
