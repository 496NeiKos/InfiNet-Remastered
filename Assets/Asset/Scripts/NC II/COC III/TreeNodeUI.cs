using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Placed on the root GO of treeNodePrefab.
/// Exposes sub-elements so spawning code can configure each row
/// without fragile path-based Find() calls.
/// </summary>
public class TreeNodeUI : MonoBehaviour
{
    [SerializeField] public Image         background;
    [SerializeField] public LayoutElement indentSpacer;
    [SerializeField] public TMP_Text      arrowLabel;
    [SerializeField] public TMP_Text      nodeLabel;
}
