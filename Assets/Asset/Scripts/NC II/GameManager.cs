using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Drop Zones")]
    public RectTransform workspaceArea;
    public RectTransform hardwareArea;

    // Singleton pattern for easy access
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}
