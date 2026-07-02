/*
 * ================================================================
 *  UNITY SETUP GUIDE — SingleTaskDisplay
 * ================================================================
 *  PURPOSE
 *    Contextual single-task HUD overlay. While a detail panel or
 *    monitor canvas is open (GameManager.IsEditorOpen == true),
 *    it shows exactly ONE task — the next incomplete task for the
 *    active topic. Hides at the world root. Click-through by
 *    default; hold Ctrl to drag it anywhere on screen.
 *
 *  KEYBOARD SHORTCUTS
 *    Ctrl (hold)   → panel becomes draggable; release to lock back
 *    Ctrl + H      → toggle hide / show
 *
 *  STEP 1 — Create the overlay canvas
 *    a) New empty GameObject: "SingleTaskDisplayCanvas"
 *    b) Add component: Canvas
 *         Render Mode: Screen Space - Overlay
 *         Sort Order:  100   ← on top of everything
 *    c) Add component: CanvasScaler (Constant Pixel Size or Scale With Screen)
 *    d) Add component: GraphicRaycaster — DISABLE it (untick "Enable")
 *         (drag detection is manual; the raycaster is not needed)
 *    e) Add component: SingleTaskDisplay  ← THIS SCRIPT on the canvas root
 *         (must stay active so Update() runs even when the panel is hidden)
 *
 *  STEP 2 — Create the display panel child
 *    a) Add a child UI Image: "SingleTaskPanel"
 *         Semi-transparent dark background (or your art).
 *         Position / anchor it where you want on screen
 *           e.g. anchor bottom-right, pivot bottom-right.
 *    b) Add component: CanvasGroup on SingleTaskPanel
 *         Interactable:    OFF  (script toggles this while Ctrl is held)
 *         Blocks Raycasts: OFF  (script toggles this while Ctrl is held)
 *    c) Start SingleTaskPanel INACTIVE in the inspector.
 *
 *  STEP 3 — Create the task text child
 *    a) Child TMP - Text (UI): "TaskText"
 *         Anchor: stretch inside SingleTaskPanel (with some padding).
 *
 *  STEP 4 — Wire inspector fields on SingleTaskDisplay
 *    displayPanel  → SingleTaskPanel
 *    taskText      → TaskText
 *    topic1Manager → NCIITaskListManager in the scene
 *    topic2Manager → T2TaskListManager  in the scene
 *    topic3Manager → T3TaskListManager  in the scene
 *
 *  HOW IT WORKS
 *    • Update() polls GameManager.IsEditorOpen each frame (bool check).
 *      On open → Refresh() shows the panel with the next task text.
 *      On close → panel hides.
 *    • OnTasksUpdated events from each task manager fire on every
 *      complete/revert → Refresh() updates the text instantly.
 *    • Hold Ctrl → CanvasGroup flips to interactable + blocksRaycasts,
 *      and left-click-drag on the panel moves it. Position is clamped
 *      inside screen bounds. Release Ctrl → click-through restored.
 *    • Ctrl+H toggles _userHidden.
 *    • When a task completes while the panel is visible, the current
 *      text flashes green for 0.6 s before switching to the next task
 *      (or hiding if all tasks are done).
 * ================================================================
 */

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class SingleTaskDisplay : MonoBehaviour
{
    [Header("Display")]
    [Tooltip("The panel child to show/hide. Must have a CanvasGroup.")]
    [SerializeField] private GameObject displayPanel;
    [Tooltip("TMP_Text inside the panel that shows the current task.")]
    [SerializeField] private TMP_Text taskText;

    [Header("Task Manager References")]
    [SerializeField] private NCIITaskListManager topic1Manager;
    [SerializeField] private T2TaskListManager   topic2Manager;
    [SerializeField] private T3TaskListManager   topic3Manager;

    private CanvasGroup   _canvasGroup;
    private RectTransform _panelRect;
    private RectTransform _canvasRect;

    private Color     _defaultTextColor;
    private Coroutine _flashCoroutine;

    private bool    _wasEditorOpen;
    private bool    _userHidden;          // toggled by Ctrl+H
    private bool    _isDragging;
    private Vector2 _dragStartLocal;      // canvas-local pointer position at drag start
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

        NCIITaskListManager.OnTasksUpdated += Refresh;
        T2TaskListManager.OnTasksUpdated   += Refresh;
        T3TaskListManager.OnTasksUpdated   += Refresh;
    }

    private void OnDestroy()
    {
        NCIITaskListManager.OnTasksUpdated -= Refresh;
        T2TaskListManager.OnTasksUpdated   -= Refresh;
        T3TaskListManager.OnTasksUpdated   -= Refresh;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        bool ctrlHeld = kb.ctrlKey.isPressed;

        // ── Ctrl+H / Ctrl+O ─────────────────────────────────────
        if (ctrlHeld && kb.hKey.wasPressedThisFrame)
        {
            _userHidden = !_userHidden;
            _isDragging = false;
            if (_userHidden)
                displayPanel?.SetActive(false);
            else
                Refresh();
        }

        // ── Pass-through toggle ──────────────────────────────────
        SetPassThrough(!ctrlHeld);
        if (!ctrlHeld) _isDragging = false;

        // ── Drag while Ctrl held ─────────────────────────────────
        if (ctrlHeld && displayPanel != null && displayPanel.activeSelf && _panelRect != null)
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 mousePos = mouse.position.ReadValue();

                if (mouse.leftButton.wasPressedThisFrame)
                {
                    // Only start drag when the pointer is actually over the panel.
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
                            _canvasRect, mousePos, null, out Vector2 currentLocal);
                        Vector2 delta = currentLocal - _dragStartLocal;
                        _panelRect.anchoredPosition = ClampToScreen(_dragStartAnchoredPos + delta);
                    }
                    else
                    {
                        _isDragging = false;
                    }
                }
            }
        }

        // ── IsEditorOpen polling ─────────────────────────────────
        bool isOpen = GameManager.Instance != null && GameManager.Instance.IsEditorOpen;
        if (isOpen == _wasEditorOpen) return;
        _wasEditorOpen = isOpen;

        if (isOpen)
        {
            if (!_userHidden) Refresh();
        }
        else
        {
            _isDragging = false;
            displayPanel?.SetActive(false);
        }
    }

    // Called by Update (editor open/close) and by OnTasksUpdated events (task change).
    private void Refresh()
    {
        if (_userHidden)
        {
            displayPanel?.SetActive(false);
            return;
        }

        if (GameManager.Instance == null || !GameManager.Instance.IsEditorOpen)
        {
            displayPanel?.SetActive(false);
            return;
        }

        string newText = GetActiveTopicNextTask();

        bool panelVisible = displayPanel != null && displayPanel.activeSelf;
        bool textChanged  = taskText != null && taskText.text != newText;

        // Flash the current text green before switching — only when the panel is
        // already visible and the text is actually changing (i.e. a task just completed).
        if (panelVisible && textChanged)
        {
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);

            if (string.IsNullOrEmpty(newText))
                _flashCoroutine = StartCoroutine(FlashThenHide());
            else
                _flashCoroutine = StartCoroutine(FlashThenReplace(newText));

            return;
        }

        // First open or same text — show immediately without animation.
        if (string.IsNullOrEmpty(newText))
        {
            displayPanel?.SetActive(false);
            return;
        }

        if (taskText != null) taskText.text = newText;
        displayPanel?.SetActive(true);
    }

    private IEnumerator FlashThenReplace(string newText)
    {
        if (taskText != null) taskText.color = Color.green;
        yield return new WaitForSeconds(0.6f);
        if (taskText != null)
        {
            taskText.color = _defaultTextColor;
            taskText.text  = newText;
        }
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

    // Queries only the manager whose topic container is currently active.
    private string GetActiveTopicNextTask()
    {
        if (topic1Manager != null && topic1Manager.gameObject.activeInHierarchy)
            return topic1Manager.GetNextIncompleteTaskText();
        if (topic2Manager != null && topic2Manager.gameObject.activeInHierarchy)
            return topic2Manager.GetNextIncompleteTaskText();
        if (topic3Manager != null && topic3Manager.gameObject.activeInHierarchy)
            return topic3Manager.GetNextIncompleteTaskText();
        return null;
    }

    // Flips the panel between click-through (default) and interactable (Ctrl held).
    private void SetPassThrough(bool passThrough)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.interactable   = !passThrough;
        _canvasGroup.blocksRaycasts = !passThrough;
    }

    // Clamps candidatePos so the panel stays fully within screen bounds.
    // Uses GetWorldCorners — correct for Screen Space - Overlay regardless of
    // canvas scale or panel anchor/pivot settings.
    private Vector2 ClampToScreen(Vector2 candidatePos)
    {
        if (_panelRect == null) return candidatePos;

        // Temporarily apply the candidate, sample corners, then restore.
        Vector2 saved = _panelRect.anchoredPosition;
        _panelRect.anchoredPosition = candidatePos;

        Vector3[] corners = new Vector3[4];
        _panelRect.GetWorldCorners(corners);            // [0]=BL [1]=TL [2]=TR [3]=BR

        _panelRect.anchoredPosition = saved;

        float minX = corners[0].x;
        float maxX = corners[2].x;
        float minY = corners[0].y;
        float maxY = corners[1].y;

        Vector2 adjust = Vector2.zero;

        if      (minX < 0)               adjust.x = -minX;
        else if (maxX > Screen.width)    adjust.x = Screen.width  - maxX;

        if      (minY < 0)               adjust.y = -minY;
        else if (maxY > Screen.height)   adjust.y = Screen.height - maxY;

        return candidatePos + adjust;
    }
}
