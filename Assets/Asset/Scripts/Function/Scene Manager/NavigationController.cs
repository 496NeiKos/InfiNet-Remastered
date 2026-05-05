using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class NavigationController : MonoBehaviour
{
    //Menus

    private IEnumerator WaitAndLoadScene(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }
    public void ReturnToMainMenu()
    {
        AudioClip sfx = SoundManager.instance.backSFX;
        SoundManager.instance.PlaySFX(sfx);
        StartCoroutine(WaitAndLoadScene("MainMenu", sfx.length));
    }
    public void ReturnToLessonSelection()
    {
        AudioClip sfx = SoundManager.instance.backSFX;
        SoundManager.instance.PlaySFX(sfx);
        StartCoroutine(WaitAndLoadScene("LessonSelection", sfx.length));
    }

    public void ReturnToHardware()
    {
        AudioClip sfx = SoundManager.instance.backSFX;
        SoundManager.instance.PlaySFX(sfx);
        StartCoroutine(WaitAndLoadScene("Hardware", sfx.length));
    }

    //Module
    public void ReturnToLessonBook()
    {
        AudioClip sfx = SoundManager.instance.backSFX;
        SoundManager.instance.PlaySFX(sfx);
        StartCoroutine(WaitAndLoadScene("LessonBook", sfx.length));
    }
    public void ReturnToUserManual()
    {
        AudioClip sfx = SoundManager.instance.backSFX;
        SoundManager.instance.PlaySFX(sfx);
        StartCoroutine(WaitAndLoadScene("UserManual", sfx.length));
    }

    //Lesson Book Scenes
    public void ProceedToLessonBook1()
    {
        AudioClip sfx = SoundManager.instance.clickSFX;
        SoundManager.instance.PlaySFX(sfx);
        StartCoroutine(WaitAndLoadScene("Introduction", sfx.length));
    }
    public void ProceedToLessonBook2()
    {
        AudioClip sfx = SoundManager.instance.clickSFX;
        SoundManager.instance.PlaySFX(sfx);
        StartCoroutine(WaitAndLoadScene("HandTools", sfx.length));
    }
    public void ProceedToLessonBook3()
    {
        AudioClip sfx = SoundManager.instance.clickSFX;
        SoundManager.instance.PlaySFX(sfx);
        StartCoroutine(WaitAndLoadScene("OSInstallation", sfx.length));
    }
    public void ProceedToLessonBook4()
    {
        AudioClip sfx = SoundManager.instance.clickSFX;
        SoundManager.instance.PlaySFX(sfx);
        StartCoroutine(WaitAndLoadScene("PatchPanel", sfx.length));
    }

    //Usern Manual Scenes
    public void ProceedToUserManual1()
    {
        AudioClip sfx = SoundManager.instance.clickSFX;
        SoundManager.instance.PlaySFX(sfx);
        StartCoroutine(WaitAndLoadScene("umHardware", sfx.length));
    }
    public void ProceedToUserManual2()
    {
        AudioClip sfx = SoundManager.instance.clickSFX;
        SoundManager.instance.PlaySFX(sfx);
        StartCoroutine(WaitAndLoadScene("umSoftware", sfx.length));
    }
    public void ProceedToUserManual3()
    {
        AudioClip sfx = SoundManager.instance.clickSFX;
        SoundManager.instance.PlaySFX(sfx);
        StartCoroutine(WaitAndLoadScene("umNetworking", sfx.length));
    }
}