/*
 * ================================================================
 *  UNITY SETUP GUIDE — CommandPromptManager
 * ================================================================
 *  PURPOSE
 *    Simulates a Windows CMD session that walks the student through
 *    creating a bootable USB flash drive using DiskPart. The UI
 *    mimics a real terminal: each submitted command is locked in
 *    history, output appears below it, and a new input line spawns
 *    at the bottom inside a scroll view.
 *
 * ----------------------------------------------------------------
 *  PREFAB 1 — PromptLinePrefab
 * ----------------------------------------------------------------
 *    Root GameObject  "PromptLine"
 *      ├─ RectTransform
 *      ├─ HorizontalLayoutGroup
 *      │     Child Alignment : Upper Left
 *      │     Control Child Size Width : true   Height : true
 *      │     Child Force Expand Width : false  Height : false
 *      └─ ContentSizeFitter  (Vertical: Preferred Size)
 *
 *    Children (in order):
 *      ┌─ "PrefixLabel"
 *      │     TMP_Text
 *      │     Font: monospaced recommended (e.g. Liberation Mono)
 *      │     Overflow: Overflow / Alignment: Left-Middle
 *      │     Layout Element: Flexible Width = 0  (does NOT stretch)
 *      │     Enable "Auto Size" OFF — fixed font size
 *      │
 *      └─ "InputField"  (TMP_InputField)
 *            ContentType: Standard
 *            Line Type: Single Line
 *            Font: same monospaced font as PrefixLabel
 *            Layout Element: Flexible Width = 1  (fills remainder)
 *            Caret Color: white (or lime green for classic look)
 *            Selection Color: semi-transparent white
 *
 *    IMPORTANT — InputField visual when disabled:
 *      In the InputField's Colors block set Disabled Color the same
 *      as Normal Color so locked history lines look identical to
 *      active ones. Set Transition to None if you prefer.
 *
 *    IMPORTANT — InputField background:
 *      Set the InputField's Image component Color to fully transparent
 *      (alpha = 0) so the terminal background shows through.
 *
 * ----------------------------------------------------------------
 *  PREFAB 2 — TextLinePrefab
 * ----------------------------------------------------------------
 *    Root GameObject  "TextLine"
 *      ├─ RectTransform
 *      ├─ TMP_Text
 *      │     Font: same monospaced font
 *      │     Text Wrapping: Enabled
 *      │     Overflow: Overflow
 *      │     Alignment: Left-Top
 *      │     Color: white (or light grey for output lines)
 *      └─ ContentSizeFitter  (Vertical: Preferred Size)
 *         LayoutElement: Flexible Width = 1
 *
 * ----------------------------------------------------------------
 *  SCROLL VIEW SETUP
 * ----------------------------------------------------------------
 *    Use a standard UI ScrollView. Configure:
 *      ScrollRect: Vertical = true, Horizontal = false
 *      Viewport  : Mask component (Rect Mask 2D is fine)
 *      Content   : this is the ContentContainer
 *
 *    ContentContainer (the "Content" child of the Viewport):
 *      ├─ VerticalLayoutGroup
 *      │     Child Alignment   : Upper Left
 *      │     Control Child Size Width : true   Height : true
 *      │     Child Force Expand Width : true   Height : false
 *      │     Spacing: 2
 *      └─ ContentSizeFitter   (Vertical: Preferred Size)
 *
 *    The CommandPrompt panel itself should have a dark background
 *    (e.g. black or near-black Image component) to sell the look.
 *
 * ----------------------------------------------------------------
 *  COMPONENT PLACEMENT
 * ----------------------------------------------------------------
 *    Add CommandPromptManager to the root of the CommandPrompt
 *    panel GameObject.
 *
 * ----------------------------------------------------------------
 *  INSPECTOR WIRING
 * ----------------------------------------------------------------
 *    promptLinePrefab   → PromptLinePrefab asset (see above)
 *    textLinePrefab     → TextLinePrefab asset (see above)
 *    contentContainer   → The "Content" RectTransform inside the
 *                         Viewport of the ScrollRect
 *    scrollRect         → The ScrollRect component on the ScrollView
 *    resultDelay        → Seconds pause before output appears (0.2s
 *                         recommended for a natural feel)
 *
 * ----------------------------------------------------------------
 *  TASK CONDITION (read by T2TaskListManager when integrated)
 * ----------------------------------------------------------------
 *    IsCmdSequenceComplete — true once all DiskPart steps finish
 *
 * ----------------------------------------------------------------
 *  COMMAND SEQUENCE (hardcoded — edit result strings here)
 * ----------------------------------------------------------------
 *    The simulation walks through:
 *      1. diskpart          (opens DiskPart, prefix changes)
 *      2. list disk         (shows disk table)
 *      3. select disk 1     (selects the USB)
 *      4. clean             (wipes the USB)
 *      5. create partition primary
 *      6. select partition 1
 *      7. active            (marks partition bootable)
 *      8. format fs=ntfs quick
 *      9. assign            (gives drive letter)
 *     10. exit              (returns to CMD, prefix changes back)
 *     11. xcopy e:\*.* f:\ /s /e /f  (copies Windows files)
 *
 *  Wrong input at any step shows an authentic CMD/DiskPart error
 *  and re-spawns an input line at the same step.
 * ================================================================
 */

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CommandPromptManager : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("HorizontalLayoutGroup with a PrefixLabel (TMP_Text) child named 'PrefixLabel' and a TMP_InputField child named 'InputField'.")]
    [SerializeField] private GameObject promptLinePrefab;
    [Tooltip("Single TMP_Text with ContentSizeFitter (Vertical: Preferred Size).")]
    [SerializeField] private GameObject textLinePrefab;

    [Header("Scroll View")]
    [Tooltip("The 'Content' RectTransform inside the ScrollRect viewport. Has a VerticalLayoutGroup + ContentSizeFitter.")]
    [SerializeField] private RectTransform contentContainer;
    [Tooltip("The ScrollRect component on the scroll view.")]
    [SerializeField] private ScrollRect scrollRect;

    [Header("Timing")]
    [Tooltip("Seconds between locking the input and displaying the result text.")]
    [SerializeField] [Range(0f, 2f)] private float resultDelay = 0.2f;

    // ----------------------------------------------------------------
    //  Public task condition (read by T2TaskListManager when wired)
    // ----------------------------------------------------------------

    public bool IsCmdSequenceComplete { get; private set; }

    // ----------------------------------------------------------------
    //  Command sequence data
    // ----------------------------------------------------------------

    private readonly struct CommandStep
    {
        public readonly string Expected;   // normalized (lowercase, trimmed)
        public readonly string Result;     // output text shown after the command
        public readonly string NextPrefix; // non-null when the prompt prefix changes

        public CommandStep(string expected, string result, string nextPrefix = null)
        {
            Expected   = expected;
            Result     = result;
            NextPrefix = nextPrefix;
        }
    }

    private static readonly CommandStep[] Steps =
    {
        // Step 0 — launch DiskPart; prefix switches to DISKPART>
        new CommandStep(
            expected:   "diskpart",
            result:     "Microsoft DiskPart version 10.0.19041.964\n" +
                        "Copyright (C) Microsoft Corporation.\n" +
                        "On computer: DESKTOP-NCII",
            nextPrefix: "DISKPART> "
        ),
        // Step 1 — list all disks so the student can identify the USB
        new CommandStep(
            expected: "list disk",
            result:   "  Disk ###  Status         Size     Free     Dyn  Gpt\n" +
                      "  --------  -------------  -------  -------  ---  ---\n" +
                      "  Disk 0    Online          476 GB   449 MB        \n" +
                      "  Disk 1    Online           29 GB      0 B        "
        ),
        // Step 2 — target Disk 1 (the USB flash drive)
        new CommandStep(
            expected: "select disk 1",
            result:   "Disk 1 is now the selected disk."
        ),
        // Step 3 — wipe all existing partitions and data
        new CommandStep(
            expected: "clean",
            result:   "DiskPart succeeded in cleaning the disk."
        ),
        // Step 4 — create a single primary partition
        new CommandStep(
            expected: "create partition primary",
            result:   "DiskPart succeeded in creating the specified partition."
        ),
        // Step 5 — select the partition just created
        new CommandStep(
            expected: "select partition 1",
            result:   "Partition 1 is now the selected partition."
        ),
        // Step 6 — mark partition as active (required for BIOS boot)
        new CommandStep(
            expected: "active",
            result:   "DiskPart marked the current partition as active."
        ),
        // Step 7 — quick-format as NTFS
        new CommandStep(
            expected: "format fs=ntfs quick",
            result:   "  100 percent completed\n\nDiskPart successfully formatted the volume."
        ),
        // Step 8 — assign a drive letter automatically
        new CommandStep(
            expected: "assign",
            result:   "DiskPart successfully assigned the drive letter or mount point."
        ),
        // Step 9 — exit DiskPart; prefix switches back to CMD
        new CommandStep(
            expected:   "exit",
            result:     "Leaving DiskPart...",
            nextPrefix: @"C:\Windows\system32>"
        ),
        // Step 10 — copy all Windows installation files from mounted ISO (E:) to USB (F:)
        new CommandStep(
            expected: @"xcopy e:\*.* f:\ /s /e /f",
            result:   "E:\\autorun.inf -> F:\\autorun.inf\n" +
                      "E:\\bootmgr -> F:\\bootmgr\n" +
                      "E:\\bootmgr.efi -> F:\\bootmgr.efi\n" +
                      "E:\\setup.exe -> F:\\setup.exe\n" +
                      "E:\\sources\\install.wim -> F:\\sources\\install.wim\n" +
                      "E:\\sources\\boot.wim -> F:\\sources\\boot.wim\n" +
                      "E:\\support\\logging\\cbs.log -> F:\\support\\logging\\cbs.log\n" +
                      "E:\\efi\\boot\\bootx64.efi -> F:\\efi\\boot\\bootx64.efi\n" +
                      "E:\\efi\\microsoft\\boot\\bcd -> F:\\efi\\microsoft\\boot\\bcd\n" +
                      "...\n" +
                      "        6437 File(s) copied"
        ),
    };

    // ----------------------------------------------------------------
    //  Runtime state
    // ----------------------------------------------------------------

    private static readonly string InitialPrefix = @"C:\Windows\system32>";

    private int             _currentStep;
    private string          _currentPrefix;
    private TMP_InputField  _activeInput;

    // ----------------------------------------------------------------
    //  Lifecycle
    // ----------------------------------------------------------------

    private void Awake()
    {
        _currentStep   = 0;
        _currentPrefix = InitialPrefix;
    }

    private void Start()
    {
        // Remove any designer-placed placeholder children
        foreach (Transform child in contentContainer)
            Destroy(child.gameObject);

        SpawnPromptLine();
    }

    // ----------------------------------------------------------------
    //  Spawn helpers
    // ----------------------------------------------------------------

    private void SpawnPromptLine()
    {
        if (promptLinePrefab == null || contentContainer == null) return;

        GameObject obj = Instantiate(promptLinePrefab, contentContainer);

        Transform prefixTransform = obj.transform.Find("PrefixLabel");
        if (prefixTransform != null)
        {
            TMP_Text label = prefixTransform.GetComponent<TMP_Text>();
            if (label != null)
                label.text = _currentPrefix;
        }
        else
        {
            Debug.LogWarning("[CommandPromptManager] PromptLinePrefab is missing a child named 'PrefixLabel'.");
        }

        TMP_InputField inputField = obj.GetComponentInChildren<TMP_InputField>(true);
        if (inputField != null)
        {
            inputField.text = string.Empty;
            inputField.interactable = true;
            inputField.onSubmit.AddListener(OnInputSubmitted);
            _activeInput = inputField;
            StartCoroutine(FocusNextFrame(inputField));
        }
        else
        {
            Debug.LogWarning("[CommandPromptManager] PromptLinePrefab is missing a TMP_InputField.");
        }

        ScrollToBottom();
    }

    private void SpawnTextLine(string text)
    {
        if (textLinePrefab == null || contentContainer == null) return;

        GameObject obj = Instantiate(textLinePrefab, contentContainer);
        TMP_Text tmp = obj.GetComponent<TMP_Text>();
        if (tmp == null)
            tmp = obj.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
            tmp.text = text;

        ScrollToBottom();
    }

    // ----------------------------------------------------------------
    //  Input handling
    // ----------------------------------------------------------------

    private void OnInputSubmitted(string input)
    {
        if (_activeInput == null) return;
        if (string.IsNullOrWhiteSpace(input)) return;

        // Lock this line — it becomes permanent history
        _activeInput.interactable = false;
        _activeInput.onSubmit.RemoveAllListeners();
        _activeInput = null;

        string normalized = Normalize(input);
        string expected   = Steps[_currentStep].Expected;

        if (normalized == expected)
            StartCoroutine(HandleCorrectInput(_currentStep));
        else
            StartCoroutine(HandleWrongInput(normalized));
    }

    private IEnumerator HandleCorrectInput(int stepIndex)
    {
        yield return new WaitForSeconds(resultDelay);

        CommandStep step = Steps[stepIndex];

        if (!string.IsNullOrEmpty(step.Result))
            SpawnTextLine(step.Result);

        if (!string.IsNullOrEmpty(step.NextPrefix))
            _currentPrefix = step.NextPrefix;

        _currentStep++;

        if (_currentStep >= Steps.Length)
        {
            IsCmdSequenceComplete = true;
            SpawnTextLine("Bootable USB flash drive created successfully.");
            T2TaskListManager.CheckConditions();
            Debug.Log("[CommandPromptManager] Sequence complete.");
            ScrollToBottom();
            yield break;
        }

        yield return new WaitForSeconds(resultDelay);
        SpawnPromptLine();
    }

    private IEnumerator HandleWrongInput(string badInput)
    {
        yield return new WaitForSeconds(resultDelay);

        bool inDiskpart = _currentPrefix.StartsWith("DISKPART");
        string error = inDiskpart
            ? $"'{badInput}' is not a valid DISKPART command. Enter ? for help."
            : $"'{badInput}' is not recognized as an internal or external command,\noperable program or batch file.";

        SpawnTextLine(error);

        yield return new WaitForSeconds(resultDelay);
        SpawnPromptLine();
    }

    // ----------------------------------------------------------------
    //  Utilities
    // ----------------------------------------------------------------

    // Strips extra whitespace and lowercases so matching is forgiving.
    private static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return string.Join(" ",
            s.Trim().ToLowerInvariant()
             .Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries));
    }

    private IEnumerator FocusNextFrame(TMP_InputField field)
    {
        yield return null; // wait one frame for layout to settle
        if (field != null && field.interactable)
        {
            field.ActivateInputField();
            field.Select();
        }
    }

    private void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }

    // ----------------------------------------------------------------
    //  Exit button on nav panel → OnClick
    // ----------------------------------------------------------------

    public void Close()
    {
        gameObject.SetActive(false);
    }

    // ----------------------------------------------------------------
    //  Public reset — call this if the panel needs to restart from scratch
    // ----------------------------------------------------------------

    public void Reset()
    {
        StopAllCoroutines();

        foreach (Transform child in contentContainer)
            Destroy(child.gameObject);

        _currentStep          = 0;
        _currentPrefix        = InitialPrefix;
        _activeInput          = null;
        IsCmdSequenceComplete = false;

        SpawnPromptLine();
        Debug.Log("[CommandPromptManager] Reset.");
    }
}
