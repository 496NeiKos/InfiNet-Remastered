using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Drop Zones")]
    public RectTransform workspaceArea;
    public RectTransform hardwareArea;

    [Header("UI Panels")]
    public GameObject editingPanel;

    public static GameManager Instance { get; private set; }

    public bool IsEditorOpen { get; private set; } = false;

    private PrefabInteraction _activeInteraction;

    private Transform _prefabOriginalParent;
    private Vector3 _prefabOriginalWorldPos;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void OpenEditor(PrefabInteraction interaction)
    {
        if (_activeInteraction != null && _activeInteraction != interaction)
            _activeInteraction.OnEditorClosed();

        _activeInteraction = interaction;
        IsEditorOpen = true;

        // Save original parent and position before reparenting
        _prefabOriginalParent = interaction.transform.parent;
        _prefabOriginalWorldPos = interaction.transform.position;

        // Reparent prefab to editing panel
        interaction.transform.SetParent(editingPanel.transform, true);

        // Show detail centered on the editing panel
        _activeInteraction.ShowDetailCentered();

        if (editingPanel != null)
            editingPanel.SetActive(true);
        else
            Debug.LogError("GameManager: editingPanel is not assigned in the Inspector!");
    }

    public void CloseEditor()
    {
        IsEditorOpen = false;

        if (_activeInteraction != null)
        {
            // Return prefab to its original parent and position
            _activeInteraction.transform.SetParent(_prefabOriginalParent, true);
            _activeInteraction.transform.position = _prefabOriginalWorldPos;

            _activeInteraction.OnEditorClosed();
            _activeInteraction = null;
        }

        if (editingPanel != null)
            editingPanel.SetActive(false);
    }
}