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
 *      │           │     └── OpenBtn      → diskCleanupBtn  (interactable = FALSE — future topic)
 *      │           └── Windows Security Entry → windowsSecurityEntry  (INACTIVE)
 *      │                 ├── AppNameTMP   "Windows Security"
 *      │                 ├── CategoryTMP  "System"
 *      │                 └── OpenBtn      → windowsSecurityBtn  (interactable = FALSE — future topic)
 *      │
 *      ├── Desktop Icons
 *      └── App Panels
 *
 *  INSPECTOR ASSIGNMENTS
 *    searchBarBtn             Button on the Search Bar in Taskbar
 *    searchWindowPanel        The search window panel GameObject
 *    overlay                  Full-screen transparent Button (same setup as Start Menu overlay)
 *    overlayBtn               Button on overlay
 *    searchInput              TMP_InputField inside the panel
 *    restorePointEntry        Parent GO of the "Create a restore point" result row
 *    diskCleanupEntry         Parent GO of the "Disk Cleanup" result row
 *    windowsSecurityEntry     Parent GO of the "Windows Security" result row
 *    restorePointBtn          Button inside restorePointEntry
 *    diskCleanupBtn           Button inside diskCleanupEntry    (leave interactable = false)
 *    windowsSecurityBtn       Button inside windowsSecurityEntry (leave interactable = false)
 *    startMenuController      StartMenuController reference (drag from scene)
 *    systemProtectionController  SystemProtectionController reference (drag from scene)
 *
 *  HOW IT WORKS
 *    Clicking the Search Bar opens the Search Window overlay.
 *    Start Menu is closed first if open.
 *    Typing in searchInput filters the three result entries:
 *      an entry's parent GO is shown when the typed text is a substring
 *      of any of that app's registered keywords (case-insensitive).
 *    Clicking "Create a restore point" closes the search window and
 *    opens SystemProtectionController.
 *    The other two entries are visible when matched but non-interactable
 *    (will be wired in future COC IV topics).
 *    Clicking the overlay (outside the panel) closes the search window.
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
    [SerializeField] private GameObject windowsSecurityEntry;

    [Header("Result Buttons")]
    [SerializeField] private Button restorePointBtn;
    [SerializeField] private Button diskCleanupBtn;
    [SerializeField] private Button windowsSecurityBtn;

    [Header("References")]
    [SerializeField] private StartMenuController         startMenuController;
    [SerializeField] private SystemProtectionController  systemProtectionController;

    // Keywords per app entry (all lowercase)
    private static readonly string[] RestorePointKeywords  = { "create a restore point", "restore point", "restore", "system protection" };
    private static readonly string[] DiskCleanupKeywords   = { "disk cleanup", "disk", "cleanup" };
    private static readonly string[] WindowsSecurityKeywords = { "windows security", "security", "defender", "firewall" };

    private void Awake()
    {
        searchBarBtn?.onClick.AddListener(OpenSearch);
        overlayBtn?.onClick.AddListener(CloseSearch);
        searchInput?.onValueChanged.AddListener(OnSearchChanged);
        restorePointBtn?.onClick.AddListener(OpenRestorePoint);

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
        windowsSecurityEntry?.SetActive(false);
    }

    private void OnSearchChanged(string value)
    {
        string lower = value.ToLowerInvariant().Trim();
        bool empty   = string.IsNullOrEmpty(lower);

        restorePointEntry?.SetActive(!empty && Matches(lower, RestorePointKeywords));
        diskCleanupEntry?.SetActive(!empty && Matches(lower, DiskCleanupKeywords));
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
}
