using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadHandler : MonoBehaviour
{
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // After scene loads, store initial positions for all DragItems
        var dragItems = FindObjectsByType<DragItem>(FindObjectsSortMode.None);
        foreach (var item in dragItems)
        {
            item.StoreInitialPosition();
        }
        Debug.Log($"Stored initial positions for {dragItems.Length} DragItems after scene load");
    }
}