using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Drop Zones")]
    public RectTransform workspaceArea;
    public RectTransform hardwareArea;

    [Header("UI Panels")]
    public GameObject editingPanel;

    public static GameManager Instance { get; private set; }

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
    /// Stores which prefab is active and shows the editing panel.
    /// </summary>
    public void OpenEditor(PrefabInteraction interaction)
    {
        _activeInteraction = interaction;

        if (editingPanel != null)
            editingPanel.SetActive(true);
        else
            Debug.LogError("GameManager: editingPanel is not assigned in the Inspector!");
    }

    /// <summary>
    /// Called by the Close button in the EditingPanel.
    /// Wire the Close button's OnClick event to this method via the GameManager scene object.
    /// </summary>
    public void CloseEditor()
    {
        if (_activeInteraction != null)
        {
            _activeInteraction.OnEditorClosed();
            _activeInteraction = null;
        }

        if (editingPanel != null)
            editingPanel.SetActive(false);
    }
}