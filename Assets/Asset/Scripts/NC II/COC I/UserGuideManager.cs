/*
 * ================================================================
 *  UNITY SETUP GUIDE — UserGuideManager
 * ================================================================
 *  STEP 1 — Create the Canvas
 *    a) New GameObject: "UserGuideCanvas"
 *    b) Add Canvas component:
 *         Render Mode:  Screen Space - Overlay
 *         Sort Order:   200   ← above everything
 *    c) Add CanvasScaler (Constant Pixel Size, Scale Factor 1)
 *    d) Add GraphicRaycaster
 *    e) Attach UserGuideManager to this GameObject
 *
 *  STEP 2 — "?" Toggle Button (always active, lives OUTSIDE the panel)
 *    a) Child UI Button: "GuideToggleButton"
 *    b) Label text: "?"
 *    c) Wire Button.OnClick → UserGuideManager.ToggleGuide()
 *       (or leave blank — the script wires it in Start if you assign
 *        toggleButton in the Inspector)
 *
 *  STEP 3 — Guide Panel (starts INACTIVE)
 *    Child of the Canvas root:
 *
 *    GuidePanel  (Image background, starts inactive)
 *      ├─ LeftNav  (Panel)
 *      │    ├─ SearchBar  (TMP_InputField)  ← assign to searchField
 *      │    └─ NavScroll  (ScrollRect)
 *      │         └─ NavContent  (Vertical Layout Group) ← assign to navButtonContainer
 *      │
 *      ├─ ImagePanel  (Panel)
 *      │    ├─ ImageDisplay  (UI Image)     ← assign to imageDisplay
 *      │    ├─ PrevButton    (Button)       ← assign to prevImageButton
 *      │    ├─ NextButton    (Button)       ← assign to nextImageButton
 *      │    ├─ PageLabel     (TMP_Text)     ← assign to imagePageLabel   ("1 / 3")
 *      │    └─ NoImageLabel  (TMP_Text)     ← assign to noImageLabel     (shown when no images)
 *      │
 *      └─ DescriptionPanel  (Panel)
 *           ├─ EntryTitleText  (TMP_Text)   ← assign to entryTitleText
 *           └─ DescriptionText (TMP_Text)   ← assign to descriptionText
 *
 *  STEP 4 — Nav Buttons (pre-placed in scene — no prefab needed)
 *    Inside NavContent, manually create one UI Button child per guide entry.
 *    Each button needs a TMP_Text child for its label.
 *    The script reads them in order (sibling index) and wires them to the
 *    matching entry at the same position in the Guide Data list.
 *    Button count and entry count must match.
 *
 *  STEP 5 — Fill Guide Data
 *    On the UserGuideManager Inspector, expand the "Guide Data" list.
 *    Add one entry per feature:
 *      • Title       — short name shown in the nav list and above the description
 *      • Description — full explanation text (supports \n line breaks)
 *      • Images      — 0–N sprites shown in the carousel; leave empty for text-only entries
 *
 *  STEP 6 — Visual Tuning
 *    • navNormalColor   — default nav button background tint
 *    • navSelectedColor — tint applied to the active nav button
 *    • F1 key toggles the panel (in addition to the ? button)
 * ================================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UserGuideManager : MonoBehaviour
{
    // ----------------------------------------------------------------
    //  Data model
    // ----------------------------------------------------------------

    [System.Serializable]
    public class GuideEntry
    {
        public string title = "New Entry";
        [TextArea(3, 10)]
        public string description = string.Empty;
        public Sprite[] images = new Sprite[0];
    }

    // ----------------------------------------------------------------
    //  Inspector fields
    // ----------------------------------------------------------------

    [Header("Guide Data")]
    [SerializeField] private List<GuideEntry> entries = new();

    [Header("Toggle")]
    [Tooltip("The '?' button that opens/closes the guide. Its OnClick can call ToggleGuide() or leave blank and assign it here.")]
    [SerializeField] private Button toggleButton;
    [Tooltip("Close/back button inside the guide panel. Calls CloseGuide().")]
    [SerializeField] private Button closeButton;
    [Tooltip("The root panel that is shown/hidden when the guide opens. Starts inactive.")]
    [SerializeField] private GameObject guidePanel;

    [Header("Navigation")]
    [Tooltip("Parent transform (Vertical Layout Group) that contains the pre-placed nav buttons.")]
    [SerializeField] private Transform navButtonContainer;
    [SerializeField] private Color navNormalColor   = new Color(0.18f, 0.18f, 0.18f, 1f);
    [SerializeField] private Color navSelectedColor = new Color(0.25f, 0.55f, 1f,    1f);

    [Header("Search")]
    [Tooltip("TMP_InputField above the nav list. Filters entries by title and description.")]
    [SerializeField] private TMP_InputField searchField;

    [Header("Image Panel")]
    [SerializeField] private Image    imageDisplay;
    [SerializeField] private Button   prevImageButton;
    [SerializeField] private Button   nextImageButton;
    [Tooltip("Shows '1 / 3' style page indicator. Hidden when entry has 0 or 1 image.")]
    [SerializeField] private TMP_Text imagePageLabel;
    [Tooltip("Shown when the selected entry has no images.")]
    [SerializeField] private TMP_Text noImageLabel;

    [Header("Description Panel")]
    [SerializeField] private TMP_Text entryTitleText;
    [SerializeField] private TMP_Text descriptionText;

    // ----------------------------------------------------------------
    //  Private state
    // ----------------------------------------------------------------

    private readonly List<Button> _navButtons = new();
    private int  _selectedIndex    = 0;
    private int  _lastOpenIndex    = 0;
    private int  _currentImageIdx  = 0;
    private bool _isOpen           = false;

    // ----------------------------------------------------------------
    //  Unity lifecycle
    // ----------------------------------------------------------------

    private void Start()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleGuide);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseGuide);

        if (prevImageButton != null)
            prevImageButton.onClick.AddListener(PrevImage);

        if (nextImageButton != null)
            nextImageButton.onClick.AddListener(NextImage);

        if (searchField != null)
            searchField.onValueChanged.AddListener(OnSearchChanged);

        BuildNavButtons();

        if (guidePanel != null)
            guidePanel.SetActive(false);
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.f1Key.wasPressedThisFrame)
            ToggleGuide();
    }

    // ----------------------------------------------------------------
    //  Toggle
    // ----------------------------------------------------------------

    public void ToggleGuide()
    {
        if (_isOpen) CloseGuide();
        else         OpenGuide();
    }

    public void OpenGuide()
    {
        _isOpen = true;
        if (guidePanel != null)
            guidePanel.SetActive(true);

        // Clear search so the full list is visible on open.
        if (searchField != null)
            searchField.SetTextWithoutNotify(string.Empty);

        ShowAllNavButtons();
        SelectEntry(_lastOpenIndex);
    }

    public void CloseGuide()
    {
        _isOpen = false;
        if (guidePanel != null)
            guidePanel.SetActive(false);
    }

    // ----------------------------------------------------------------
    //  Nav button wiring (reads pre-placed children, no instantiation)
    // ----------------------------------------------------------------

    private void BuildNavButtons()
    {
        _navButtons.Clear();

        if (navButtonContainer == null) return;

        for (int i = 0; i < navButtonContainer.childCount; i++)
        {
            Button btn = navButtonContainer.GetChild(i).GetComponent<Button>();
            if (btn == null) continue;

            // Sync the button label text with the matching entry title.
            if (i < entries.Count)
            {
                TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = entries[i].title;
            }

            int captured = i;
            btn.onClick.AddListener(() => SelectEntry(captured));
            _navButtons.Add(btn);
        }

        if (_navButtons.Count != entries.Count)
            Debug.LogWarning($"[UserGuideManager] Nav button count ({_navButtons.Count}) " +
                             $"does not match entry count ({entries.Count}). " +
                             "Add or remove buttons in NavContent to match.");
    }

    // ----------------------------------------------------------------
    //  Entry selection
    // ----------------------------------------------------------------

    public void SelectEntry(int index)
    {
        if (entries.Count == 0) return;
        index = Mathf.Clamp(index, 0, entries.Count - 1);

        _selectedIndex   = index;
        _lastOpenIndex   = index;
        _currentImageIdx = 0;

        var entry = entries[index];

        if (entryTitleText  != null) entryTitleText.text  = entry.title;
        if (descriptionText != null) descriptionText.text = entry.description;

        RefreshImage();
        RefreshNavHighlight();
    }

    // ----------------------------------------------------------------
    //  Image carousel
    // ----------------------------------------------------------------

    public void NextImage()
    {
        if (!HasImages(out int count)) return;
        _currentImageIdx = (_currentImageIdx + 1) % count;
        RefreshImage();
    }

    public void PrevImage()
    {
        if (!HasImages(out int count)) return;
        _currentImageIdx = (_currentImageIdx - 1 + count) % count;
        RefreshImage();
    }

    private void RefreshImage()
    {
        bool has = HasImages(out int count);
        _currentImageIdx = has ? Mathf.Clamp(_currentImageIdx, 0, count - 1) : 0;

        if (imageDisplay != null)
        {
            imageDisplay.gameObject.SetActive(has);
            if (has)
                imageDisplay.sprite = entries[_selectedIndex].images[_currentImageIdx];
        }

        bool multiPage = has && count > 1;

        if (imagePageLabel != null)
        {
            imagePageLabel.gameObject.SetActive(multiPage);
            if (multiPage)
                imagePageLabel.text = $"{_currentImageIdx + 1} / {count}";
        }

        if (prevImageButton != null) prevImageButton.gameObject.SetActive(multiPage);
        if (nextImageButton != null) nextImageButton.gameObject.SetActive(multiPage);

        if (noImageLabel != null)
            noImageLabel.gameObject.SetActive(!has);
    }

    private bool HasImages(out int count)
    {
        if (_selectedIndex < 0 || _selectedIndex >= entries.Count)
        {
            count = 0;
            return false;
        }
        var imgs = entries[_selectedIndex].images;
        count = imgs != null ? imgs.Length : 0;
        return count > 0;
    }

    // ----------------------------------------------------------------
    //  Search
    // ----------------------------------------------------------------

    private void OnSearchChanged(string query)
    {
        string q = query.Trim().ToLower();

        bool anyVisible = false;
        int  firstVisible = -1;

        for (int i = 0; i < _navButtons.Count && i < entries.Count; i++)
        {
            bool match = string.IsNullOrEmpty(q)
                || entries[i].title.ToLower().Contains(q)
                || entries[i].description.ToLower().Contains(q);

            _navButtons[i].gameObject.SetActive(match);

            if (match && firstVisible < 0)
                firstVisible = i;

            if (match)
                anyVisible = true;
        }

        // If the currently selected entry was hidden by the filter, jump to the first visible one.
        if (anyVisible && (_selectedIndex < 0 || !_navButtons[_selectedIndex].gameObject.activeSelf))
            SelectEntry(firstVisible);
    }

    private void ShowAllNavButtons()
    {
        foreach (var b in _navButtons)
            if (b != null) b.gameObject.SetActive(true);
    }

    // ----------------------------------------------------------------
    //  Nav highlight
    // ----------------------------------------------------------------

    private void RefreshNavHighlight()
    {
        for (int i = 0; i < _navButtons.Count; i++)
        {
            if (_navButtons[i] == null) continue;

            Image bg = _navButtons[i].GetComponent<Image>();
            if (bg != null)
                bg.color = (i == _selectedIndex) ? navSelectedColor : navNormalColor;
        }
    }
}
