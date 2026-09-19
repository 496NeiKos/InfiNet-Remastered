using TMPro;
using UnityEngine;

/// <summary>
/// Assigned to linkRowPrefab. Holds the four TMP columns for the
/// GPO Scope → Links table in GroupPolicyController.
/// </summary>
public class LinkRowUI : MonoBehaviour
{
    public TMP_Text col_location;
    public TMP_Text col_enforced;
    public TMP_Text col_linkEnabled;
    public TMP_Text col_path;
}
