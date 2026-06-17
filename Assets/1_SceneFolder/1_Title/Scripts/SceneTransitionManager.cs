using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor; // 씬 에셋을 드래그 앤 드롭하기 위해 필요한 에디터 전용 네임스페이스입니다.
#endif

public class SceneTransitionManager : MonoBehaviour
{
    [Header("씬 이동 설정")]
    [Tooltip("클릭 시 작동할 버튼 UI를 드래그해서 넣어주세요.")]
    public Button targetButton;

#if UNITY_EDITOR
    [Tooltip("이동할 씬(Scene) 에셋 파일을 직접 드래그해서 넣어주세요.")]
    public SceneAsset sceneToLoad;
#endif

    // 인스펙터에는 보이지 않지만, 드래그한 씬의 이름을 자동으로 저장해두는 변수입니다.
    [HideInInspector]
    public string sceneNameToLoad;

    [Tooltip("버튼 클릭 후 씬이 전환되기까지의 대기 시간(초)입니다.")]
    public float delayTime = 2.0f;

    // 인스펙터에서 값이 변경될 때마다 자동으로 실행되는 유니티 내장 함수입니다.
    private void OnValidate()
    {
#if UNITY_EDITOR
        // 드래그해서 넣은 씬 파일이 있다면, 그 파일의 이름을 문자열로 추출하여 저장합니다.
        if (sceneToLoad != null)
        {
            sceneNameToLoad = sceneToLoad.name;
        }
        else
        {
            sceneNameToLoad = string.Empty;
        }
#endif
    }

    private void Start()
    {
        if (targetButton != null)
        {
            targetButton.onClick.AddListener(OnButtonClicked);
        }
        else
        {
            Debug.LogWarning("버튼이 연결되지 않았습니다. 인스펙터 창을 확인해 주세요.");
        }
    }

    private void OnButtonClicked()
    {
        if (string.IsNullOrEmpty(sceneNameToLoad))
        {
            Debug.LogWarning("이동할 씬이 드래그되지 않았습니다. 씬 파일을 할당해 주세요.");
            return;
        }

        // 다중 클릭 방지를 위해 버튼을 비활성화합니다.
        targetButton.interactable = false;

        // 코루틴을 통해 대기 시간 후 씬을 이동합니다.
        StartCoroutine(LoadSceneWithDelay());
    }

    private IEnumerator LoadSceneWithDelay()
    {
        yield return new WaitForSeconds(delayTime);
        SceneManager.LoadScene(sceneNameToLoad);
    }
}