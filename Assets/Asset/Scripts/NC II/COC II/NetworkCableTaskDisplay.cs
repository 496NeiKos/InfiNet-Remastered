/*
 * COC II equivalent of SingleTaskDisplay.
 * Mimics the same behavior (show next task while detail panel is open,
 * flash on completion, Ctrl+H to hide, Ctrl-drag to reposition) but
 * references only NetworkCableTaskManager — no COC I scripts touched.
 *
 * UNITY SETUP
 *   1. Create an empty GameObject: "NetworkCableTaskDisplayCanvas"
 *   2. Add component: Canvas  (Render Mode: Screen Space - Overlay, Sort Order: 100)
 *   3. Add component: CanvasScaler
 *   4. Add component: GraphicRaycaster — UNTICK "Enabled"
 *   5. Add THIS script to that root GameObject
 *
 *   6. Child Image: "SingleTaskPanel"
 *        Semi-transparent dark background, anchor bottom-right (or wherever you like)
 *        Add component: CanvasGroup  (Interactable: OFF, BlocksRaycasts: OFF)
 *        Start INACTIVE in the Inspector
 *
 *   7. Child TMP text inside SingleTaskPanel: "TaskText"
 *        Stretch-anchored inside the panel with padding
 *
 *   8. Wire the inspector fields on NetworkCableTaskDisplay:
 *        displayPanel      → SingleTaskPanel
 *        taskText          → TaskText
 *        cableTaskManager  → NetworkCableTaskManager in the scene
 */

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkCableTaskDisplay : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private GameObject displayPanel;
    [SerializeField] private TMP_Text   taskText;

    [Header("Task Manager")]
    [SerializeField] private NetworkCableTaskManager cableTaskManager;

    private CanvasGroup   _canvasGroup;
    private RectTransform _panelRect;
    private RectTransform _canvasRect;

    private Color     _defaultTextColor;
    private Coroutine _flashCoroutine;

    private bool    _wasEditorOpen;
    private bool    _userHidden;
    private bool    _isDragging;
    private Vector2 _dragStartLocal;
    private Vector2 _dragStartAnchoredPos;

    private void Start()
    {
        _canvasRect = GetComponent<RectTransform>();

        if (displayPanel != null)
        {
            _panelRect   = displayPanel.GetComponent<RectTransform>();
            _canvasGroup = displayPanel.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = displayPanel.AddComponent<CanvasGroup>();

            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;
            displayPanel.SetActive(false);
        }

        _defaultTextColor = taskText != null ? taskText.color : Color.white;

        NetworkCableTaskManager.OnTasksUpdated += Refresh;
    }

    private void OnDestroy()
    {
        NetworkCableTaskManager.OnTasksUpdated -= Refresh;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        bool ctrlHeld = kb.ctrlKey.isPressed;

        if (ctrlHeld && kb.hKey.wasPressedThisFrame)
        {
            _userHidden = !_userHidden;
            _isDragging = false;
            if (_userHidden) displayPanel?.SetActive(false);
            else             Refresh();
        }

        SetPassThrough(!ctrlHeld);
        if (!ctrlHeld) _isDragging = false;

        if (ctrlHeld && displayPanel != null && displayPanel.activeSelf && _panelRect != null)
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 mousePos = mouse.position.ReadValue();
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(_panelRect, mousePos, null))
                    {
                        _isDragging = true;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            _canvasRect, mousePos, null, out _dragStartLocal);
                        _dragStartAnchoredPos = _panelRect.anchoredPosition;
                    }
                }
                if (_isDragging)
                {
                    if (mouse.leftButton.isPressed)
                    {
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            _canvasRect, mousePos, null, out Vector2 current);
                        _panelRect.anchoredPosition = ClampToScreen(_dragStartAnchoredPos + (current - _dragStartLocal));
                    }
                    else _isDragging = false;
                }
            }
        }

        bool isOpen = GameManager.Instance != null && GameManager.Instance.IsEditorOpen;
        if (isOpen == _wasEditorOpen) return;
        _wasEditorOpen = isOpen;

        if (isOpen) { if (!_userHidden) Refresh(); }
        else        { _isDragging = false; displayPanel?.SetActive(false); }
    }

    private void Refresh()
    {
        if (_userHidden) { displayPanel?.SetActive(false); return; }
        if (GameManager.Instance == null || !GameManager.Instance.IsEditorOpen)
        { displayPanel?.SetActive(false); return; }

        string newText      = cableTaskManager != null ? cableTaskManager.GetNextIncompleteTaskText() : null;
        Color  displayColor = cableTaskManager != null
            ? cableTaskManager.GetDisplayColor(_defaultTextColor)
            : _defaultTextColor;

        bool panelVisible = displayPanel != null && displayPanel.activeSelf;
        bool textChanged  = taskText != null && taskText.text != newText;

        if (panelVisible && textChanged)
        {
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = string.IsNullOrEmpty(newText)
                ? StartCoroutine(FlashThenHide())
                : StartCoroutine(FlashThenReplace(newText, displayColor));
            return;
        }

        if (string.IsNullOrEmpty(newText)) { displayPanel?.SetActive(false); return; }

        if (taskText != null) { taskText.text = newText; taskText.color = displayColor; }
        displayPanel?.SetActive(true);
    }

    private IEnumerator FlashThenReplace(string newText, Color restoreColor)
    {
        if (taskText != null) taskText.color = Color.green;
        yield return new WaitForSeconds(0.6f);
        if (taskText != null) { taskText.color = restoreColor; taskText.text = newText; }
        _flashCoroutine = null;
    }

    private IEnumerator FlashThenHide()
    {
        if (taskText != null) taskText.color = Color.green;
        yield return new WaitForSeconds(0.6f);
        if (taskText != null) taskText.color = _defaultTextColor;
        displayPanel?.SetActive(false);
        _flashCoroutine = null;
    }

    private void SetPassThrough(bool passThrough)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.interactable   = !passThrough;
        _canvasGroup.blocksRaycasts = !passThrough;
    }

    private Vector2 ClampToScreen(Vector2 candidatePos)
    {
        if (_panelRect == null) return candidatePos;
        Vector2 saved = _panelRect.anchoredPosition;
        _panelRect.anchoredPosition = candidatePos;
        Vector3[] corners = new Vector3[4];
        _panelRect.GetWorldCorners(corners);
        _panelRect.anchoredPosition = saved;

        float minX = corners[0].x, maxX = corners[2].x;
        float minY = corners[0].y, maxY = corners[1].y;
        Vector2 adjust = Vector2.zero;
        if      (minX < 0)             adjust.x = -minX;
        else if (maxX > Screen.width)  adjust.x = Screen.width  - maxX;
        if      (minY < 0)             adjust.y = -minY;
        else if (maxY > Screen.height) adjust.y = Screen.height - maxY;
        return candidatePos + adjust;
    }
}
