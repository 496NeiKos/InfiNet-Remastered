using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Header("References")]
    public GameObject settingsPanel;

    // Called by Start button
    public void StartLesson()
    {
        AudioClip sfx = SoundManager.instance.startSFX;
        SoundManager.instance.PlaySFX(sfx);
        StartCoroutine(WaitAndLoadScene("LessonSelection", sfx.length));
    }

    private IEnumerator WaitAndLoadScene(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }

    // Called by Settings button
    public void OpenSettings()
    {
        SoundManager.instance.PlaySFX(SoundManager.instance.clickSFX);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        SoundManager.instance.PlaySFX(SoundManager.instance.backSFX);
        settingsPanel.SetActive(false);
    }

    // Called by Quit button
    public void QuitGame()
    {
        AudioClip sfx = SoundManager.instance.quitSFX;
        SoundManager.instance.PlaySFX(sfx);
        StartCoroutine(WaitAndQuit(sfx.length));
    }

    private IEnumerator WaitAndQuit(float delay)
    {
        yield return new WaitForSeconds(delay);
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
