using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LessonSelectionController : MonoBehaviour
{
    private IEnumerator WaitAndLoadScene(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }
    public void LoadLessonBook()
    {
        AudioClip sfx = SoundManager.instance.clickSFX;
        SoundManager.instance.PlaySFX(sfx);
        StartCoroutine(WaitAndLoadScene("LessonBook", sfx.length));
    }

    public void LoadUserManual()
    {
        AudioClip sfx = SoundManager.instance.clickSFX;
        SoundManager.instance.PlaySFX(sfx);
        StartCoroutine(WaitAndLoadScene("UserManual", sfx.length));
    }
    public void LoadLesson1()
    {
        AudioClip sfx = SoundManager.instance.clickSFX;
        SoundManager.instance.PlaySFX(sfx);
        StartCoroutine(WaitAndLoadScene("Hardware", sfx.length));
    }

    public void LoadLesson2()
    {
        AudioClip sfx = SoundManager.instance.clickSFX;
        SoundManager.instance.PlaySFX(sfx);
        StartCoroutine(WaitAndLoadScene("Software", sfx.length));
    }

    public void LoadLesson3()
    {
        AudioClip sfx = SoundManager.instance.clickSFX;
        SoundManager.instance.PlaySFX(sfx);
        StartCoroutine(WaitAndLoadScene("Networking", sfx.length));
    }

    
}
