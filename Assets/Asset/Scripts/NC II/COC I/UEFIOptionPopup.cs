/*
 * ================================================================
 *  UNITY SETUP GUIDE — UEFIOptionPopup
 * ================================================================
 *  PURPOSE
 *    ONE shared pop-up instance used by every UEFISettingButton.
 *    Any button calls Show() with its option list; the popup renders
 *    those options, the player picks one, the popup hides itself and
 *    notifies the button.
 *
 *  STEP 1 — Create the popup GameObject
 *    - Add a child GameObject to UEFICanvasRoot named "UEFIOptionPopup".
 *    - Give it a high sibling index so it renders on top of all panels.
 *    - Add component: UEFIOptionPopup.
 *    - Start it INACTIVE (m_IsActive: 0).
 *
 *  STEP 2 — Popup hierarchy
 *
 *    UEFIOptionPopup   ← this script, starts inactive
 *      ├─ Background   ← Image (dark overlay or panel background)
 *      ├─ TitleText    ← TMP_Text — shows the setting name
 *      └─ OptionContainer
 *           ├─ OptionButton_0   ← Button + TMP_Text child, pre-built
 *           ├─ OptionButton_1
 *           ├─ OptionButton_2
 *           └─ OptionButton_3   ← up to 4 options supported
 *
 *    All OptionButtons start ACTIVE inside OptionContainer.
 *    The script hides the ones not needed for the current setting.
 *
 *    TIP: Give OptionContainer a Vertical Layout Group so buttons
 *    auto-stack regardless of how many are visible.
 *
 *  STEP 3 — Wire the inspector
 *    UEFIOptionPopup:
 *      titleText        → TitleText TMP object
 *      optionButtons[0] → OptionButton_0
 *      optionButtons[1] → OptionButton_1
 *      optionButtons[2] → OptionButton_2
 *      optionButtons[3] → OptionButton_3
 *
 *  STEP 4 — Wire each OptionButton's OnClick
 *    OptionButton_0 OnClick → UEFIOptionPopup.SelectOption(0)
 *    OptionButton_1 OnClick → UEFIOptionPopup.SelectOption(1)
 *    OptionButton_2 OnClick → UEFIOptionPopup.SelectOption(2)
 *    OptionButton_3 OnClick → UEFIOptionPopup.SelectOption(3)
 *
 *    NOTE: SelectOption(int) is wired in the inspector (not via
 *    AddListener at runtime) so the buttons are visible in the
 *    Unity Editor's OnClick list for easy auditing.
 *
 *  HOW IT WORKS
 *    1. UEFISettingButton.OnClick() calls UEFIOptionPopup.Instance.Show()
 *    2. Show() sets the title, configures visible option buttons,
 *       stores the callback, then activates the popup GameObject.
 *    3. Player clicks an option button → SelectOption(index) fires
 *       → popup deactivates → callback invoked with chosen value.
 * ================================================================
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UEFIOptionPopup : MonoBehaviour
{
    // Lazy singleton — UEFIOptionPopup starts inactive inside an inactive canvas,
    // so Awake() never runs on scene load. The getter finds it via FindObjectOfType
    // (includeInactive: true) on first access instead of relying on Awake().
    private static UEFIOptionPopup _instance;
    public static UEFIOptionPopup Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UEFIOptionPopup>(true);
                Debug.Log(_instance != null
                    ? $"[UEFIOptionPopup] Instance found: {_instance.gameObject.name}"
                    : "[UEFIOptionPopup] Instance NOT FOUND — is UEFIOptionPopup in the scene?");
            }
            return _instance;
        }
    }

    [Tooltip("TMP text that displays the setting name at the top of the popup.")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("Pre-built option buttons. Wire each OnClick to SelectOption(n). Max 4.")]
    [SerializeField] private Button[] optionButtons;

    private string[] _currentOptions;
    private Action<string> _callback;

    // Called by UEFISettingButton. Do not call directly from buttons.
    public void Show(string settingName, string[] options, Action<string> onSelected)
    {
        Debug.Log($"[UEFIOptionPopup] Show() called — setting: '{settingName}', options: {options?.Length}");

        if (options == null || options.Length == 0)
        {
            Debug.LogWarning("[UEFIOptionPopup] Show() aborted — options is null or empty.");
            return;
        }

        _currentOptions = options;
        _callback       = onSelected;

        if (titleText != null)
            titleText.text = settingName;
        else
            Debug.LogWarning("[UEFIOptionPopup] titleText is not assigned.");

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] == null)
            {
                Debug.LogWarning($"[UEFIOptionPopup] optionButtons[{i}] is null — check inspector.");
                continue;
            }

            bool visible = i < options.Length;
            optionButtons[i].gameObject.SetActive(visible);

            if (visible)
            {
                var tmp = optionButtons[i].GetComponentInChildren<TMP_Text>();
                if (tmp != null) tmp.text = options[i];
                else Debug.LogWarning($"[UEFIOptionPopup] optionButtons[{i}] has no TMP_Text child — text won't update.");
            }
        }

        Debug.Log("[UEFIOptionPopup] Activating popup.");
        gameObject.SetActive(true);
    }

    // Wire each OptionButton's OnClick to this method with its index (0-3).
    public void SelectOption(int index)
    {
        if (_currentOptions == null || index < 0 || index >= _currentOptions.Length) return;

        string chosen = _currentOptions[index];
        gameObject.SetActive(false);
        _callback?.Invoke(chosen);

        Debug.Log($"[UEFIOptionPopup] Selected: {chosen}");
    }
}
