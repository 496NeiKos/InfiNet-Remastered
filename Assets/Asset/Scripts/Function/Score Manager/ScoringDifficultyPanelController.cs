/*
 * ================================================================
 *  ScoringDifficultyPanelController
 * ================================================================
 *  PLACEMENT
 *    Add to a panel GameObject in the LessonSelection scene canvas.
 *    Panel starts INACTIVE.
 *
 *  HIERARCHY
 *    ScoringDifficultyPanel  (starts inactive)
 *      ├── NormalButton      → OnClick: OnNormalClicked()
 *      ├── HardButton        → OnClick: OnHardClicked()
 *      └── BackButton        → OnClick: OnBackClicked()
 *
 *  FLOW
 *    LessonOverviewManager.ProceedToLesson()
 *      → calls Open(sceneName, cocIndex)
 *      → player picks Normal or Hard
 *      → if saved score exists → opens RetainProgressPopupController
 *      → else → SetupRun + load scene
 * ================================================================
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class ScoringDifficultyPanelController : MonoBehaviour
{
    public static ScoringDifficultyPanelController Instance { get; private set; }

    [SerializeField] private GameObject panel;

    private string     _pendingScene;
    private int        _pendingCOC;
    private Difficulty _pendingDifficulty;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Open(string sceneName, int cocIndex)
    {
        _pendingScene = sceneName;
        _pendingCOC   = cocIndex;
        panel.SetActive(true);
    }

    public void OnNormalClicked() => SelectDifficulty(Difficulty.Normal);
    public void OnHardClicked()   => SelectDifficulty(Difficulty.Hard);

    private void SelectDifficulty(Difficulty d)
    {
        _pendingDifficulty = d;

        if (COCScoreManager.Instance != null && COCScoreManager.Instance.HasSavedScore(_pendingCOC))
        {
            RetainProgressPopupController.Instance?.Open(_pendingCOC, d, _pendingScene);
        }
        else
        {
            Proceed(overwrite: true);
        }
    }

    public void OnBackClicked()
    {
        panel.SetActive(false);
    }

    public void Proceed(bool overwrite)
    {
        panel.SetActive(false);
        COCScoreManager.Instance?.SetupRun(_pendingCOC, _pendingDifficulty, overwrite);
        StartCoroutine(LoadScene());
    }

    private IEnumerator LoadScene()
    {
        AudioClip sfx = SoundManager.instance?.confirmSFX;
        if (sfx != null)
        {
            SoundManager.instance.PlaySFX(sfx);
            yield return new WaitForSeconds(sfx.length);
        }
        SceneManager.LoadScene(_pendingScene);
    }
}
