/*
 * ================================================================
 *  UNITY SETUP GUIDE — DashboardPanelController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to the "Dashboard Panel" GameObject, which is a direct
 *    child of "Main Panel" inside "Server Manager Panel".
 *    Starts ACTIVE (it is the default view when Server Manager opens).
 *
 *  HIERARCHY
 *    Dashboard Panel                      ← this script here (ACTIVE)
 *      │
 *      ├── WelcomeGroup
 *      │     ├── GroupHeaderTMP           TMP_Text  "WELCOME TO SERVER MANAGER"
 *      │     └── WelcomeBigPanel          Image (background panel)
 *      │           ├── LeftPanel          Image (left half)
 *      │           │     ├── QuickStartTMP   TMP_Text  "QUICK START"
 *      │           │     ├── WhatsNewTMP     TMP_Text  "WHAT'S NEW"
 *      │           │     └── LearnMoreTMP    TMP_Text  "LEARN MORE"
 *      │           └── RightPanel         Image (right half)
 *      │                 ├── ConfigureLocalServerBtn  → configureLocalServerBtn
 *      │                 │     Child TMP: "Configure this local server"
 *      │                 ├── AddRolesBtn  Button (static, no script reference)
 *      │                 │     Child TMP: "Add roles and features"
 *      │                 ├── AddOtherServersBtn  Button (static)
 *      │                 │     Child TMP: "Add other servers to manage"
 *      │                 ├── CreateServerGroupBtn  Button (static)
 *      │                 │     Child TMP: "Create a server group"
 *      │                 └── ConnectToCloudBtn  Button (static)
 *      │                       Child TMP: "Connect this server to cloud services"
 *      │
 *      └── RolesAndGroupsGroup
 *            ├── GroupHeaderTMP           TMP_Text  "ROLES AND SERVER GROUPS"
 *            ├── SummaryTMP               TMP_Text  → summaryTMP
 *            │     Default text: "Roles: 0 | Server Groups: 0 | Servers Total: 1"
 *            └── RoleTilesContainer       → roleTilesContainer (empty at start, no Image needed)
 *                  └── [RoleTilePrefab instances spawned here at runtime]
 *
 *  ROLE TILE PREFAB  (Asset/Prefab/NC II Prefab/COC III/roleTilePrefab.prefab)
 *    Root: Image (card background), VerticalLayoutGroup or manual layout
 *      ├── RoleNameTMP      TMP_Text  default ""     ← index [0], set at runtime
 *      ├── ManageabilityTMP TMP_Text  default "OK"   ← index [1], static
 *      ├── EventsTMP        TMP_Text  default "0"    ← index [2], static
 *      ├── ServicesTMP      TMP_Text  default "Running" ← index [3], static
 *      ├── PerformanceTMP   TMP_Text  default "—"    ← index [4], static
 *      └── BPAResultTMP     TMP_Text  default "Not run" ← index [5], static
 *    IMPORTANT: All six TMP_Text components must be DIRECT children of the prefab
 *    root so that GetComponentsInChildren<TMP_Text>() returns them in order [0..5].
 *    Do not nest them inside additional child GameObjects.
 *
 *  INSPECTOR ASSIGNMENTS
 *    configureLocalServerBtn → RightPanel/ConfigureLocalServerBtn Button
 *    summaryTMP              → RolesAndGroupsGroup/SummaryTMP
 *    roleTilesContainer      → RolesAndGroupsGroup/RoleTilesContainer (Transform)
 *    roleTilePrefab          → roleTilePrefab prefab asset
 *
 *  HOW IT WORKS
 *    ServerManagerController calls Refresh(installedRoleNames) every time the
 *    Dashboard tree button is clicked or Server Manager is opened.
 *    Refresh rebuilds the role tiles from scratch and updates the summary counter.
 *    ConfigureLocalServerBtn routes through ServerManagerController.ShowLocalServer()
 *    so the tree panel highlight stays in sync.
 * ================================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DashboardPanelController : MonoBehaviour
{
    [Header("Welcome Group")]
    [SerializeField] private Button configureLocalServerBtn;

    [Header("Roles and Server Groups")]
    [SerializeField] private TMP_Text  summaryTMP;
    [SerializeField] private Transform roleTilesContainer;
    [SerializeField] private GameObject roleTilePrefab;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        configureLocalServerBtn?.onClick.AddListener(() =>
            ServerManagerController.Instance?.ShowLocalServer());
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Rebuilds the role tiles and updates the summary counter.
    /// Called by ServerManagerController whenever the dashboard is shown or a role is installed.
    /// </summary>
    public void Refresh(List<string> installedRoleNames)
    {
        RefreshSummary(installedRoleNames.Count);
        RefreshRoleTiles(installedRoleNames);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void RefreshSummary(int roleCount)
    {
        if (summaryTMP != null)
            summaryTMP.text = $"Roles: {roleCount} | Server Groups: 0 | Servers Total: 1";
    }

    private void RefreshRoleTiles(List<string> roleNames)
    {
        if (roleTilesContainer == null || roleTilePrefab == null) return;

        foreach (Transform child in roleTilesContainer)
            Destroy(child.gameObject);

        foreach (var name in roleNames)
        {
            var tile = Instantiate(roleTilePrefab, roleTilesContainer);
            var tmps = tile.GetComponentsInChildren<TMP_Text>();
            // [0] = RoleName (dynamic), [1..5] = static metrics set in prefab Inspector
            if (tmps.Length > 0) tmps[0].text = name;
        }
    }
}
