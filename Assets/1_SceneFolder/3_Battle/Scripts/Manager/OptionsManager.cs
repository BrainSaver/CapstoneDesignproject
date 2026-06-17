// Assets/Battle/Scripts/Managers/OptionsManager.cs
using UnityEngine;

/// <summary>
/// 게임 옵션(볼륨, 해상도 등) 설정을 관리하는 매니저.
/// GameManager에서 프리팹으로 생성하거나 씬에 직접 배치해 사용한다.
/// </summary>
public class OptionsManager : MonoBehaviour
{
    public static OptionsManager Instance { get; private set; }

    [Header("기본 볼륨 설정")]
    [Range(0f, 1f)][SerializeField] private float defaultMusicVolume = 1f; // 기본 BGM 볼륨
    [Range(0f, 1f)][SerializeField] private float defaultSFXVolume = 1f; // 기본 SFX 볼륨

    // PlayerPrefs 저장 키
    private const string KeyMusicVolume = "MusicVolume";
    private const string KeySFXVolume = "SFXVolume";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>저장된 설정을 불러와 AudioManager에 적용한다.</summary>
    private void LoadSettings()
    {
        float musicVolume = PlayerPrefs.GetFloat(KeyMusicVolume, defaultMusicVolume);
        float sfxVolume = PlayerPrefs.GetFloat(KeySFXVolume, defaultSFXVolume);

        ApplyMusicVolume(musicVolume);
        ApplySFXVolume(sfxVolume);

        Logger.Log($"[OptionsManager] 설정 로드 완료. BGM={musicVolume}, SFX={sfxVolume}");
    }

    /// <summary>현재 설정을 PlayerPrefs에 저장한다.</summary>
    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(KeyMusicVolume, GetMusicVolume());
        PlayerPrefs.SetFloat(KeySFXVolume, GetSFXVolume());
        PlayerPrefs.Save();
        Logger.Log("[OptionsManager] 설정 저장 완료.");
    }

    // ── BGM 볼륨 ────────────────────────────────────────────────

    /// <summary>BGM 볼륨을 설정하고 AudioManager에 적용한다.</summary>
    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(KeyMusicVolume, volume);
        ApplyMusicVolume(volume);
    }

    /// <summary>현재 BGM 볼륨을 반환한다.</summary>
    public float GetMusicVolume() => PlayerPrefs.GetFloat(KeyMusicVolume, defaultMusicVolume);

    // ── SFX 볼륨 ────────────────────────────────────────────────

    /// <summary>SFX 볼륨을 설정하고 AudioManager에 적용한다.</summary>
    public void SetSFXVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(KeySFXVolume, volume);
        ApplySFXVolume(volume);
    }

    /// <summary>현재 SFX 볼륨을 반환한다.</summary>
    public float GetSFXVolume() => PlayerPrefs.GetFloat(KeySFXVolume, defaultSFXVolume);

    // ── 내부 적용 헬퍼 ──────────────────────────────────────────

    private void ApplyMusicVolume(float volume)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(volume);
    }

    private void ApplySFXVolume(float volume)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(volume);
    }
}