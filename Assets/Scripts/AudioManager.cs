using UnityEngine;

/// <summary>
/// 게임 전체 오디오 볼륨을 관리한다. PlayerPrefs에 설정을 저장한다.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    const string KeyMaster = "Audio_Master";
    const string KeyBgm    = "Audio_BGM";
    const string KeySfx    = "Audio_SFX";

    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume    = 0.8f;
    [Range(0f, 1f)] public float sfxVolume    = 1f;

    AudioSource bgmSource;
    AudioSource sfxSource;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        SetupSources();
        LoadSettings();
        ApplyVolumes();
    }

    void SetupSources()
    {
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplyVolumes();
        SaveSettings();
    }

    public void SetBgmVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);
        ApplyVolumes();
        SaveSettings();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        ApplyVolumes();
        SaveSettings();
    }

    public void PlayBgm(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.volume = masterVolume * bgmVolume;
        bgmSource.Play();
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, masterVolume * sfxVolume);
    }

    void ApplyVolumes()
    {
        AudioListener.volume = masterVolume;

        if (bgmSource != null)
            bgmSource.volume = masterVolume * bgmVolume;

        if (sfxSource != null)
            sfxSource.volume = masterVolume * sfxVolume;
    }

    void SaveSettings()
    {
        PlayerPrefs.SetFloat(KeyMaster, masterVolume);
        PlayerPrefs.SetFloat(KeyBgm, bgmVolume);
        PlayerPrefs.SetFloat(KeySfx, sfxVolume);
        PlayerPrefs.Save();
    }

    void LoadSettings()
    {
        if (PlayerPrefs.HasKey(KeyMaster)) masterVolume = PlayerPrefs.GetFloat(KeyMaster);
        if (PlayerPrefs.HasKey(KeyBgm))    bgmVolume    = PlayerPrefs.GetFloat(KeyBgm);
        if (PlayerPrefs.HasKey(KeySfx))    sfxVolume    = PlayerPrefs.GetFloat(KeySfx);
    }
}
