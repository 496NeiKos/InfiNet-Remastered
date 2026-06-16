/*
 * ================================================================
 *  UNITY SETUP GUIDE — T2SystemUnitController
 * ================================================================
 *  STEP 1 — Create the T2 System Unit GameObject
 *    - Place it inside Topic 2's worldRootContainer (assigned in TopicManager).
 *    - Add components: SpriteRenderer, Collider2D, PrefabInteraction,
 *      T2SystemUnitController.
 *    - Do NOT add DragPrefab — this object is not draggable.
 *
 *  STEP 2 — Create the front view child
 *    - Add a child GameObject named "T2SystemUnitFront".
 *    - This is the panel shown when the player right-clicks the system unit.
 *    - Add whatever art/sprites represent the system unit front face.
 *    - Inside it, create a child "USBPort":
 *        Components: SpriteRenderer (port art), Collider2D (drop zone), CablePort
 *        CablePort settings:
 *          acceptedCableTypes: ["USB"]
 *          startEmpty: true (tick this)
 *
 *  STEP 3 — Wire the inspector
 *    T2SystemUnitController:
 *      frontView → T2SystemUnitFront
 *
 *  STEP 4 — Flash drive HardwareHolder (in Topic 2's hardwareAreaContainer)
 *    - Create a HardwareHolder UI element in Topic 2's hardware area.
 *    - Its hardwarePrefab is the Flash Drive GameObject which needs:
 *        Components: SpriteRenderer, Collider2D, CableBehavior
 *        CableBehavior settings:
 *          cableType: "USB"
 *          hardwareHolder: reference back to this HardwareHolder
 *          homePort: leave blank (auto-detected on install)
 * ================================================================
 */

using UnityEngine;

public class T2SystemUnitController : MonoBehaviour, IHardwareController
{
    [Header("Views")]
    [SerializeField] private GameObject frontView;

    [Header("References")]
    [SerializeField] private T2MonitorController monitorController;

    private void Start()
    {
        frontView?.SetActive(false);
    }

    public void ShowDetailAtCenter()
    {
        var layer = GameManager.Instance?.firstLayer;
        if (layer != null)
        {
            RectTransform rect = layer.GetComponent<RectTransform>();
            if (rect != null)
            {
                Vector3 panelCenter = rect.TransformPoint(
                    new Vector3(rect.rect.center.x, rect.rect.center.y, 0f));
                panelCenter.z = 0f;
                transform.position = panelCenter;

                if (frontView != null)
                    frontView.transform.position = panelCenter;
            }
        }

        frontView?.SetActive(true);
        monitorController?.PushBehind();
    }

    public void HideDetail()
    {
        frontView?.SetActive(false);
        monitorController?.RestoreLayer();
    }
}
