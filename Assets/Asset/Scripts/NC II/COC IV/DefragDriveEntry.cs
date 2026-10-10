[System.Serializable]
public class DefragDriveEntry
{
    public string DriveDisplay;   // e.g. "C: (Windows)"
    public string MediaType;      // "Solid State Drive" or "Hard Disk Drive"
    public string LastRun;        // e.g. "10/7/2026 2:14 AM" or "Never run"
    public string CurrentStatus;  // e.g. "OK (0 days since last run)"
    public bool   IsSSD;
}
