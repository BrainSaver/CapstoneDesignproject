using UnityEngine;

/// <summary>
/// 씬 단위 싱글턴 베이스 클래스.
/// DontDestroyOnLoad 없이 씬 내에서만 유지되며, OnDestroy 시 Instance를 초기화해 씬 재로드를 허용한다.
/// </summary>
public abstract class SceneSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        var self = this as T;
        if (Instance != null && Instance != self)
        {
            Debug.LogWarning($"[{typeof(T).Name}] 중복 인스턴스 감지. 파괴합니다.", this);
            Destroy(gameObject);
            return;
        }
        Instance = self;
    }

    protected virtual void OnDestroy()
    {
        // 씬 언로드 시 Instance 초기화 → 씬 재로드 시 재등록 가능
        if (Instance == (this as T))
            Instance = null;
    }
}