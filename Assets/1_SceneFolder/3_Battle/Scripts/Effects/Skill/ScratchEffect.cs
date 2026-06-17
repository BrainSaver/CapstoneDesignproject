using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 적 피격 시 스크래치 VFX를 재생하는 컴포넌트.
/// DamageEffect/AOEDamageEffect에서 인스턴스를 생성해 호출한다.
/// </summary>
public class ScratchEffect : MonoBehaviour
{
    [Header("애니메이션 설정")]
    [SerializeField] private float duration = 0.4f;  // 이펙트 전체 재생 시간
    [SerializeField] private float peakScale = 1.3f;  // 최대 확대 배율
    [SerializeField] private float startAlpha = 1f;    // 시작 알파값

    private Image image;
    private CanvasGroup canvasGroup;
    public string cardDescription;
    private void Awake()
    {
        image = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();

        // CanvasGroup이 없으면 자동 추가
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>스크래치 이펙트를 재생하고 완료 시 오브젝트를 파괴한다.</summary>
    public void PlayEffect()
    {
        if (image != null)
        {
            // 랜덤 회전으로 매번 다르게 보이도록
            transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        }

        canvasGroup.alpha = startAlpha;
        transform.localScale = Vector3.zero;

        // 확대 후 페이드 아웃
        DOTween.Sequence()
            .Append(transform.DOScale(peakScale, duration * 0.3f).SetEase(Ease.OutBack))
            .Append(transform.DOScale(peakScale * 0.8f, duration * 0.4f).SetEase(Ease.InOutQuad))
            .Join(canvasGroup.DOFade(0f, duration * 0.4f).SetEase(Ease.InQuad))
            .OnComplete(() => Destroy(gameObject));
    }
}