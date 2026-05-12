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

        _prefabOriginalParent = interaction.transform.parent;
        _prefabOriginalWorldPos = interaction.transform.position;

        interaction.transform.SetParent(editingPanel.transform, true);

        _activeInteraction.ShowDetailCentered();

        if (editingPanel != null)
            editingPanel.SetActive(true);
        else
            Debug.LogError("GameManager: editingPanel is not assigned in the Inspector!");
    }

    /// <summary>
    /// Called by the Close button.
    /// If inner panel is open, close inner panel first.
    /// If inner panel is closed, close the main editing panel.
    /// </summary>
    public void CloseEditor()
    {
        // Check if there's an inner panel open — close that first
        if (_activeInteraction != null)
        {
            DetailViewManager dvm = _activeInteraction.GetComponent<DetailViewManager>();
            if (dvm != null && dvm.IsInnerPanelOpen)
            {
                dvm.CloseInnerPanel();
                return; // Don't close main panel yet
            }
        }

        // Close the main editing panel
        IsEditorOpen = false;

        if (_activeInteraction != null)
        {
            _activeInteraction.transform.SetParent(_prefabOriginalParent, true);
            _activeInteraction.transform.position = _prefabOriginalWorldPos;

            _activeInteraction.OnEditorClosed();
            _activeInteraction = null;
        }

        if (editingPanel != null)
            editingPanel.SetActive(false);
    }
}