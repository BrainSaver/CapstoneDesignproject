using UnityEngine;

/// <summary>
/// 핵심 게임 시스템(오디오, 옵션)을 초기화하고 씬 전환에도 유지하는 중앙 매니저.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public AudioManager AudioManager { get; private set; }
    public OptionsManager OptionsManager { get; private set; }

    [Header("커서 설정")]
    [SerializeField] private Texture2D cursorTexture;
    [SerializeField] private Vector2 hotspot = Vector2.zero;
    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;

    private void Awake()
    {
        InitializeManagers();

        if (cursorTexture != null)
        {
            Cursor.SetCursor(cursorTexture, hotspot, cursorMode);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    /// <summary>핵심 매니저 프리팹을 로드하거나 자식에서 탐색해 초기화한다.</summary>
    private void InitializeManagers()
    {
        AudioManager = LoadOrInstantiateManager<AudioManager>("Prefabs/Managers/AudioManager");
        OptionsManager = LoadOrInstantiateManager<OptionsManager>("Prefabs/Managers/OptionsManager");
    }

    private T LoadOrInstantiateManager<T>(string prefabPath) where T : Component
    {
        T manager = GetComponentInChildren<T>();
        if (manager != null) return manager;

        GameObject prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab == null)
        {
            Logger.LogError($"[GameManager] {typeof(T).Name} 프리팹을 찾을 수 없습니다: {prefabPath}", this);
            return null;
        }

        GameObject instance = Instantiate(prefab, transform.position, Quaternion.identity, transform);
        manager = instance.GetComponent<T>();

        if (manager == null)
            Logger.LogError($"[GameManager] {typeof(T).Name} 컴포넌트가 없습니다.", this);
        else
            Logger.Log($"[GameManager] {typeof(T).Name} 초기화 완료.", this);

        return manager;
    }
}