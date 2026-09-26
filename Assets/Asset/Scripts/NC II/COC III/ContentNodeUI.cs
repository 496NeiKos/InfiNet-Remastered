using UnityEngine;

public class ContentNodeUI : MonoBehaviour
{
    public string NodeName;
    public string NodeFullPath;
    public bool   IsInteractive;
    public FolderData Data; // null for drive nodes and predefined folder nodes
}
