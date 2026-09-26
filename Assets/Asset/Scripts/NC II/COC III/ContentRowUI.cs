using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Placed on the root GO of contentRowPrefab.
/// One prefab shared across DNS, DHCP, and ADUC content panels.
/// col2TMP and actionBtn are hidden (SetActive false) when not needed.
/// </summary>
public class ContentRowUI : MonoBehaviour
{
    [SerializeField] public TMP_Text col1TMP;
    [SerializeField] public TMP_Text col2TMP;
    [SerializeField] public Button   actionBtn;
    [SerializeField] public TMP_Text actionBtnLabel;
}
