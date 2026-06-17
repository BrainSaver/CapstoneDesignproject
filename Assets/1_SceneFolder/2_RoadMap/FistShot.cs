using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class FistShot : MonoBehaviour
{
    public static FistShot Instance;

    [Header("Fist References")]
    public RectTransform leftFist;
    public RectTransform rightFist;

    [Header("Punch Animation Settings")]
    public float punchSpeed = 0.12f;
    public float returnSpeed = 0.2f;
    public float punchScale = 0.5f;   // 타격 시 크기 비율 (기존 크기의 절반)
    public Ease punchEase = Ease.OutQuad;
    public Ease returnEase = Ease.OutQuad;
    
    [Header("Shake Settings")]
    public float shakeDuration = 0.1f;
    public float shakeStrength = 0.2f;

    [Header("SFX Settings")]
    [Tooltip("휘두르는 소리(펀치 시작 시 재생) 목록. 목록 중 하나가 랜덤 재생됩니다.")]
    [SerializeField] private List<AudioClip> swingSFX = new List<AudioClip>();
    [Range(0f, 1f)]
    [SerializeField] private float swingSFXVolume = 1f;

    [Tooltip("맞는 소리(펀치 완료 시 재생) 목록. 목록 중 하나가 랜덤 재생됩니다.")]
    [SerializeField] private List<AudioClip> hitSFX = new List<AudioClip>();
    [Range(0f, 1f)]
    [SerializeField] private float hitSFXVolume = 1f;

    // 전용 재생용 AudioSource (동시 재생 보장)
    private AudioSource localSfxSource;

    private Vector2 leftFistOriginPos;
    private Vector3 leftFistOriginScale;
    private Vector2 rightFistOriginPos;
    private Vector3 rightFistOriginScale;
    
    private bool useLeftFist = true;

    // 프레임 충돌 방지용 플래그
    private bool wantsToPunchMouse = false;
    private Vector2 pendingMousePos;
    private bool handledPriorityPunch = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (leftFist != null) 
        {
            leftFistOriginPos = leftFist.anchoredPosition;
            leftFistOriginScale = leftFist.localScale; // 실제 초기 크기 저장
            Image img = leftFist.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;
        }
        if (rightFist != null) 
        {
            rightFistOriginPos = rightFist.anchoredPosition;
            rightFistOriginScale = rightFist.localScale; // 실제 초기 크기 저장
            Image img = rightFist.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;
        }

        // 전용 AudioSource 생성 — spatialBlend 0으로 UI용(2D) 재생
        localSfxSource = gameObject.AddComponent<AudioSource>();
        localSfxSource.playOnAwake = false;
        localSfxSource.spatialBlend = 0f;
        localSfxSource.loop = false;
    }

    private void Update()
    {
        bool isClicked = false;
        Vector2 mousePos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            isClicked = true;
            mousePos = Mouse.current.position.ReadValue();
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            isClicked = true;
            mousePos = Input.mousePosition;
        }
#endif

        if (isClicked)
        {
            wantsToPunchMouse = true;
            pendingMousePos = mousePos;
        }
    }

    private void LateUpdate()
    {
        if (wantsToPunchMouse)
        {
            if (!handledPriorityPunch)
            {
                PunchToInternal(pendingMousePos, true);
            }
            wantsToPunchMouse = false;
        }
        handledPriorityPunch = false;
    }

    public void PunchTo(Vector3 worldPosition, System.Action onHit = null)
    {
        handledPriorityPunch = true;
        PunchToInternal(worldPosition, false, onHit);
    }

    private void PunchToInternal(Vector3 position, bool isScreenPos, System.Action onHit = null)
    {
        useLeftFist = !useLeftFist;

        RectTransform targetFist = useLeftFist ? leftFist : rightFist;
        Vector2 originPos = useLeftFist ? leftFistOriginPos : rightFistOriginPos;
        Vector3 originScale = useLeftFist ? leftFistOriginScale : rightFistOriginScale;

        if (targetFist == null)
        {
            onHit?.Invoke();
            return;
        }

        Canvas canvas = targetFist.GetComponentInParent<Canvas>();
        Camera uiCam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : Camera.main;

        Vector2 screenPoint = isScreenPos ? (Vector2)position : RectTransformUtility.WorldToScreenPoint(uiCam, position);
        Vector2 localPoint;
        
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(targetFist.parent as RectTransform, screenPoint, uiCam, out localPoint))
        {
            targetFist.DOKill();

            // 휘두르는 소리 재생 (펀치 시작)
            PlayRandomSwingSFX();

            // 1. 타격: 작아지면서 이동 (초기 크기 대비 비율 적용)
            targetFist.DOAnchorPos(localPoint, punchSpeed).SetEase(punchEase);
            targetFist.DOScale(originScale * punchScale, punchSpeed).SetEase(punchEase)
                .OnComplete(() =>
                {
                    // 맞는 소리 재생 (펀치 끝에)
                    PlayRandomHitSFX();

                    onHit?.Invoke();
                    if (Camera.main != null)
                    {
                        Camera.main.transform.DOShakePosition(shakeDuration, shakeStrength);
                    }
                    
                    // 2. 복귀: 정확하게 원래 위치와 저장해둔 '진짜 초기 크기'로 복구
                    targetFist.DOAnchorPos(originPos, returnSpeed).SetEase(returnEase);
                    targetFist.DOScale(originScale, returnSpeed).SetEase(returnEase);
                });
        }
        else
        {
            onHit?.Invoke();
        }
    }

    private float GetGlobalSFXVolume()
    {
        if (OptionsManager.Instance != null) return OptionsManager.Instance.GetSFXVolume();
        return 1f;
    }

    private void PlayRandomSwingSFX()
    {
        if (swingSFX == null || swingSFX.Count == 0 || localSfxSource == null) return;

        int idx = Random.Range(0, swingSFX.Count);
        AudioClip clip = swingSFX[idx];
        if (clip == null) return;

        float global = GetGlobalSFXVolume();
        localSfxSource.PlayOneShot(clip, swingSFXVolume * global);
    }

    private void PlayRandomHitSFX()
    {
        if (hitSFX == null || hitSFX.Count == 0 || localSfxSource == null) return;

        int idx = Random.Range(0, hitSFX.Count);
        AudioClip clip = hitSFX[idx];
        if (clip == null) return;

        float global = GetGlobalSFXVolume();
        localSfxSource.PlayOneShot(clip, hitSFXVolume * global);
    }
}
