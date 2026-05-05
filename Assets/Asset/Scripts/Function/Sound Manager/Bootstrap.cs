using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject musicManagerPrefab;

    private void Awake()
    {
        // ✅ Spawn MusicManager only once
        if (MusicManager.Instance == null)
        {
            GameObject mm = Instantiate(musicManagerPrefab);
            DontDestroyOnLoad(mm);
            Debug.Log("Bootstrap spawned MusicManager.");
        }
    }
}
