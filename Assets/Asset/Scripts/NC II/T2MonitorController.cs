/*
 * ================================================================
 *  UNITY SETUP GUIDE — T2MonitorController
 * ================================================================
 *  STEP 1 — Create the T2 Monitor root GameObject
 *    - Place it inside Topic 2's worldRootContainer.
 *    - Add components: SpriteRenderer (monitor workspace sprite),
 *      Collider2D, PrefabInteraction, T2MonitorController,
 *      T2MonitorNavigator.
 *    - Do NOT add DragPrefab — monitor is not draggable.
 *    - This world-space object is what the player right-clicks.
 *
 *  STEP 2 — Create the detail view child as a Canvas
 *    a) Add a child GameObject named "T2MonitorDetailed".
 *
 *    b) Add a Canvas component to T2MonitorDetailed:
 *         Render Mode:    Screen Space - Camera
 *         Render Camera:  Main Camera
 *         Plane Distance: 100
 *       The Canvas auto-fills the camera view — do NOT resize its
 *       RectTransform manually.
 *
 *    c) On the CanvasScaler component (auto-added):
 *         UI Scale Mode:  Constant Pixel Size
 *         Scale Factor:   1
 *
 *    d) GraphicRaycaster is auto-added — leave it as-is.
 *
 *    e) Start with T2MonitorDetailed INACTIVE (untick in inspector).
 *       T2MonitorController will activate it on right-click.
 *
 *  STEP 3 — Add monitor art and panels inside T2MonitorDetailed
 *    Everything inside is now a UI element (RectTransform):
 *    - Add a UI Image for the monitor bezel/frame art (background).
 *    - Add screen panels as children (see T2MonitorNavigator guide).
 *
 *  STEP 4 — Wire the inspector
 *    T2MonitorController:
 *      monitorDetailRoot → T2MonitorDetailed
 *      navigator         → T2MonitorNavigator (on the T2 Monitor root)
 * ================================================================
 */

using UnityEngine;

public class T2MonitorController : MonoBehaviour, IHardwareController
{
    [Header("References")]
    [SerializeField] private GameObject monitorDetailRoot;
    [SerializeField] private T2MonitorNavigator navigator;

    private void Start()
    {
        if (navigator == null)
            navigator = GetComponent<T2MonitorNavigator>();

        monitorDetailRoot?.SetActive(false);
    }

    public void ShowDetailAtCenter()
    {
        monitorDetailRoot?.SetActive(true);
        navigator?.ResetToDefault();
    }

    public void HideDetail()
    {
        monitorDetailRoot?.SetActive(false);
    }
}
