using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    private AudioSource audioSource;

    [Header("Static SFX")]
    public AudioClip startSFX;
    public AudioClip quitSFX;
    public AudioClip errorSFX;

    [Header("Menu SFX")]
    public AudioClip clickSFX;
    public AudioClip backSFX;
    public AudioClip confirmSFX;
    [Range(0f, 1f)] public float menuVolume = 1f;

    [Header("In-Game SFX")]
    public AudioClip avrSFX;
    public AudioClip vgaSFX;
    public AudioClip dropSFX;
    public AudioClip mbSFX;
    [Range(0f, 1f)] public float ingameVolume = 1f;

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        // ✅ Load saved values
        menuVolume = PlayerPrefs.GetFloat("MenuVolume", 1f);
        ingameVolume = PlayerPrefs.GetFloat("IngameVolume", 1f);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        float volume = 1f;

        if (clip == clickSFX || clip == backSFX || clip == confirmSFX)
            volume = menuVolume;
        else if (clip == avrSFX || clip == vgaSFX || clip == dropSFX || clip == mbSFX)
            volume = ingameVolume;

        audioSource.PlayOneShot(clip, volume);
    }

    public void SetMenuVolume(float value)
    {
        menuVolume = value;
        PlayerPrefs.SetFloat("MenuVolume", value);
        PlayerPrefs.Save();
    }

    public void SetIngameVolume(float value)
    {
        ingameVolume = value;
        PlayerPrefs.SetFloat("IngameVolume", value);
        PlayerPrefs.Save();
    }
}
