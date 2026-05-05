using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LoadingController : MonoBehaviour
{
    [Header("Loading UI")]
    public Slider loadingSlider;       // Slider UI element
    public Text loadingText;           // Text element for percentage

    [Header("Settings")]
    public float loadingDuration = 3f; // Time in seconds for the slider to fill
    public string nextSceneName = "MainMenu"; // Scene to load after timer

    void Start()
    {
        // Start loading automatically when this scene begins
        StartCoroutine(LoadNextScene());
    }

    private IEnumerator LoadNextScene()
    {
        float elapsed = 0f;
        while (elapsed < loadingDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / loadingDuration);

            if (loadingSlider != null)
                loadingSlider.value = progress;

            if (loadingText != null)
                loadingText.text = Mathf.RoundToInt(progress * 100f) + "%";

            yield return null;
        }

        // ✅ Load the next scene after timer completes
        SceneManager.LoadScene(nextSceneName);
    }
}
