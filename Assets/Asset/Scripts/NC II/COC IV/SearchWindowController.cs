/*
 * ================================================================
 *  UNITY SETUP GUIDE — SearchWindowController (COC IV)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "Search Window Root" — a new always-active empty
 *    GameObject that is a DIRECT CHILD of Desktop Panel.
 *    Do NOT place under App Panels.
 *
 *  HIERARCHY
 *    Desktop Panel
 *      ├── Taskbar
 *      │     ├── Windows Icon Btn    (wired in StartMenuController)
 *      │     └── Search Bar Btn      → searchBarBtn
 *      │
 *      ├── Start Menu Root           (StartMenuController lives here)
 *      │
 *      ├── Search Window Root        ← this script here (ACTIVE)
 *      │     ├── Search Window Overlay → overlay  (INACTIVE, fullscreen transparent Button)
 *      │     └── Search Window Panel   → searchWindowPanel  (INACTIVE)
 *      │           ├── SearchInput     → searchInput  (TMP_InputField)
 *      │           ├── Create Restore Point Entry → restorePointEntry (INACTIVE)
 *      │           │     ├── AppNameTMP   "Create a restore point"
 *      │           │     ├── CategoryTMP  "Control Panel"
 *      │           │     └── OpenBtn      → restorePointBtn  (interactable = true)
 *      │           ├── Disk Cleanup Entry → diskCleanupEntry  (INACTIVE)
 *      │           │     ├── AppNameTMP   "Disk Cleanup"
 *      │           │     ├── CategoryTMP  "System"
 *      │           │     └── OpenBtn      → diskCleanupBtn   (interactable = TRUE — enable now)
 *      │           ├── Defragment Entry   → defragEntry  (INACTIVE)  ← NEW
 *      │           │     ├── AppNameTMP   "Defragment and Optimize Drives"
 *      │           │     ├── CategoryTMP  "System"
 *      │           │     └── OpenBtn      → defragBtn   (interactable = true)
 *      │           └── Windows Security Entry → windowsSecurityEntry  (INACTIVE)
 *      │                 ├── AppNameTMP   "Windows Security"
 *      │                 ├── CategoryTMP  "System"
 *      │                 └── OpenBtn      → windowsSecurityBtn  (interactable = FALSE — future topic)
 *      │
 *      ├── Desktop Icons
 *      └── App Panels
 *            ├── System Protection Chain  (SystemProtectionController)
 *            ├── Disk Cleanup Chain       (DiskCleanupController)  ← NEW
 *            └── Defrag Chain             (DefragController)        ← NEW
 *
 *  INSPECTOR ASSIGNMENTS
 *    searchBarBtn               Button on the Search Bar in Taskbar
 *    searchWindowPanel          The search window panel GameObject
 *    overlay                    Full-screen transparent Button
 *    overlayBtn                 Button on overlay
 *    searchInput                TMP_InputField inside the panel
 *    restorePointEntry          Parent GO of the "Create a restore point" result row
 *    diskCleanupEntry           Parent GO of the "Disk Cleanup" result row
 *    defragEntry                Parent GO of the "Defragment and Optimize Drives" result row (NEW)
 *    windowsSecurityEntry       Parent GO of the "Windows Security" result row
 *    restorePointBtn            Button inside restorePointEntry
 *    diskCleanupBtn             Button inside diskCleanupEntry    (set interactable = TRUE)
 *    defragBtn                  Button inside defragEntry         (set interactable = true)
 *    windowsSecurityBtn         Button inside windowsSecurityEntry (leave interactable = false)
 *    startMenuController        StartMenuController reference
 *    systemProtectionController SystemProtectionController reference
 *    diskCleanupController      DiskCleanupController reference   (NEW — drag from scene)
 *    defragController           DefragController reference        (NEW — drag from scene)
 *
 *  HOW IT WORKS
 *    Typing filters all entries by substring match against keyword arrays.
 *    Clicking an active entry closes the search window and opens its controller.
 *    Windows Security remains non-interactable (future topic).
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SearchWindowController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject searchWindowPanel;
    [SerializeField] private GameObject overlay;

    [Header("Taskbar Button")]
    [SerializeField] private Button searchBarBtn;
    [SerializeField] private Button overlayBtn;

    [Header("Search Input")]
    [SerializeField] private TMP_InputField searchInput;

    [Header("Result Entries (parent GameObjects)")]
    [SerializeField] private GameObject restorePointEntry;
    [SerializeField] private GameObject diskCleanupEntry;
    [SerializeField] private GameObject defragEntry;
    [SerializeField] private GameObject windowsSecurityEntry;

    [Header("Result Buttons")]
    [SerializeField] private Button restorePointBtn;
    [SerializeField] private Button diskCleanupBtn;
    [SerializeField] private Button defragBtn;
    [SerializeField] private Button windowsSecurityBtn;

    [Header("References")]
    [SerializeField] private StartMenuController         startMenuController;
    [SerializeField] private SystemProtectionController  systemProtectionController;
    [SerializeField] private DiskCleanupController       diskCleanupController;
    [SerializeField] private DefragController            defragController;

    // Keywords per app entry (all lowercase)
    private static readonly string[] RestorePointKeywords    = { "create a restore point", "restore point", "restore", "system protection" };
    private static readonly string[] DiskCleanupKeywords     = { "disk cleanup", "disk", "cleanup" };
    private static readonly string[] DefragKeywords          = { "defragment and optimize drives", "defragment", "defrag", "optimize drives", "optimize" };
    private static readonly string[] WindowsSecurityKeywords = { "windows security", "security", "defender", "firewall" };

    private void Awake()
    {
        searchBarBtn?.onClick.AddListener(OpenSearch);
        overlayBtn?.onClick.AddListener(CloseSearch);
        searchInput?.onValueChanged.AddListener(OnSearchChanged);

        restorePointBtn?.onClick.AddListener(OpenRestorePoint);
        diskCleanupBtn?.onClick.AddListener(OpenDiskCleanup);
        defragBtn?.onClick.AddListener(OpenDefrag);

        searchWindowPanel?.SetActive(false);
        overlay?.SetActive(false);
    }

    public void OpenSearch()
    {
        startMenuController?.Close();
        ResetSearch();
        searchWindowPanel?.SetActive(true);
        overlay?.SetActive(true);
        searchInput?.ActivateInputField();
    }

    public void CloseSearch()
    {
        searchWindowPanel?.SetActive(false);
        overlay?.SetActive(false);
    }

    private void ResetSearch()
    {
        if (searchInput != null) searchInput.text = "";
        restorePointEntry?.SetActive(false);
        diskCleanupEntry?.SetActive(false);
        defragEntry?.SetActive(false);
        windowsSecurityEntry?.SetActive(false);
    }

    private void OnSearchChanged(string value)
    {
        string lower = value.ToLowerInvariant().Trim();
        bool   empty = string.IsNullOrEmpty(lower);

        restorePointEntry?.SetActive(!empty && Matches(lower, RestorePointKeywords));
        diskCleanupEntry?.SetActive(!empty && Matches(lower, DiskCleanupKeywords));
        defragEntry?.SetActive(!empty && Matches(lower, DefragKeywords));
        windowsSecurityEntry?.SetActive(!empty && Matches(lower, WindowsSecurityKeywords));
    }

    private static bool Matches(string input, string[] keywords)
    {
        foreach (string keyword in keywords)
            if (keyword.Contains(input))
                return true;
        return false;
    }

    private void OpenRestorePoint()
    {
        CloseSearch();
        systemProtectionController?.Open();
    }

    private void OpenDiskCleanup()
    {
        CloseSearch();
        diskCleanupController?.Open();
    }

    private void OpenDefrag()
    {
        CloseSearch();
        defragController?.Open();
    }
}
