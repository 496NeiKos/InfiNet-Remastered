using System.Collections.Generic;

[System.Serializable]
public class FolderData
{
    public string Name;
    public string ParentPath;
    public string FullPath;
    public string DateCreated;
    public bool IsShared;
    public string ShareName;
    public string Comments;
    public List<FolderPermissionEntry> Permissions = new List<FolderPermissionEntry>();
}

[System.Serializable]
public class FolderPermissionEntry
{
    public string GroupOrUser = "Everyone";
    public bool FullControlAllow;
    public bool FullControlDeny;
    public bool ChangeAllow;
    public bool ChangeDeny;
    public bool ReadAllow;
    public bool ReadDeny;
}

// SharedFolderData is defined in ServerDeviceState.cs
