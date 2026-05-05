using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("Grouped SFX Sliders + Test Buttons")]
    public Slider menuVolumeSlider;
    public Button menuTestButton;

    public Slider ingameVolumeSlider;
    public Button ingameTestButton;

    [Header("Music Volume")]
    public Slider musicVolumeSlider;

    void Start()
    {
        // ✅ Initialize sliders
        menuVolumeSlider.value = SoundManager.instance.menuVolume;
        ingameVolumeSlider.value = SoundManager.instance.ingameVolume;
        musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);

        // ✅ Add listeners for sliders
        menuVolumeSlider.onValueChanged.AddListener(OnMenuVolumeChanged);
        ingameVolumeSlider.onValueChanged.AddListener(OnIngameVolumeChanged);
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        // ✅ Add listeners for test buttons
        menuTestButton.onClick.AddListener(TestMenuSFX);
        ingameTestButton.onClick.AddListener(TestIngameSFX);
    }

    void OnMenuVolumeChanged(float value) => SoundManager.instance.SetMenuVolume(value);
    void OnIngameVolumeChanged(float value) => SoundManager.instance.SetIngameVolume(value);
    void OnMusicVolumeChanged(float value) => MusicManager.Instance.SetMusicVolume(value);

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
