using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Placed on the root GO of gpoRowPrefab.
/// Exposes all 8 column labels so spawning code can fill them directly.
/// </summary>
public class GPORowUI : MonoBehaviour
{
    [SerializeField] public Image    background;
    [SerializeField] public TMP_Text col_linkOrder;
    [SerializeField] public TMP_Text col_gpo;
    [SerializeField] public TMP_Text col_enforced;
    [SerializeField] public TMP_Text col_linkEnabled;
    [SerializeField] public TMP_Text col_status;
    [SerializeField] public TMP_Text col_wmiFilter;
    [SerializeField] public TMP_Text col_modified;
    [SerializeField] public TMP_Text col_domain;
}
