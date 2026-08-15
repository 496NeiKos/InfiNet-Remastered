/*
 * ================================================================
 *  UNITY SETUP GUIDE — PingCmdManager
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to the "CMD Panel" GameObject inside the
 *    Virtual OS Canvas. Start the panel INACTIVE.
 *
 *  HIERARCHY (CMD Panel children — duplicate from COC I CMD panel)
 *    CMD Panel  (this script here — start inactive)
 *      ├── Title Bar
 *      │     └── Exit Button  → ExitCmd() (wire in Inspector)
 *      └── ScrollView         → scrollRect
 *            ├── Viewport (Rect Mask 2D)
 *            │     └── Content  → contentContainer
 *            │           (VerticalLayoutGroup, Control Child Size both,
 *            │            Force Expand Width, ContentSizeFitter Vertical=Preferred)
 *            └── Scrollbar Vertical
 *
 *  PREFABS (reuse from COC I)
 *    PromptLinePrefab  → promptLinePrefab
 *      Root: HorizontalLayoutGroup + ContentSizeFitter (Vertical: Preferred)
 *        ├── PrefixLabel  (TMP_Text, Flexible Width=0)
 *        └── InputField   (TMP_InputField, Flexible Width=1, transparent BG,
 *                          disabled color = normal color so locked rows look identical)
 *    TextLinePrefab    → textLinePrefab
 *      Root: TMP_Text + ContentSizeFitter (Vertical: Preferred)
 *            + LayoutElement (Flexible Width=1)
 *
 *  INSPECTOR ASSIGNMENTS
 *    promptLinePrefab  → PromptLinePrefab asset
 *    textLinePrefab    → TextLinePrefab asset
 *    contentContainer  → Content RectTransform (child of Viewport)
 *    scrollRect        → ScrollRect on ScrollView
 *    resultDelay       → 0.3  (seconds before output appears)
 *    tpLinkTabManager  → TPLinkTabManager on Page 2 - Main
 *    dLinkAPManager    → DLinkAPManager on DLink AP Page
 *    routerReset       → RouterResetController on the Router reset button
 *    apReset           → AccessPointResetController on the AP reset button
 *
 *  ENTRY POINT
 *    CmdBtn on Windows Desktop → VirtualOSManager auto-wires to Toggle()
 *    Exit / X button on CMD Panel → wire to PingCmdManager.ExitCmd() in Inspector
 *
 *  COMMANDS
 *    ipconfig          → shows adapter name, IPv4, subnet, gateway for current device.
 *                        Shows APIPA (169.254.x.x / 255.255.0.0) if not static-configured.
 *    ping [IP]         → free-form; validates format (0–255 each octet), resolves to one
 *                        of 5 known devices, checks conditions, returns success or timeout.
 *    ping              → shows abbreviated usage
 *    anything else     → "'X' is not recognized..." error
 *
 *  PING SUCCESS CONDITIONS
 *    Source (current device):
 *      Computer1/2 → UseStaticIP = true AND all IP octets filled
 *      Laptop      → WifiConnected = true AND UseStaticIP = true AND all IP octets filled
 *    Target (typed IP resolves to a device):
 *      Router      → RouterResetController.IsConfigured
 *      AP          → AccessPointResetController.IsConfigured
 *      Computer1/2 → device state IPConfigured
 *      Laptop      → IPConfigured AND WifiConnected
 *    Subnet check  → source and target share the same /24 prefix (first 3 octets match)
 *    Failure in any condition → "Request timed out." × 4 + stats + hint message
 *    Unknown IP (no device match) → "Request timed out." × 4 + stats, no hint
 *
 *  PROGRESS (PingResults[5] in DeviceOSState)
 *    Persists even when CMD is closed or the student switches devices.
 *    [0]=Router  [1]=AP  [2]=Computer1  [3]=Computer2  [4]=Laptop
 *
 *  STATE BEHAVIOUR
 *    ESC (VirtualOS hidden)    → CMD stays as-is; canvas just hides. No state change.
 *    Device switch             → CloseForSwitch() clears visual; CmdOutputLines stays in state.
 *    CMD X button              → ExitCmd() clears visual AND CmdOutputLines. PingResults kept.
 *    Reopen CMD (same device)  → Toggle() calls RestoreVisualFromState() which re-spawns
 *                                 history lines from CmdOutputLines, then a fresh prompt.
 * ================================================================
 */

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PingCmdManager : MonoBehaviour
{
    // PingResults indices — must match DeviceOSState.PingResults[5] order
    private const int IdxRouter    = 0;
    private const int IdxAP        = 1;
    private const int IdxComputer1 = 2;
    private const int IdxComputer2 = 3;
    private const int IdxLaptop    = 4;

    private const string CmdPrefix = @"C:\Windows\system32>";

    // Non-breaking space: keeps ContentSizeFitter at full line height for "blank" rows
    private const string BlankLine = " ";

    // Fixed per-device APIPA addresses shown when static IP is not configured
    private const string ApipaComputer1 = "169.254.201.5";
    private const string ApipaComputer2 = "169.254.180.21";
    private const string ApipaLaptop    = "169.254.154.9";

    // ----------------------------------------------------------------
    //  Inspector fields
    // ----------------------------------------------------------------

    [Header("Prefabs (reuse from COC I)")]
    [SerializeField] private GameObject     promptLinePrefab;
    [SerializeField] private GameObject     textLinePrefab;

    [Header("Scroll")]
    [SerializeField] private RectTransform  contentContainer;
    [SerializeField] private ScrollRect     scrollRect;
    [SerializeField] private float          resultDelay = 0.3f;

    [Header("Device References")]
    [SerializeField] private TPLinkTabManager           tpLinkTabManager;
    [SerializeField] private DLinkAPManager             dLinkAPManager;
    [SerializeField] private RouterResetController      routerReset;
    [SerializeField] private AccessPointResetController apReset;

    // ----------------------------------------------------------------
    //  Runtime state
    // ----------------------------------------------------------------

    private Coroutine _activeRoutine;
    private bool      _processingCommand;

    // ----------------------------------------------------------------
    //  Public API
    // ----------------------------------------------------------------

    // Wired by VirtualOSManager.Start() via CmdBtn
    public void Toggle()
    {
        bool willOpen = !gameObject.activeSelf;
        if (willOpen) RestoreVisualFromState();
        gameObject.SetActive(willOpen);
        Debug.Log($"[PingCmdManager] CMD {(willOpen ? "opened" : "closed")}.");
    }

    // Called by VirtualOSManager on device switch — preserves CmdOutputLines in state
    public void CloseForSwitch()
    {
        ClearVisual();
        gameObject.SetActive(false);
        Debug.Log("[PingCmdManager] CMD closed for device switch — history preserved.");
    }

    // Wired to CMD's X / Exit button in Inspector — closes and clears history
    public void ExitCmd()
    {
        ClearVisual();
        DeviceOSState state = VirtualOSManager.Instance?.CurrentState;
        if (state != null) state.CmdOutputLines.Clear();
        gameObject.SetActive(false);
        Debug.Log("[PingCmdManager] CMD exited — history cleared.");
    }

    // ----------------------------------------------------------------
    //  Visual restore
    // ----------------------------------------------------------------

    private void RestoreVisualFromState()
    {
        ClearVisual();

        DeviceOSState state = VirtualOSManager.Instance?.CurrentState;
        if (state != null && state.CmdOutputLines.Count > 0)
        {
            foreach (string line in state.CmdOutputLines)
                SpawnHistoryLine(line);
        }

        SpawnPromptLine();
        ScrollToBottom();
    }

    private void ClearVisual()
    {
        if (_activeRoutine != null)
        {
            StopCoroutine(_activeRoutine);
            _activeRoutine = null;
        }
        _processingCommand = false;

        if (contentContainer == null) return;
        foreach (Transform child in contentContainer)
            Destroy(child.gameObject);
    }

    // ----------------------------------------------------------------
    //  Prompt line — spawns interactive input row
    // ----------------------------------------------------------------

    private void SpawnPromptLine()
    {
        if (promptLinePrefab == null || contentContainer == null) return;

        GameObject obj = Instantiate(promptLinePrefab, contentContainer);

        TMP_Text prefixLabel = obj.transform.Find("PrefixLabel")?.GetComponent<TMP_Text>();
        if (prefixLabel != null) prefixLabel.text = CmdPrefix;

        TMP_InputField inputField = obj.GetComponentInChildren<TMP_InputField>();
        if (inputField == null) return;

        inputField.text         = "";
        inputField.interactable = true;

        StartCoroutine(FocusNextFrame(inputField));

        inputField.onSubmit.AddListener((raw) =>
        {
            if (_processingCommand) return;
            inputField.interactable = false;
            inputField.onSubmit.RemoveAllListeners();

            // Save the completed prompt line to history before output
            AddToHistory(CmdPrefix + raw);

            _activeRoutine = StartCoroutine(ProcessCommand(raw.Trim()));
        });

        ScrollToBottom();
    }

    private IEnumerator FocusNextFrame(TMP_InputField field)
    {
        yield return null;
        if (field != null && field.interactable)
        {
            field.ActivateInputField();
            field.Select();
        }
    }

    // ----------------------------------------------------------------
    //  Line spawning
    // ----------------------------------------------------------------

    // Re-spawns an existing history line — does NOT add to CmdOutputLines (already there)
    private void SpawnHistoryLine(string content)
    {
        if (textLinePrefab == null || contentContainer == null) return;
        GameObject obj = Instantiate(textLinePrefab, contentContainer);
        TMP_Text tmp = obj.GetComponent<TMP_Text>() ?? obj.GetComponentInChildren<TMP_Text>();
        if (tmp != null) tmp.text = content;
    }

    // Spawns a new output line AND saves it to CmdOutputLines
    private void SpawnOutputLine(string content)
    {
        SpawnHistoryLine(content);
        AddToHistory(content);
        ScrollToBottom();
    }

    private void AddToHistory(string line)
    {
        DeviceOSState state = VirtualOSManager.Instance?.CurrentState;
        state?.CmdOutputLines.Add(line);
    }

    // ----------------------------------------------------------------
    //  Command dispatch
    // ----------------------------------------------------------------

    private IEnumerator ProcessCommand(string raw)
    {
        _processingCommand = true;

        yield return new WaitForSeconds(resultDelay);

        // Normalize: lowercase, collapse internal whitespace
        string normalized = System.Text.RegularExpressions.Regex
            .Replace(raw.ToLower().Trim(), @"\s+", " ");

        if (normalized == "ipconfig")
        {
            yield return StartCoroutine(RunIpconfig());
        }
        else if (normalized == "ping")
        {
            // "ping" with no argument — show usage
            SpawnOutputLine("Usage: ping [-t] [-n count] [-l size] target_name");
            yield return new WaitForSeconds(resultDelay);
        }
        else if (normalized.StartsWith("ping "))
        {
            string arg = normalized.Substring(5).Trim();
            yield return StartCoroutine(RunPing(arg));
        }
        else if (!string.IsNullOrWhiteSpace(normalized))
        {
            SpawnOutputLine($"'{raw}' is not recognized as an internal or external command,");
            SpawnOutputLine("operable program or batch file.");
            yield return new WaitForSeconds(resultDelay);
        }
        // Empty input — just re-prompt with no output

        _processingCommand = false;
        SpawnPromptLine();
        ScrollToBottom();
    }

    // ----------------------------------------------------------------
    //  ipconfig
    // ----------------------------------------------------------------

    private IEnumerator RunIpconfig()
    {
        DeviceOSState state  = VirtualOSManager.Instance?.CurrentState;
        DeviceID      device = VirtualOSManager.Instance?.CurrentDevice ?? DeviceID.Computer1;

        bool isLaptop     = device == DeviceID.Laptop;
        bool isConfigured = state != null && state.IPConfigured;

        string adapterLine = isLaptop
            ? "Wireless LAN adapter Wi-Fi:"
            : "Ethernet adapter Local Area Connection:";

        SpawnOutputLine("Windows IP Configuration");
        SpawnOutputLine(BlankLine);
        SpawnOutputLine(adapterLine);
        SpawnOutputLine(BlankLine);
        SpawnOutputLine("   Connection-specific DNS Suffix  . : ");

        if (!isConfigured)
        {
            string apipa = GetApipaAddress(device);
            SpawnOutputLine($"   Autoconfiguration IPv4 Address. . : {apipa}");
            SpawnOutputLine("   Subnet Mask . . . . . . . . . . . : 255.255.0.0");
            SpawnOutputLine("   Default Gateway . . . . . . . . . : ");
        }
        else
        {
            string ip      = JoinOctets(state.IPOctets);
            string subnet  = IsAnyOctetEmpty(state.SubnetOctets)
                             ? "255.255.255.0"
                             : JoinOctets(state.SubnetOctets);
            string gateway = IsAnyOctetEmpty(state.GatewayOctets)
                             ? ""
                             : JoinOctets(state.GatewayOctets);

            SpawnOutputLine($"   IPv4 Address. . . . . . . . . . . : {ip}");
            SpawnOutputLine($"   Subnet Mask . . . . . . . . . . . : {subnet}");
            SpawnOutputLine($"   Default Gateway . . . . . . . . . : {gateway}");
        }

        yield return new WaitForSeconds(resultDelay);
    }

    // ----------------------------------------------------------------
    //  ping
    // ----------------------------------------------------------------

    private IEnumerator RunPing(string arg)
    {
        // Step 1 — validate IP format (each octet 0–255)
        if (!IsValidIPString(arg))
        {
            SpawnOutputLine($"Ping request could not find host {arg}. Please check the name and try again.");
            yield return new WaitForSeconds(resultDelay);
            yield break;
        }

        SpawnOutputLine($"Pinging {arg} with 32 bytes of data:");
        yield return new WaitForSeconds(resultDelay);

        DeviceOSState currentState  = VirtualOSManager.Instance?.CurrentState;
        DeviceID      currentDevice = VirtualOSManager.Instance?.CurrentDevice ?? DeviceID.Computer1;

        // Step 2 — source connectivity
        if (!CheckSourceConnectivity(currentState, currentDevice, out string sourceHint))
        {
            yield return StartCoroutine(SpawnTimeoutBlock(arg, sourceHint));
            yield break;
        }

        // Step 3 — resolve typed IP to a known device index
        if (!TryResolveTarget(arg, out int targetIndex))
        {
            // Unknown IP — no hint (we can't tell what device they intended)
            yield return StartCoroutine(SpawnTimeoutBlock(arg, ""));
            yield break;
        }

        // Step 4 — target conditions + subnet check
        if (!CheckTargetConditions(arg, targetIndex, currentState, currentDevice, out string targetHint))
        {
            yield return StartCoroutine(SpawnTimeoutBlock(arg, targetHint));
            yield break;
        }

        // Step 5 — success
        for (int i = 0; i < 4; i++)
        {
            SpawnOutputLine($"Reply from {arg}: bytes=32 time<1ms TTL=128");
            yield return new WaitForSeconds(resultDelay * 0.5f);
        }
        SpawnOutputLine(BlankLine);
        SpawnOutputLine($"Ping statistics for {arg}:");
        SpawnOutputLine("    Packets: Sent = 4, Received = 4, Lost = 0 (0% loss),");
        SpawnOutputLine("Approximate round trip times in milli-seconds:");
        SpawnOutputLine("    Minimum = 0ms, Maximum = 0ms, Average = 0ms");

        // Mark progress
        if (currentState?.PingResults != null && targetIndex < currentState.PingResults.Length)
            currentState.PingResults[targetIndex] = true;

        Debug.Log($"[PingCmdManager] {currentDevice} pinged target index {targetIndex} ({arg}) — SUCCESS.");
    }

    private IEnumerator SpawnTimeoutBlock(string ip, string hint)
    {
        for (int i = 0; i < 4; i++)
        {
            SpawnOutputLine("Request timed out.");
            yield return new WaitForSeconds(resultDelay * 0.5f);
        }
        SpawnOutputLine(BlankLine);
        SpawnOutputLine($"Ping statistics for {ip}:");
        SpawnOutputLine("    Packets: Sent = 4, Received = 0, Lost = 4 (100% loss),");

        if (!string.IsNullOrEmpty(hint))
        {
            yield return new WaitForSeconds(resultDelay);
            SpawnOutputLine(BlankLine);
            SpawnOutputLine(hint);
        }
    }

    // ----------------------------------------------------------------
    //  Source connectivity check
    // ----------------------------------------------------------------

    private static bool CheckSourceConnectivity(DeviceOSState state, DeviceID device, out string hint)
    {
        hint = "";
        if (state == null) return false;

        if (device == DeviceID.Laptop)
        {
            if (!state.WifiConnected)
            {
                hint = "Hint: Wi-Fi is not connected. Connect to a wireless network first.";
                return false;
            }
            if (!state.IPConfigured)
            {
                hint = "Hint: The Laptop does not have a static IP configured. Set it in Network and Sharing Center.";
                return false;
            }
        }
        else
        {
            if (!state.IPConfigured)
            {
                hint = "Hint: This device does not have a valid static IP configured. Set it in Network and Sharing Center.";
                return false;
            }
        }

        return true;
    }

    // ----------------------------------------------------------------
    //  Target IP resolution
    // ----------------------------------------------------------------

    private bool TryResolveTarget(string ip, out int index)
    {
        index = -1;

        string routerIP = tpLinkTabManager?.GetRouterLanIP() ?? "";
        if (!string.IsNullOrEmpty(routerIP) && ip == routerIP)
        { index = IdxRouter; return true; }

        string apIP = dLinkAPManager?.GetApIPAddress() ?? "";
        if (!string.IsNullOrEmpty(apIP) && ip == apIP)
        { index = IdxAP; return true; }

        string pc1IP = GetDeviceIPString(DeviceID.Computer1);
        if (!string.IsNullOrEmpty(pc1IP) && ip == pc1IP)
        { index = IdxComputer1; return true; }

        string pc2IP = GetDeviceIPString(DeviceID.Computer2);
        if (!string.IsNullOrEmpty(pc2IP) && ip == pc2IP)
        { index = IdxComputer2; return true; }

        string laptopIP = GetDeviceIPString(DeviceID.Laptop);
        if (!string.IsNullOrEmpty(laptopIP) && ip == laptopIP)
        { index = IdxLaptop; return true; }

        return false;
    }

    // ----------------------------------------------------------------
    //  Target conditions + subnet check
    // ----------------------------------------------------------------

    private bool CheckTargetConditions(string typedIP, int targetIndex,
        DeviceOSState sourceState, DeviceID sourceDevice, out string hint)
    {
        hint = "";

        switch (targetIndex)
        {
            case IdxRouter:
                if (routerReset == null || !routerReset.IsConfigured)
                {
                    hint = "Hint: The Router is not configured. Complete the TP-Link LAN setup first.";
                    return false;
                }
                break;

            case IdxAP:
                if (apReset == null || !apReset.IsConfigured)
                {
                    hint = "Hint: The Access Point is not configured. Complete the D-Link setup with a Static IP first.";
                    return false;
                }
                break;

            case IdxComputer1:
            {
                DeviceOSState s = VirtualOSManager.Instance?.GetState(DeviceID.Computer1);
                if (s == null || !s.IPConfigured)
                {
                    hint = "Hint: Computer 1 does not have a valid static IP configured.";
                    return false;
                }
                break;
            }

            case IdxComputer2:
            {
                DeviceOSState s = VirtualOSManager.Instance?.GetState(DeviceID.Computer2);
                if (s == null || !s.IPConfigured)
                {
                    hint = "Hint: Computer 2 does not have a valid static IP configured.";
                    return false;
                }
                break;
            }

            case IdxLaptop:
            {
                DeviceOSState s = VirtualOSManager.Instance?.GetState(DeviceID.Laptop);
                if (s == null || !s.IPConfigured)
                {
                    hint = "Hint: The Laptop does not have a valid static IP configured.";
                    return false;
                }
                if (!s.WifiConnected)
                {
                    hint = "Hint: The Laptop is not connected to Wi-Fi and cannot be reached on the network.";
                    return false;
                }
                break;
            }
        }

        // Subnet check — compare /24 prefix of source IP vs typed IP
        string sourceIP = GetDeviceIPString(sourceDevice);
        if (!string.IsNullOrEmpty(sourceIP))
        {
            string sourcePrefix = GetSubnetPrefix(sourceIP);
            string targetPrefix = GetSubnetPrefix(typedIP);

            if (!string.IsNullOrEmpty(sourcePrefix)
             && !string.IsNullOrEmpty(targetPrefix)
             && sourcePrefix != targetPrefix)
            {
                hint = $"Hint: The target is on a different subnet ({targetPrefix}.x vs {sourcePrefix}.x). " +
                       "All devices must use the same network address.";
                return false;
            }
        }

        return true;
    }

    // ----------------------------------------------------------------
    //  Helpers
    // ----------------------------------------------------------------

    // Validates that every octet is a number in range 0–255
    private static bool IsValidIPString(string ip)
    {
        if (string.IsNullOrEmpty(ip)) return false;
        string[] parts = ip.Split('.');
        if (parts.Length != 4) return false;
        foreach (string part in parts)
            if (!int.TryParse(part, out int val) || val < 0 || val > 255) return false;
        return true;
    }

    // Returns true if all octets are non-empty (does not range-check — use IsValidIPString for that)
    private static bool IsAnyOctetEmpty(string[] octets)
    {
        if (octets == null) return true;
        foreach (string o in octets)
            if (string.IsNullOrEmpty(o)) return true;
        return false;
    }

    private static string JoinOctets(string[] octets) => string.Join(".", octets);

    // First 3 octets as "A.B.C" — used for /24 subnet comparison
    private static string GetSubnetPrefix(string ip)
    {
        string[] parts = ip.Split('.');
        if (parts.Length != 4) return "";
        return $"{parts[0]}.{parts[1]}.{parts[2]}";
    }

    // Returns the joined IP string for a device only if UseStaticIP = true and all octets filled
    private static string GetDeviceIPString(DeviceID device)
    {
        DeviceOSState state = VirtualOSManager.Instance?.GetState(device);
        if (state == null || !state.UseStaticIP || IsAnyOctetEmpty(state.IPOctets)) return "";
        return JoinOctets(state.IPOctets);
    }

    private static string GetApipaAddress(DeviceID device)
    {
        switch (device)
        {
            case DeviceID.Computer1: return ApipaComputer1;
            case DeviceID.Computer2: return ApipaComputer2;
            case DeviceID.Laptop:    return ApipaLaptop;
            default:                 return "169.254.0.1";
        }
    }

    private void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }

    // ----------------------------------------------------------------
    //  Public read (for future task list integration)
    // ----------------------------------------------------------------

    public bool[] GetPingResults(DeviceID device)
        => VirtualOSManager.Instance?.GetState(device)?.PingResults;

    public bool HasPingedAll(DeviceID device)
    {
        bool[] results = GetPingResults(device);
        if (results == null) return false;
        foreach (bool b in results)
            if (!b) return false;
        return true;
    }
}
