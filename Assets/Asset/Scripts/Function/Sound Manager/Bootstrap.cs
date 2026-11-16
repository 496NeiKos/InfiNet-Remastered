using UnityEngine;
using UnityEngine.InputSystem;

public class Bootstrap : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject musicManagerPrefab;

    private void Awake()
    {
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;

        if (MusicManager.Instance == null)
        {
            GameObject mm = Instantiate(musicManagerPrefab);
            DontDestroyOnLoad(mm);
            Debug.Log("Bootstrap spawned MusicManager.");
        }
    }
}
