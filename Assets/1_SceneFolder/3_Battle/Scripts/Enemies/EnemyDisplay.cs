using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 적의 스프라이트, HP바, 인텐트, 데미지 팝업, 상태 비주얼을 관리하는 UI 컴포넌트.
/// </summary>
public class EnemyDisplay : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public Image enemyImage;
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private IntentDisplay intentDisplay;
    [SerializeField] private Transform textSpawnAnchor;
    [SerializeField] private GameObject floatingDamageTextPrefab;
    [SerializeField] private Image enragedImage;
    [SerializeField] private Image awakenedImage;
    [SerializeField] private TextMeshProUGUI armorText;
    [SerializeField] private GameObject armorGroup;

    [Header("무뎌짐 UI")]
    [SerializeField] private GameObject dullnessGroup;      // 무뎌짐 0이면 숨길 오브젝트
    [SerializeField] private Image dullnessIcon;            // 무뎌짐 아이콘
    [SerializeField] private TextMeshProUGUI dullnessText;  // 무뎌짐 남은 턴

    [Header("노출 UI")]
    [SerializeField] private GameObject exposedGroup;       // 노출 0이면 숨길 오브젝트
    [SerializeField] private Image exposedIcon;             // 노출 아이콘
    [SerializeField] private TextMeshProUGUI exposedText;   // 노출 남은 턴

    [Header("힘 UI")]
    [SerializeField] private GameObject strengthGroup;      // 힘 0이면 숨길 오브젝트
    [SerializeField] private Image strengthIcon;            // 힘 아이콘
    [SerializeField] private TextMeshProUGUI strengthText;  // 힘 수치

    private RectTransform enemyRect;

    // ── 초기화 ───────────────────────────────────────────────────

    public void Setup(Enemy enemy, EnemyData enemyData)
    {
        if (enemyImage == null)
        {
            Debug.LogError("[EnemyDisplay] enemyImage가 연결되지 않았습니다.", this);
            return;
        }

        enemyImage.sprite = enemyData.enemySprite;
        enemyImage.type = Image.Type.Simple;
        enemyImage.preserveAspect = true;

        if (enragedImage != null) { var c = enragedImage.color; c.a = 0f; enragedImage.color = c; }
        if (awakenedImage != null) { var c = awakenedImage.color; c.a = 0f; awakenedImage.color = c; }

        enemyRect = GetComponent<RectTransform>();
        if (enemyRect != null)
            enemyRect.sizeDelta = enemyData.size;

        if (enemy != null)
            UpdateDisplay(enemy.currentHP, enemy.maxHP);

        UpdateArmorDisplay(0);

        // 초기화 시 무뎌짐/노출 숨김
        UpdateDullnessDisplay(0);
        UpdateExposedDisplay(0);
        UpdateStrengthDisplay(0);
    }

    // ── HP 표시 ──────────────────────────────────────────────────

    public void UpdateDisplay(int currentHealth, int maxHealth)
    {
        if (healthBar == null) return;
        healthBar.UpdateHealthBar(currentHealth, maxHealth);
    }

    /// <summary>방어도 UI를 갱신한다.</summary>
    public void UpdateArmorDisplay(int armor)
    {
        if (armorText != null)
            armorText.text = armor.ToString();

        if (armorGroup != null)
            armorGroup.SetActive(armor > 0);
    }

    // ── 상태이상 UI ──────────────────────────────────────────────

    /// <summary>무뎌짐 UI를 갱신한다.</summary>
    public void UpdateDullnessDisplay(int remainingTurns)
    {
        if (dullnessGroup != null)
            dullnessGroup.SetActive(remainingTurns > 0);

        if (dullnessText != null)
            dullnessText.text = remainingTurns.ToString();

        // 새로 적용될 때 펀치 애니메이션
        if (remainingTurns > 0 && dullnessIcon != null)
        {
            dullnessIcon.rectTransform.DOKill();
            dullnessIcon.rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 6, 0.8f);
        }
    }

    /// <summary>노출 UI를 갱신한다.</summary>
    public void UpdateExposedDisplay(int remainingTurns)
    {
        if (exposedGroup != null)
            exposedGroup.SetActive(remainingTurns > 0);

        if (exposedText != null)
            exposedText.text = remainingTurns.ToString();

        // 새로 적용될 때 펀치 애니메이션
        if (remainingTurns > 0 && exposedIcon != null)
        {
            exposedIcon.rectTransform.DOKill();
            exposedIcon.rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 6, 0.8f);
        }
    }

    /// <summary>힘 UI를 갱신한다.</summary>
    public void UpdateStrengthDisplay(int strength)
    {
        Debug.Log($"[EnemyDisplay] UpdateStrengthDisplay 호출. strength={strength}, strengthGroup={strengthGroup != null}");

        if (strengthGroup != null)
            strengthGroup.SetActive(strength != 0);

        if (strengthText != null)
            strengthText.text = strength.ToString();

        if (strength != 0 && strengthIcon != null)
        {
            strengthIcon.rectTransform.DOKill();
            strengthIcon.rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 6, 0.8f);
        }
    }

    // ── 인텐트 표시 ──────────────────────────────────────────────

    public void SetIntent(EnemyIntent intent)
    {
        if (intentDisplay != null) intentDisplay.SetIntent(intent);
    }

    public void ClearIntentDisplay()
    {
        if (intentDisplay != null) intentDisplay.ClearIntent();
    }

    // ── 상태 비주얼 ──────────────────────────────────────────────

    public void SetEnragedVisual(bool isEnraged)
    {
        if (enragedImage != null)
        {
            enragedImage.DOKill();
            enragedImage.DOFade(isEnraged ? 1f : 0f, 0.2f);
            if (enemyImage != null) enemyImage.color = Color.white;
        }
        else if (enemyImage != null)
        {
            enemyImage.color = isEnraged ? new Color(1f, 0.18f, 0.23f, 1f) : Color.white;
        }
    }

    public void SetAwakenVisual(bool on)
    {
        if (awakenedImage == null) return;

        awakenedImage.DOKill();
        awakenedImage.DOFade(on ? 1f : 0f, on ? 1.2f : 0.25f);

        if (on && enemyImage != null)
            enemyImage.rectTransform.DOPunchScale(Vector3.one * 0.12f, 0.28f, 6, 0.6f);
    }

    // ── 팝업 ─────────────────────────────────────────────────────

    public void ShowDamagePopup(int damage)
    {
        if (floatingDamageTextPrefab == null || textSpawnAnchor == null) return;

        GameObject go = Instantiate(floatingDamageTextPrefab, textSpawnAnchor);
        RectTransform rect = go.GetComponent<RectTransform>();
        CanvasGroup group = go.GetComponent<CanvasGroup>();
        TMP_Text text = go.GetComponentInChildren<TMP_Text>();

        if (text != null) { text.text = damage.ToString(); text.color = Color.white; }

        rect.localScale = Vector3.one * 1.6f;

        DOTween.Sequence()
            .Append(rect.DOShakeScale(0.1f, 0.2f, 10))
            .Append(rect.DOScale(0.8f, 0.6f).SetEase(Ease.InOutQuad))
            .Join(rect.DOAnchorPosY(rect.anchoredPosition.y + 80f, 0.6f).SetEase(Ease.OutCubic))
            .Join(group != null ? group.DOFade(0f, 0.6f) : null)
            .OnComplete(() => Destroy(go));
    }

    public void ShowHealPopup(int amount)
    {
        if (floatingDamageTextPrefab == null || textSpawnAnchor == null) return;

        GameObject go = Instantiate(floatingDamageTextPrefab, textSpawnAnchor);
        RectTransform rect = go.GetComponent<RectTransform>();
        CanvasGroup group = go.GetComponent<CanvasGroup>();
        TMP_Text text = go.GetComponentInChildren<TMP_Text>();

        if (text != null) { text.text = $"+{amount}"; text.color = Color.green; }

        rect.localScale = Vector3.one * 1.3f;

        DOTween.Sequence()
            .Append(rect.DOScale(1.0f, 0.2f))
            .Append(rect.DOAnchorPosY(rect.anchoredPosition.y + 70f, 0.6f).SetEase(Ease.OutCubic))
            .Join(group != null ? group.DOFade(0f, 0.6f) : null)
            .OnComplete(() => Destroy(go));
    }

    // ── 사망 ─────────────────────────────────────────────────────

    public void PlayDeathAnimation(System.Action onComplete = null)
    {
        if (enemyImage == null) { onComplete?.Invoke(); return; }

        enemyImage.DOKill();
        enragedImage?.DOKill();
        awakenedImage?.DOKill();

        var seq = DOTween.Sequence()
            .Join(enemyImage.DOFade(0f, 1f).SetEase(Ease.InOutQuad));

        if (enragedImage != null) seq.Join(enragedImage.DOFade(0f, 1f).SetEase(Ease.InOutQuad));
        if (awakenedImage != null) seq.Join(awakenedImage.DOFade(0f, 1f).SetEase(Ease.InOutQuad));

        seq.OnComplete(() => { onComplete?.Invoke(); gameObject.SetActive(false); });
    }

    public void ShowStunEffect(bool isStunned)
    {
        if (enemyImage != null)
            enemyImage.color = isStunned ? new Color(0.5f, 0.7f, 1f, 1f) : Color.white;
    }
}