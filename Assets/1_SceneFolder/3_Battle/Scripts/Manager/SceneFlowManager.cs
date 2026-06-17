using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 씬 전환 흐름을 관리하는 매니저.
/// Battle1 → Reward1 → BattleBoss1 → Victory
/// </summary>
public enum SceneType { 
    BattleScene, // 전투씬
    BossScene, // 보스씬 
    Heal, // 회복 씬
    RandomStage, //랜덤 이벤트 씬
    RoadMap, // 지도 씬
    Shop, // 상점 씬
    Title, // 타이틀 씬
    Ending // 엔딩 씬
}

public class SceneFlowManager : MonoBehaviour
{
    public static SceneFlowManager Instance { get; private set; }

    private Dictionary<SceneType, SceneType> sceneFlowMap;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSceneFlow();
        }
        else Destroy(gameObject);
    }

    private void InitializeSceneFlow()
    {
        // 단방향 고정 흐름만 여기서 관리
        sceneFlowMap = new Dictionary<SceneType, SceneType>
    {
        { SceneType.BattleScene, SceneType.RoadMap  }, // 전투 → 로드맵
        { SceneType.Heal,        SceneType.RoadMap  }, // 회복 → 로드맵
        { SceneType.RandomStage, SceneType.RoadMap  }, // 랜덤이벤트 → 로드맵
        { SceneType.BossScene,   SceneType.Ending   }, // 보스 → 완결
    };
    }

    /// <summary>지정된 씬으로 페이드 전환한다.</summary>
    public void LoadScene(SceneType scene)
    {
        StartCoroutine(LoadSceneWithFade(scene.ToString()));
    }

    /// <summary>노드 선택 시 직접 씬을 지정해서 이동한다.</summary>
    public void LoadSceneFromNode(SceneType targetScene)
    {
        LoadScene(targetScene);
    }

    /// <summary>현재 씬 다음 씬으로 이동한다.</summary>
    public void LoadNextAfterBattle()
    {
        if (System.Enum.TryParse(SceneManager.GetActiveScene().name, out SceneType current))
        {
            if (sceneFlowMap.TryGetValue(current, out SceneType next))
                LoadScene(next);
            else
                Logger.LogWarning($"[SceneFlowManager] '{current}'의 다음 씬이 정의되지 않았습니다.");
        }
        else
            Logger.LogError($"[SceneFlowManager] 현재 씬 '{SceneManager.GetActiveScene().name}'이 SceneType에 없습니다.");
    }

    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        if (ScreenFader.Instance == null)
            new GameObject("ScreenFader").AddComponent<ScreenFader>();

        yield return ScreenFader.Instance.FadeOut();

        AudioManager.Instance?.StopMusic();

        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f) yield return null;

        yield return null;
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        ScreenFader.Instance?.BringToFront();
        yield return null;
        yield return ScreenFader.Instance.FadeIn();
    }

    /// <summary>현재 씬을 다시 로드한다.</summary>
    public void RetryCurrentScene()
    {
        Time.timeScale = 1f;
        var active = SceneManager.GetActiveScene();
        if (System.Enum.TryParse(active.name, out SceneType current))
            LoadScene(current);
        else
            SceneManager.LoadScene(active.name);
    }

    public void LoadMainMenu() => LoadScene(SceneType.Title);
    public void LoadRetry() => RetryCurrentScene();
}