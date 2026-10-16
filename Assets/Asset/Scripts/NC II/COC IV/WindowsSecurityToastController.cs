/*
 * ================================================================
 *  UNITY SETUP GUIDE — WindowsSecurityToastController (COC IV)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to the "Toast Panel" GO.
 *    "Toast Panel" MUST be the LAST CHILD of "Windows Security Chain"
 *    so it renders on top of the main app panel.
 *    Starts INACTIVE in the scene.
 *
 *  ── HIERARCHY ───────────────────────────────────────────────────
 *
 *    Toast Panel                          ← this script (starts INACTIVE)
 *      ├── Background Image               (decorative — e.g. Windows Security shield icon)
 *      ├── TitleTMP                       (static: "Windows Security")
 *      ├── BodyTMP                        (static — see body text below)
 *      ├── DismissBtn                     → dismissBtn  label: "Dismiss"
 *      └── ExitBtn                        → exitBtn     label: "X"
 *
 *  ── BODY TMP TEXT ───────────────────────────────────────────────
 *    "Microsoft Defender scan result.
 *     Microsoft Defender Antivirus scanned your device.
 *     No new threats were found."
 *
 *  ── RECT TRANSFORM SETUP ────────────────────────────────────────
 *    Anchor Min/Max: (1, 0)  — bottom-right corner anchor
 *    Pivot:         (1, 0)  — right-bottom pivot
 *    Anchored Position: set via Inspector fields onScreenX / onScreenY
 *      Recommended: onScreenX = -16, onScreenY = 80
 *      (16 px inset from right edge, 80 px above bottom — clears the taskbar)
 *    Width / Height: set in the editor to your desired toast size
 *
 *  ── POSITIONING NOTES ───────────────────────────────────────────
 *    The slide animation moves anchoredPosition.x from off-screen
 *    (toastWidth + 50 to the right of the right anchor) to onScreenX.
 *    IMPORTANT: ensure no ancestor has a RectMask2D or Mask component,
 *    or the toast will be clipped before it enters view.
 *    "Windows Security Chain" should cover the full canvas area.
 *
 *  ── TIMING ──────────────────────────────────────────────────────
 *    slideInDuration  default 0.4s — SerializeField, adjust in Inspector.
 *
 *  ── INSPECTOR ASSIGNMENTS ───────────────────────────────────────
 *    dismissBtn        "Dismiss" button
 *    exitBtn           "X" / exit button (same behavior as Dismiss)
 *    onScreenX         Target anchoredPosition.x  (default -16)
 *    onScreenY         Target anchoredPosition.y  (default 80)
 *    slideInDuration   Slide animation duration   (default 0.4)
 * ================================================================
 */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WindowsSecurityToastController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button dismissBtn;
    [SerializeField] private Button exitBtn;

    [Header("Animation")]
    [SerializeField] private float slideInDuration = 0.4f;
    [SerializeField] private float onScreenX       = -16f;
    [SerializeField] private float onScreenY       = 80f;

    private RectTransform _rect;
    private Coroutine     _slideCoroutine;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        dismissBtn?.onClick.AddListener(Dismiss);
        exitBtn?.onClick.AddListener(Dismiss);
    }

    public void Show()
    {
        if (_slideCoroutine != null)
        {
            StopCoroutine(_slideCoroutine);
            _slideCoroutine = null;
        }
        gameObject.SetActive(true);
        _slideCoroutine = StartCoroutine(SlideIn());
    }

    public void Hide()
    {
        if (_slideCoroutine != null)
        {
            StopCoroutine(_slideCoroutine);
            _slideCoroutine = null;
        }
        gameObject.SetActive(false);
    }

    private void Dismiss()
    {
        WindowsSecurityController.Instance?.MarkToastDismissed();
        Hide();
    }

    private IEnumerator SlideIn()
    {
        // Place off-screen immediately (same frame as SetActive) to prevent flash.
        if (_rect != null)
            _rect.anchoredPosition = new Vector2(1000f, onScreenY);

        yield return null; // let Unity finalise layout so rect.width is accurate

        float offScreenX = (_rect != null ? _rect.rect.width : 400f) + 50f;
        if (_rect != null)
            _rect.anchoredPosition = new Vector2(offScreenX, onScreenY);

        float elapsed = 0f;
        while (elapsed < slideInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / slideInDuration));
            if (_rect != null)
                _rect.anchoredPosition = new Vector2(Mathf.Lerp(offScreenX, onScreenX, t), onScreenY);
            yield return null;
        }

        if (_rect != null)
            _rect.anchoredPosition = new Vector2(onScreenX, onScreenY);

        _slideCoroutine = null;
    }
}
