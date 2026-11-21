/*
 * ================================================================
 *  RetainProgressPopupController
 * ================================================================
 *  PLACEMENT
 *    Add to a popup panel in the LessonSelection scene canvas.
 *    Panel starts INACTIVE. Opens after difficulty is chosen IF
 *    a saved score already exists for the selected COC.
 *
 *  HIERARCHY
 *    RetainProgressPopup  (starts inactive)
 *      ├── DescriptionText   TMP_Text → (optional, auto-set)
 *      ├── OverwriteButton   → OnClick: OnOverwriteClicked()
 *      └── RetainButton      → OnClick: OnRetainClicked()
 * ================================================================
 */

using TMPro;
using UnityEngine;

public class RetainProgressPopupController : MonoBehaviour
{
    public static RetainProgressPopupController Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text   descriptionText;

    private int        _pendingCOC;
    private Difficulty _pendingDifficulty;
    private string     _pendingScene;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Open(int cocIndex, Difficulty difficulty, string sceneName)
    {
        _pendingCOC        = cocIndex;
        _pendingDifficulty = difficulty;
        _pendingScene      = sceneName;

        if (descriptionText != null)
            descriptionText.text =
                $"You already have a saved score for COC {cocIndex + 1}.\n" +
                "Do you want to overwrite it with this new run, or keep your existing score?";

        panel.SetActive(true);
    }

    public void OnOverwriteClicked()
    {
        panel.SetActive(false);
        ScoringDifficultyPanelController.Instance?.Proceed(overwrite: true);
    }

    public void OnRetainClicked()
    {
        panel.SetActive(false);
        ScoringDifficultyPanelController.Instance?.Proceed(overwrite: false);
    }
}
