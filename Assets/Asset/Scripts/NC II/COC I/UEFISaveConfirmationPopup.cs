/*
 * ================================================================
 *  UNITY SETUP GUIDE — UEFISaveConfirmationPopup
 * ================================================================
 *  PURPOSE
 *    Shown when the player presses F10 inside the UEFI panel.
 *    Two buttons: Return (cancel, stay in UEFI) and Save Changes
 *    (commit the boot state and close the UEFI canvas).
 *
 *  STEP 1 — Create the popup GameObject
 *    - Add as a child of UEFIPanel (sibling of Navbar, TabContent).
 *    - Give it the highest sibling index so it renders on top.
 *    - Add component: UEFISaveConfirmationPopup.
 *    - Start it INACTIVE.
 *
 *  STEP 2 — Build the hierarchy
 *    UEFISaveConfirmationPopup   ← this script, starts INACTIVE
 *      ├─ Background             ← Image (dark semi-transparent overlay)
 *      ├─ MessageText            ← TMP_Text  "Save changes and exit UEFI?"
 *      ├─ ReturnButton           → UEFISaveConfirmationPopup.Return()
 *      └─ SaveButton             → UEFISaveConfirmationPopup.SaveChanges()
 *
 *  STEP 3 — Wire the inspector
 *    navigator         → UEFINavigator on the UEFI Monitor root
 *    monitorController → T3MonitorController on the UEFI Monitor root
 * ================================================================
 */

using UnityEngine;

public class UEFISaveConfirmationPopup : MonoBehaviour
{
    [SerializeField] private UEFINavigator navigator;
    [SerializeField] private T3MonitorController monitorController;

    public bool IsVisible => gameObject.activeSelf;

    public void Show() => gameObject.SetActive(true);

    // Wire to the Return button — cancels and stays in the UEFI panel.
    public void Return() => gameObject.SetActive(false);

    // Wire to the Save Changes button — commits the boot state and closes UEFI.
    public void SaveChanges()
    {
        gameObject.SetActive(false);
        navigator?.CommitSave();
        monitorController?.OnBootStateSaved();
    }
}
