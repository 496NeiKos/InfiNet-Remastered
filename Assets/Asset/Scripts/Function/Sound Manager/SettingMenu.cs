using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    [Header("Grouped SFX Sliders + Test Buttons")]
    public Slider menuVolumeSlider;
    public Button menuTestButton;

    public Slider ingameVolumeSlider;
    public Button ingameTestButton;

    [Header("Music Volume")]
    public Slider musicVolumeSlider;

    [Header("Volume Labels")]
    public TMP_Text menuVolumeLabel;
    public TMP_Text ingameVolumeLabel;
    public TMP_Text musicVolumeLabel;

    void Start()
    {
        // ✅ Initialize sliders
        menuVolumeSlider.value = SoundManager.instance.menuVolume;
        ingameVolumeSlider.value = SoundManager.instance.ingameVolume;
        musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);

        // ✅ Initialize labels
        UpdateLabel(menuVolumeLabel, menuVolumeSlider.value);
        UpdateLabel(ingameVolumeLabel, ingameVolumeSlider.value);
        UpdateLabel(musicVolumeLabel, musicVolumeSlider.value);

        // ✅ Add listeners for sliders
        menuVolumeSlider.onValueChanged.AddListener(OnMenuVolumeChanged);
        ingameVolumeSlider.onValueChanged.AddListener(OnIngameVolumeChanged);
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        // ✅ Add listeners for test buttons
        menuTestButton.onClick.AddListener(TestMenuSFX);
        ingameTestButton.onClick.AddListener(TestIngameSFX);
    }

    void UpdateLabel(TMP_Text label, float value) =>
        label.text = Mathf.RoundToInt(value * 100) + "%";

    void OnMenuVolumeChanged(float value)
    {
        SoundManager.instance.SetMenuVolume(value);
        UpdateLabel(menuVolumeLabel, value);
    }

    void OnIngameVolumeChanged(float value)
    {
        SoundManager.instance.SetIngameVolume(value);
        UpdateLabel(ingameVolumeLabel, value);
    }

    void OnMusicVolumeChanged(float value)
    {
        MusicManager.Instance.SetMusicVolume(value);
        UpdateLabel(musicVolumeLabel, value);
    }

    // ✅ Test buttons
    void TestMenuSFX()
    {
        // Play one of the menu sounds (Click as preview)
        SoundManager.instance.PlaySFX(SoundManager.instance.clickSFX);
    }

    void TestIngameSFX()
    {
        // Play one of the in-game sounds (AVR as preview)
        SoundManager.instance.PlaySFX(SoundManager.instance.avrSFX);
    }
}
