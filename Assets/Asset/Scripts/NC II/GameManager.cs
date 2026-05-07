using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Drop Zones")]
    public RectTransform workspaceArea;
    public RectTransform hardwareArea;

    [Header("UI Panels")]
    public GameObject editingPanel;

    public static GameManager Instance { get; private set; }

    /// <summary>
    /// True while the editing panel is open.
    /// All workspace interaction scripts check this before doing anything.
    /// </summary>
    public bool IsEditorOpen { get; private set; } = false;

    private PrefabInteraction _activeInteraction;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Called by PrefabInteraction when the user right-clicks a prefab.
    /// Keeps the workspace snapshot visible, moves the detail group to
    /// screen centre, and shows the editing panel.
    /// </summary>
    public void OpenEditor(PrefabInteraction interaction)
    {
        // Close any previously open editor first (handles rapid switching)
        if (_activeInteraction != null && _activeInteraction != interaction)
            _activeInteraction.OnEditorClosed();

        _activeInteraction = interaction;
        IsEditorOpen = true;

        // Tell the prefab to show its detail view centred on screen
        _activeInteraction.ShowDetailCentered();

        if (editingPanel != null)
            editingPanel.SetActive(true);
        else
            Debug.LogError("GameManager: editingPanel is not assigned in the Inspector!");
    }

    /// <summary>
    /// Called by the Close button in the EditingPanel.
    /// Wire the Close button's OnClick → GameManager → CloseEditor().
    /// </summary>
    public void CloseEditor()
    {
        IsEditorOpen = false;

        if (_activeInteraction != null)
        {
            _activeInteraction.OnEditorClosed();
            _activeInteraction = null;
        }

        if (editingPanel != null)
            editingPanel.SetActive(false);
    }
}