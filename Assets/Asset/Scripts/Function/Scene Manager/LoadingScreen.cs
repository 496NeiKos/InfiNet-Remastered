using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    public float delaySeconds = 1f; // how long to wait before loading main menu

    void Start()
    {
        Debug.Log("Initiating loading...");
        StartCoroutine(LoadMainMenu());
    }

    IEnumerator LoadMainMenu()
    {
        Debug.Log("Loading Complete");
        yield return new WaitForSeconds(delaySeconds);
        SceneManager.LoadScene("MainMenu"); // make sure your main menu scene is named exactly
    }
}