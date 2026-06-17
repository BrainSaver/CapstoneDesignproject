// Assets/Battle/Scripts/Managers/AudioManager.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 배경음악(BGM)과 효과음(SFX)을 관리하는 싱글턴 매니저.
/// GameManager에서 프리팹으로 생성하거나 씬에 직접 배치해 사용한다.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM 설정")]
    [SerializeField] private AudioSource bgmSource;  // 배경음악 재생용 AudioSource

    [Header("SFX 설정")]
    [SerializeField] private AudioSource sfxSource;  // 효과음 재생용 AudioSource

    [Header("SFX 클립 목록")]
    [SerializeField] private List<SFXEntry> sfxList = new(); // 이름-클립 매핑 목록

    private Dictionary<string, AudioClip> sfxDictionary = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildSFXDictionary();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>SFX 리스트를 딕셔너리로 변환해 빠른 조회가 가능하게 한다.</summary>
    private void BuildSFXDictionary()
    {
        sfxDictionary.Clear();
        foreach (var entry in sfxList)
        {
            if (entry.clip != null && !sfxDictionary.ContainsKey(entry.name))
                sfxDictionary[entry.name] = entry.clip;
        }
    }

    // ── BGM ─────────────────────────────────────────────────────

    /// <summary>BGM을 재생한다. 이미 같은 클립이 재생 중이면 무시한다.</summary>
    public void PlayMusic(AudioClip clip, float volume = 1f)
    {
        if (clip == null || bgmSource == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.volume = volume;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    /// <summary>BGM을 정지한다.</summary>
    public void StopMusic()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    /// <summary>BGM 볼륨을 설정한다.</summary>
    public void SetMusicVolume(float volume)
    {
        if (bgmSource != null) bgmSource.volume = Mathf.Clamp01(volume);
    }

    // ── SFX ─────────────────────────────────────────────────────

    /// <summary>이름으로 SFX를 재생한다. 등록되지 않은 이름이면 경고를 출력한다.</summary>
    public void PlaySFX(string sfxName)
    {
        if (sfxSource == null) return;

        if (sfxDictionary.TryGetValue(sfxName, out AudioClip clip))
            sfxSource.PlayOneShot(clip);
        else
            Logger.LogWarning($"[AudioManager] SFX '{sfxName}'을 찾을 수 없습니다.");
    }

    /// <summary>AudioClip을 직접 전달해 SFX를 재생한다.</summary>
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }

    /// <summary>패배 징글 등 일회성 클립을 재생한다. BGM을 대체하지 않는다.</summary>
    public void PlayJingle(AudioClip clip, float volume = 1f)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }

    /// <summary>SFX 볼륨을 설정한다.</summary>
    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null) sfxSource.volume = Mathf.Clamp01(volume);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// SFXEntry — Inspector에서 SFX 이름과 클립을 매핑하기 위한 데이터 구조
// ─────────────────────────────────────────────────────────────────────────────
[System.Serializable]
public class SFXEntry
{
    public string name; // 코드에서 PlaySFX("이름") 으로 호출할 때 사용하는 키
    public AudioClip clip; // 실제 재생할 오디오 클립
}