using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class OneWayToggle : MonoBehaviour
{
    private Toggle myToggle;

    [Header("이미지 변경 설정")]
    public Image targetImage; // 이미지가 교체될 대상 (Background)
    public Sprite offSprite;  // 체크 전 이미지 (뛰어가는 고블린)
    public Sprite onSprite;   // 체크 후 이미지 (쓰러진 고블린)

    [Header("씬 전환 설정")]
    [Tooltip("토글을 누르면 이동할 씬을 설정하세요.")]
    public SceneType targetScene;
    // 중복 호출 방지 플래그
    private bool isTransitioning = false;

    [Header("SFX")]
    [Tooltip("토글 클릭 시 재생할 AudioClip을 지정하세요.")]
    [SerializeField] private AudioClip clickSFX;
    [Range(0f, 1f)]
    [SerializeField] private float clickSFXVolume = 1f;

    // 클릭 한 번만 소리 재생을 보장하는 내부 플래그
    private bool clickPlayed = false;
    private const float clickResetDelay = 0.12f; // 짧은 시간 후 재생 가능하도록 리셋

    private void Awake()
    {
        myToggle = GetComponent<Toggle>();

        // 유니티의 기본 체크마크 덮어쓰기 로직을 완전히 차단합니다.
        if (myToggle.graphic != null)
        {
            myToggle.graphic.enabled = false;
            myToggle.graphic = null;
        }

        // 상태가 변할 때 이벤트를 연결합니다.
        myToggle.onValueChanged.AddListener(OnToggleValueChanged);

        // 게임 시작 시 현재 상태(체크 여부)에 맞춰 이미지를 갱신합니다.
        UpdateImage(myToggle.isOn);
    }

    private void OnToggleValueChanged(bool isOn)
    {
        // 클릭 시 사운드: 중복 재생 방지
        if (!clickPlayed)
        {
            PlayClickSFX();
            clickPlayed = true;
            StartCoroutine(ResetClickPlayedCoroutine());
        }

        // 체크 해제를 시도하면 강제로 다시 켜짐(true) 상태로 돌려놓습니다.
        if (!isOn)
        {
            myToggle.SetIsOnWithoutNotify(true);
            isOn = true;
        }

        // 이미지를 교체합니다.
        UpdateImage(isOn);

        // 토글이 눌렸을 때 설정한 씬으로 이동 시도
        TryNavigateToTargetScene();
    }

    private IEnumerator ResetClickPlayedCoroutine()
    {
        yield return new WaitForSeconds(clickResetDelay);
        clickPlayed = false;
    }

    private void UpdateImage(bool isOn)
    {
        if (targetImage != null)
        {
            // 켜져 있으면 쓰러진 고블린, 꺼져 있으면 뛰어가는 고블린으로 통째로 교체합니다.
            targetImage.sprite = isOn ? onSprite : offSprite;
        }
    }

    /// <summary>
    /// 토글 클릭 시 씬 전환을 시도한다. 중복 호출을 방지하며
    /// SceneFlowManager가 없으면 에러 로그를 남긴다.
    /// EscapeMenu의 동작을 참고하여 Time.timeScale을 보장한다.
    /// </summary>
    public void TryNavigateToTargetScene()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        Time.timeScale = 1f;

        // 유물 랜덤 획득 로직 실행
        AcquireRandomRelic();

        // 2초 후 이동
        StartCoroutine(NavigateAfterDelay(2f));
    }

    private IEnumerator NavigateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (SceneFlowManager.Instance != null)
        {
            Debug.Log($"[OneWayToggle] {delay}초 대기 후 {targetScene} 씬으로 이동합니다.");
            SceneFlowManager.Instance.LoadScene(targetScene);
        }
        else
        {
            Debug.LogError("[OneWayToggle] SceneFlowManager.Instance가 씬에 존재하지 않습니다!");
            isTransitioning = false;
        }
    }

    private void AcquireRandomRelic()
    {
        if (RelicManager.Instance == null || RelicManager.Instance.relicDatabase == null) return;

        List<RelicData> allRelics = RelicManager.Instance.relicDatabase.relics;
        
        // Legendary 등급 제외 및 이미 보유한 유물 제외
        List<RelicData> availableRelics = allRelics.FindAll(r => r.rarity != "Legendary" && !RelicManager.Instance.HasRelic(r.itemId));

        if (availableRelics.Count == 0) return;

        // 등급별 확률 설정 (Legendary 제외). 예: Common 70%, Rare 30%
        int rand = UnityEngine.Random.Range(0, 100);
        string targetRarity = rand < 70 ? "Common" : "Rare";

        List<RelicData> filtered = availableRelics.FindAll(r => r.rarity == targetRarity);
        
        // 해당 등급의 유물이 없으면 전체 중 랜덤하게 선택
        RelicData selected = (filtered.Count > 0) 
            ? filtered[UnityEngine.Random.Range(0, filtered.Count)] 
            : availableRelics[UnityEngine.Random.Range(0, availableRelics.Count)];

        if (selected != null)
        {
            RelicManager.Instance.AddRelicToPlayer(selected.itemId);
            Debug.Log($"[OneWayToggle] 유물 획득 시도: {selected.itemId} ({selected.rarity})");
        }
    }

    private void PlayClickSFX()
    {
        if (clickSFX == null) return;

        float global = 1f;
        if (OptionsManager.Instance != null)
            global = OptionsManager.Instance.GetSFXVolume();

        float vol = Mathf.Clamp01(clickSFXVolume * global);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clickSFX, vol);
        }
        else
        {
            // 폴백: 카메라 위치에서 2D로 재생
            AudioSource.PlayClipAtPoint(clickSFX, Camera.main != null ? Camera.main.transform.position : Vector3.zero, vol);
        }
    }
}

