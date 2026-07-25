/*
 * ================================================================
 *  UNITY SETUP GUIDE — WalkthroughGuideManager
 * ================================================================
 *
 *  !! COARSE-GRAINED TRIGGER SYSTEM !!
 *  Each trigger fires ONCE per scene session for a MAJOR feature milestone —
 *  NOT for every individual component action. The trigger list evolves over time.
 *  When adding a trigger: add to WalkthroughTrigger enum, populate a sequence in
 *  the Inspector, and add a TryTrigger() call at the relevant integration point.
 *
 * ----------------------------------------------------------------
 *  STEP 1 — Attach this script to a dedicated Canvas:
 *    Render Mode:  Screen Space - Overlay
 *    Sort Order:   300   ← above UserGuideManager (200) and everything else
 *    Add CanvasScaler + GraphicRaycaster
 *
 *  STEP 2 — Build the panel (starts INACTIVE):
 *
 *    WalkthroughPanel  (Image background, starts inactive)
 *      ├─ HeaderText       (TMP_Text)  ← static "Walkthrough Guide" label
 *      ├─ ImageDisplay     (Image)     ← big guide image; hidden when frame.image is null
 *      ├─ FrameTitle       (TMP_Text)  ← per-frame title
 *      ├─ FrameDescription (TMP_Text)  ← per-frame description
 *      ├─ PageLabel        (TMP_Text)  ← "1 / 4" page indicator
 *      ├─ PrevButton       (Button)    ← navigate to previous frame
 *      ├─ NextButton       (Button)    ← navigate to next frame
 *      └─ ContinueButton   (Button)    ← finishes guide; non-interactable until last frame seen
 *
 *  STEP 3 — Wire Inspector fields:
 *    • All UI refs above
 *    • suFrontPowerButton, monitorPowerButton, avrPowerButton, psuSwitchController
 *      (needed for AllPowerSourcesOff trigger check)
 *    • backCablePorts[] — only SU-back, Monitor-back, and AVR-back CablePort components
 *      (needed for AllBackCablesUnplugged trigger check; excludes MB/GPU/HDD ports)
 *
 *  STEP 4 — Fill "Sequences" in the Inspector:
 *    Each entry maps a WalkthroughTrigger to one or more WalkthroughFrames.
 *    Each frame has: image (Sprite, optional), title, description.
 *    Continue is locked until the player navigates to the last frame at least once.
 *    Navigating back after reaching the end does NOT re-lock Continue.
 *
 *  STEP 5 — Force-skip shortcut (TESTING ONLY):
 *    Hold Ctrl + Shift + 6 + 7 simultaneously to instantly dismiss the current guide
 *    and drain the entire queue without completing it.
 *
 *  PERSISTENCE NOTE:
 *    All seen-state resets on every scene load (current behaviour).
 *    Future save integration: replace the _seenOrQueued.Clear() in Awake with
 *    a load from save data; replace ForceSkip / OnContinue with a save call.
 * ================================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class WalkthroughGuideManager : MonoBehaviour
{
    public static WalkthroughGuideManager Instance { get; private set; }

    // ----------------------------------------------------------------
    //  Trigger types
    //  COARSE-GRAINED — fires once per session for major milestones only.
    // ----------------------------------------------------------------
    public enum WalkthroughTrigger
    {
        SceneEnter,
        FirstComponentDrop,
        FirstDetailView,
        AllPowerSourcesOff,
        AllBackCablesUnplugged,
        DetailViewAngleChange,
        InventoryToggle,
        InventoryBackButton,
        MajorHardwareDeployed
    }

    // ----------------------------------------------------------------
    //  Data model
    // ----------------------------------------------------------------

    [System.Serializable]
    public class WalkthroughFrame
    {
        [Tooltip("Optional image shown for this frame. Leave null for text-only frames.")]
        public Sprite image;
        [TextArea(2, 5)]
        public string title = "Frame Title";
        [TextArea(3, 10)]
        public string description = string.Empty;
    }

    [System.Serializable]
    public class WalkthroughSequence
    {
        public WalkthroughTrigger trigger;
        public List<WalkthroughFrame> frames = new();
    }

    // ----------------------------------------------------------------
    //  Inspector — Guide Data
    // ----------------------------------------------------------------

    [Header("Guide Sequences")]
    [SerializeField] private List<WalkthroughSequence> sequences = new();

    // ----------------------------------------------------------------
    //  Inspector — Panel UI
    // ----------------------------------------------------------------

    [Header("Panel Root")]
    [SerializeField] private GameObject walkthroughPanel;

    [Header("UI References")]
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private Image    imageDisplay;
    [SerializeField] private TMP_Text frameTitle;
    [SerializeField] private TMP_Text frameDescription;
    [SerializeField] private TMP_Text pageLabel;
    [SerializeField] private Button   prevButton;
    [SerializeField] private Button   nextButton;
    [SerializeField] private Button   continueButton;

    // ----------------------------------------------------------------
    //  Inspector — Aggregate Trigger References
    // ----------------------------------------------------------------

    [Header("Power Sources (AllPowerSourcesOff trigger)")]
    [SerializeField] private PowerButton         suFrontPowerButton;
    [SerializeField] private MonitorPowerButton  monitorPowerButton;
    [SerializeField] private AVRPowerButton      avrPowerButton;
    [SerializeField] private PSUSwitchController psuSwitchController;

    [Header("Back Cable Ports (AllBackCablesUnplugged trigger)")]
    [Tooltip("Assign only SU-back, Monitor-back, and AVR-back CablePort components. Excludes MB/GPU/HDD ports.")]
    [SerializeField] private CablePort[] backCablePorts = new CablePort[0];

    // ----------------------------------------------------------------
    //  Private state
    // ----------------------------------------------------------------

    private readonly Queue<WalkthroughSequence>     _queue        = new();
    private readonly HashSet<WalkthroughTrigger>    _seenOrQueued = new();

    private WalkthroughSequence _currentSequence;
    private int  _currentFrameIndex;
    private bool _hasReachedEnd;
    private bool _isShowing;

    private int _angleChangeCount;

    public bool IsShowing => _isShowing;

    // ----------------------------------------------------------------
    //  Unity lifecycle
    // ----------------------------------------------------------------

    private void Awake()
    {
        Instance = this;
        // Reset every scene load. Future save integration: load _seenOrQueued from save data here.
        _seenOrQueued.Clear();
    }

    private void Start()
    {
        if (prevButton     != null) prevButton.onClick.AddListener(OnPrev);
        if (nextButton     != null) nextButton.onClick.AddListener(OnNext);
        if (continueButton != null) continueButton.onClick.AddListener(OnContinue);

        if (headerText != null) headerText.text = "Walkthrough Guide";

        if (walkthroughPanel != null)
            walkthroughPanel.SetActive(false);

        // Fire immediately on scene entry.
        TryTrigger(WalkthroughTrigger.SceneEnter);
    }

    private void Update()
    {
        if (!_isShowing) return;

        var kb = Keyboard.current;
        if (kb != null &&
            kb.ctrlKey.isPressed    &&
            kb.shiftKey.isPressed   &&
            kb.digit6Key.isPressed  &&
            kb.digit7Key.isPressed)
        {
            ForceSkip();
        }
    }

    // ----------------------------------------------------------------
    //  Public trigger API
    // ----------------------------------------------------------------

    /// <summary>
    /// Call this from any integration point to fire a walkthrough sequence once.
    /// Silently ignored if the trigger has already been queued or seen this session.
    /// </summary>
    public void TryTrigger(WalkthroughTrigger trigger)
    {
        if (_seenOrQueued.Contains(trigger)) return;

        WalkthroughSequence seq = sequences.Find(s => s.trigger == trigger);
        if (seq == null || seq.frames == null || seq.frames.Count == 0) return;

        _seenOrQueued.Add(trigger);
        _queue.Enqueue(seq);
        TryShowNext();
    }

    /// <summary>
    /// Call from PowerButton, MonitorPowerButton, AVRPowerButton, PSUSwitchController
    /// after any power-state change. Fires AllPowerSourcesOff when all four are off.
    /// </summary>
    public void NotifyPowerStateChanged()
    {
        bool allOff =
            (suFrontPowerButton  == null || !suFrontPowerButton.IsPoweredOn)  &&
            (monitorPowerButton  == null || !monitorPowerButton.IsPoweredOn)  &&
            (avrPowerButton      == null || !avrPowerButton.IsPoweredOn)      &&
            (psuSwitchController == null || !psuSwitchController.IsOn);

        if (allOff)
            TryTrigger(WalkthroughTrigger.AllPowerSourcesOff);
    }

    /// <summary>
    /// Call from HardwareViewController when the user presses a number key to change angle.
    /// Fires DetailViewAngleChange after the second distinct angle switch.
    /// </summary>
    public void NotifyViewAngleChanged()
    {
        _angleChangeCount++;
        if (_angleChangeCount >= 2)
            TryTrigger(WalkthroughTrigger.DetailViewAngleChange);
    }

    /// <summary>
    /// Call from CablePort.SetUninstalled() for every cable unplug.
    /// Fires AllBackCablesUnplugged only when every port in backCablePorts is uninstalled.
    /// </summary>
    public void NotifyBackCableUnplugged()
    {
        if (backCablePorts == null || backCablePorts.Length == 0) return;

        foreach (CablePort port in backCablePorts)
        {
            if (port == null || port.IsInstalled) return;
        }

        TryTrigger(WalkthroughTrigger.AllBackCablesUnplugged);
    }

    // ----------------------------------------------------------------
    //  Show / hide
    // ----------------------------------------------------------------

    private void TryShowNext()
    {
        if (_isShowing || _queue.Count == 0) return;

        _currentSequence   = _queue.Dequeue();
        _currentFrameIndex = 0;
        _hasReachedEnd     = _currentSequence.frames.Count <= 1;
        _isShowing         = true;

        if (walkthroughPanel != null)
            walkthroughPanel.SetActive(true);

        RefreshFrame();
    }

    private void HidePanel()
    {
        _isShowing = false;
        if (walkthroughPanel != null)
            walkthroughPanel.SetActive(false);
    }

    // ----------------------------------------------------------------
    //  Frame navigation
    // ----------------------------------------------------------------

    private void OnNext()
    {
        if (_currentSequence == null) return;

        int total = _currentSequence.frames.Count;
        _currentFrameIndex = Mathf.Min(_currentFrameIndex + 1, total - 1);

        if (_currentFrameIndex == total - 1)
            _hasReachedEnd = true;

        RefreshFrame();
    }

    private void OnPrev()
    {
        if (_currentSequence == null) return;
        _currentFrameIndex = Mathf.Max(_currentFrameIndex - 1, 0);
        RefreshFrame();
    }

    private void OnContinue()
    {
        HidePanel();
        TryShowNext();
    }

    // ----------------------------------------------------------------
    //  UI refresh
    // ----------------------------------------------------------------

    private void RefreshFrame()
    {
        if (_currentSequence == null || _currentSequence.frames.Count == 0) return;

        WalkthroughFrame frame = _currentSequence.frames[_currentFrameIndex];
        int total = _currentSequence.frames.Count;

        if (frameTitle       != null) frameTitle.text       = frame.title;
        if (frameDescription != null) frameDescription.text = frame.description;

        bool hasImage = frame.image != null;
        if (imageDisplay != null)
        {
            imageDisplay.gameObject.SetActive(hasImage);
            if (hasImage) imageDisplay.sprite = frame.image;
        }

        if (pageLabel != null)
            pageLabel.text = $"{_currentFrameIndex + 1} / {total}";

        if (prevButton != null) prevButton.interactable = _currentFrameIndex > 0;
        if (nextButton != null) nextButton.interactable = _currentFrameIndex < total - 1;

        if (continueButton != null)
            continueButton.interactable = _hasReachedEnd;
    }

    // ----------------------------------------------------------------
    //  Force skip — TESTING ONLY (Ctrl + Shift + 6 + 7)
    // ----------------------------------------------------------------

    private void ForceSkip()
    {
        _queue.Clear();
        HidePanel();
        Debug.Log("[WalkthroughGuideManager] Force-skipped via dev shortcut.");
    }
}
