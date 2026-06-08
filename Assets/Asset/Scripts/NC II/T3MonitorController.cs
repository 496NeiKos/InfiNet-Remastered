/*
 * ================================================================
 *  UNITY SETUP GUIDE — T3MonitorController
 * ================================================================
 *  STEP 1 — Create the UEFI Monitor root GameObject
 *    - Place it inside Topic 3's worldRootContainer.
 *    - Add components: SpriteRenderer (monitor sprite),
 *      Collider2D, T3MonitorInteraction, T3MonitorController,
 *      UEFINavigator.
 *    - Do NOT add DragPrefab — monitor is not draggable.
 *    - This world-space object is what the player right-clicks.
 *
 *  STEP 2 — Create the UEFI canvas as a child
 *    a) Add a child GameObject named "UEFICanvas".
 *    b) Add a Canvas component:
 *         Render Mode:    Screen Space - Camera
 *         Render Camera:  Main Camera
 *         Plane Distance: 100
 *    c) CanvasScaler:
 *         UI Scale Mode:  Constant Pixel Size
 *         Scale Factor:   1
 *    d) Start with UEFICanvas INACTIVE.
 *
 *  STEP 3 — Wire the inspector
 *    T3MonitorController:
 *      uefiCanvasRoot → UEFICanvas child GameObject
 *      navigator      → UEFINavigator on this same root GameObject
 * ================================================================
 */

using UnityEngine;

public class T3MonitorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject uefiCanvasRoot;
    [SerializeField] private UEFINavigator navigator;

    private void Start()
    {
        if (navigator == null)
            navigator = GetComponent<UEFINavigator>();

        uefiCanvasRoot?.SetActive(false);
    }

    public void ShowDetailAtCenter()
    {
        uefiCanvasRoot?.SetActive(true);
        navigator?.Open();
    }

    public void HideDetail()
    {
        uefiCanvasRoot?.SetActive(false);
    }
}
