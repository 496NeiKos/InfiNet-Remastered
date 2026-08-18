using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the row prefab used by NetworkCablePopupManager.
/// Wire all child references in the Inspector.
///
/// The row handles three states depending on Phase2 installation:
///
///   State A — neither side blocked:
///     MoveEndButton visible, RemoveCableButton visible.
///
///   State B — only the OTHER device's Phase2 is installed (this port is free to move):
///     MoveEndButton visible, RemoveCableButton hidden, RemoveBlockedNote visible.
///
///   State C — THIS device's Phase2 is installed (nothing can be done here):
///     MoveEndButton hidden, RemoveCableButton hidden, FullyBlockedGroup visible.
///
/// Suggested prefab hierarchy:
///   Row root        (Image bg, LayoutElement preferred height ~72)
///     DeviceNameLabel   (TextMeshProUGUI)
///     ButtonsRow        (HorizontalLayoutGroup)
///       MoveEndButton   (Button + TMP child)
///       RemoveCableButton (Button + TMP child)
///     RemoveBlockedNote (TextMeshProUGUI — State B message, e.g. "Disconnect: unplug the other device's port cable first")
///     FullyBlockedGroup (GameObject — State C message, e.g. a TMP "Remove your port cable first")
/// </summary>
public class CablePopupRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI deviceNameLabel;
    [SerializeField] private Button          moveEndButton;
    [SerializeField] private Button          removeCableButton;

    [Tooltip("Shown in State B: Move End is available but Remove Cable is not. " +
             "E.g. a small TMP label: 'Unplug the other device\\'s port cable to fully disconnect.'")]
    [SerializeField] private GameObject removeBlockedNote;

    [Tooltip("Shown in State C: this device's own port cable is installed — nothing can be done. " +
             "E.g. a TMP label: 'Remove your port cable first.'")]
    [SerializeField] private GameObject fullyBlockedGroup;

    // Exposed so NetworkCablePopupManager can add onClick listeners after Setup().
    public Button MoveEndButton     => moveEndButton;
    public Button RemoveCableButton => removeCableButton;

    /// <summary>
    /// Fills the row and switches to the correct visual state.
    /// Call immediately after Instantiate, before adding onClick listeners.
    /// </summary>
    /// <param name="otherDeviceName">Display name of the device on the other end of the cable.</param>
    /// <param name="moveBlocked">True if THIS device's Phase2 is installed — blocks Move End.</param>
    /// <param name="removeBlocked">True if EITHER device's Phase2 is installed — blocks Remove Cable.</param>
    public void Setup(string otherDeviceName, bool moveBlocked, bool removeBlocked)
    {
        if (deviceNameLabel != null)
            deviceNameLabel.text = $"↔  {otherDeviceName}";

        // State C: this port's own Phase2 installed — fully blocked.
        bool fullyBlocked = moveBlocked;

        // State B: only the other port's Phase2 installed — move allowed, remove blocked.
        bool partiallyBlocked = !moveBlocked && removeBlocked;

        if (moveEndButton     != null) moveEndButton.gameObject.SetActive(!moveBlocked);
        if (removeCableButton != null) removeCableButton.gameObject.SetActive(!removeBlocked);
        if (removeBlockedNote != null) removeBlockedNote.SetActive(partiallyBlocked);
        if (fullyBlockedGroup != null) fullyBlockedGroup.SetActive(fullyBlocked);
    }
}
