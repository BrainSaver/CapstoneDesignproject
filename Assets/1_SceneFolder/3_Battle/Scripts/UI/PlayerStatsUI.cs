using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 플레이어 방어도, 에너지, 이동 코스트, 무뎌짐, 노출, 힘, 민첩 UI를 갱신한다.
/// PlayerHUD 오브젝트에 부착한다.
/// </summary>
public class PlayerStatsUI : MonoBehaviour
{
    [Header("방어도 UI")]
    [SerializeField] private Image armorImage;
    [SerializeField] private TextMeshProUGUI armorText;

    [Header("에너지 UI")]
    [SerializeField] private Image energyFillImage;
    [SerializeField] private TextMeshProUGUI energyText;

    [Header("이동 코스트 UI")]
    [SerializeField] private Image movePointFillImage;
    [SerializeField] private TextMeshProUGUI movePointText;

    [Header("무뎌짐 UI")]
    [SerializeField] private GameObject dullnessGroup;
    [SerializeField] private Image dullnessImage;
    [SerializeField] private TextMeshProUGUI dullnessText;

    [Header("노출 UI")]
    [SerializeField] private GameObject exposedGroup;
    [SerializeField] private Image exposedImage;
    [SerializeField] private TextMeshProUGUI exposedText;

    [Header("힘 UI")]
    [SerializeField] private GameObject strengthGroup;       // 힘 0이면 숨길 오브젝트
    [SerializeField] private Image strengthImage;            // 힘 아이콘
    [SerializeField] private TextMeshProUGUI strengthText;   // 힘 수치 텍스트

    [Header("민첩 UI")]
    [SerializeField] private GameObject dexterityGroup;      // 민첩 0이면 숨길 오브젝트
    [SerializeField] private Image dexterityImage;           // 민첩 아이콘
    [SerializeField] private TextMeshProUGUI dexterityText;  // 민첩 수치 텍스트

    private void Start()
    {
        MovePointManager.OnMovePointsChanged += OnMovePointsChanged;
        UpdateUI();
    }

    private void OnDestroy()
    {
        MovePointManager.OnMovePointsChanged -= OnMovePointsChanged;
    }

    private void OnEnable()
    {
        PlayerStats.OnStatsChanged += UpdateUI;
        DullnessTracker.OnChanged += UpdateDullnessUI;
        ExposedTracker.OnChanged += UpdateExposedUI;
    }

    private void OnDisable()
    {
        PlayerStats.OnStatsChanged -= UpdateUI;
        DullnessTracker.OnChanged -= UpdateDullnessUI;
        ExposedTracker.OnChanged -= UpdateExposedUI;
    }

    /// <summary>이동 코스트 변경 시 UI 갱신.</summary>
    private void OnMovePointsChanged(int current, int max)
    {
        if (movePointFillImage != null)
            movePointFillImage.fillAmount = max > 0 ? (float)current / max : 0f;
        if (movePointText != null)
            movePointText.text = $"{current}/{max}";
    }

    /// <summary>플레이어 스탯 UI 전체 갱신.</summary>
    public void UpdateUI()
    {
        if (PlayerStats.Instance == null) { SetFallbackDisplay(); return; }

        // 방어도
        if (armorText != null)
            armorText.text = $"{PlayerStats.Instance.Armor}";

        // 에너지
        if (energyFillImage != null)
            energyFillImage.fillAmount = PlayerStats.Instance.initialEnergy > 0
                ? (float)PlayerStats.Instance.energy / PlayerStats.Instance.initialEnergy
                : 0f;
        if (energyText != null)
            energyText.text = $"{PlayerStats.Instance.energy}/{PlayerStats.Instance.initialEnergy}";

        // 이동 코스트
        if (MovePointManager.Instance != null)
        {
            int cur = MovePointManager.Instance.CurrentPoints;
            int max = MovePointManager.Instance.MaxPoints;
            if (movePointFillImage != null)
                movePointFillImage.fillAmount = max > 0 ? (float)cur / max : 0f;
            if (movePointText != null)
                movePointText.text = $"{cur}/{max}";
        }

        // 무뎌짐/노출 갱신
        UpdateDullnessUI(DullnessTracker.RemainingTurns);
        UpdateExposedUI(ExposedTracker.RemainingTurns);

        // ✅ 힘/민첩 갱신
        UpdateStrengthUI(PlayerStats.Instance.TotalStrength);
        UpdateDexterityUI(PlayerStats.Instance.TotalDexterity);
    }

    /// <summary>무뎌짐 UI 갱신.</summary>
    private void UpdateDullnessUI(int remainingTurns)
    {
        if (dullnessGroup != null)
            dullnessGroup.SetActive(remainingTurns > 0);

        if (dullnessText != null)
            dullnessText.text = remainingTurns.ToString();

        if (remainingTurns > 0 && dullnessImage != null)
        {
            dullnessImage.rectTransform.DOKill();
            dullnessImage.rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 6, 0.8f);
        }

        // 손패 카드 설명 수치 갱신
        if (Application.isPlaying && HandManager.Instance != null)
        {
            foreach (var cardObj in HandManager.Instance.CardsInHand)
                cardObj?.GetComponent<CardDisplay>()?.UpdateCardDisplay();
        }
    }

    /// <summary>노출 UI 갱신.</summary>
    private void UpdateExposedUI(int remainingTurns)
    {
        if (exposedGroup != null)
            exposedGroup.SetActive(remainingTurns > 0);

        if (exposedText != null)
            exposedText.text = remainingTurns.ToString();

        if (remainingTurns > 0 && exposedImage != null)
        {
            exposedImage.rectTransform.DOKill();
            exposedImage.rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 6, 0.8f);
        }

        if (Application.isPlaying && EnemyManager.Instance != null)
        {
            foreach (var enemy in EnemyManager.Instance.GetActiveEnemies())
                enemy?.UpdateIntentDisplay();
        }
    }

    /// <summary>힘 UI 갱신.</summary>
    private void UpdateStrengthUI(int totalStrength)
    {
        if (strengthGroup != null)
            strengthGroup.SetActive(totalStrength != 0); // ✅ 음수 포함 표시

        if (strengthText != null)
            strengthText.text = totalStrength.ToString();

        if (totalStrength != 0 && strengthImage != null)
        {
            strengthImage.rectTransform.DOKill();
            strengthImage.rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 6, 0.8f);
        }

        if (Application.isPlaying && HandManager.Instance != null)
        {
            foreach (var cardObj in HandManager.Instance.CardsInHand)
                cardObj?.GetComponent<CardDisplay>()?.UpdateCardDisplay();
        }
    }

    /// <summary>민첩 UI 갱신.</summary>
    private void UpdateDexterityUI(int totalDexterity)
    {
        if (dexterityGroup != null)
            dexterityGroup.SetActive(totalDexterity != 0); // ✅ 음수 포함 표시

        if (dexterityText != null)
            dexterityText.text = totalDexterity.ToString();

        if (totalDexterity != 0 && dexterityImage != null)
        {
            dexterityImage.rectTransform.DOKill();
            dexterityImage.rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 6, 0.8f);
        }

        if (Application.isPlaying && HandManager.Instance != null)
        {
            foreach (var cardObj in HandManager.Instance.CardsInHand)
                cardObj?.GetComponent<CardDisplay>()?.UpdateCardDisplay();
        }
    }

    /// <summary>기본값 표시.</summary>
    private void SetFallbackDisplay()
    {
        if (armorText != null) armorText.text = "--";
        if (energyText != null) energyText.text = "--";
        if (movePointText != null) movePointText.text = "--";
        if (energyFillImage != null) energyFillImage.fillAmount = 0f;
        if (movePointFillImage != null) movePointFillImage.fillAmount = 0f;
        if (dullnessGroup != null) dullnessGroup.SetActive(false);
        if (exposedGroup != null) exposedGroup.SetActive(false);
        if (strengthGroup != null) strengthGroup.SetActive(false);
        if (dexterityGroup != null) dexterityGroup.SetActive(false);
    }

    /// <summary>방어도 획득 시 애니메이션.</summary>
    public void ShowArmorGainEffect()
    {
        if (armorImage == null) return;
        armorImage.rectTransform.DOKill();
        armorImage.rectTransform.DOPunchScale(Vector3.one * 0.25f, 0.3f, 8, 1.0f);
        if (armorText != null)
        {
            armorText.rectTransform.DOKill();
            armorText.rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 6, 0.8f);
            Color originalColor = armorText.color;
            armorText.DOColor(Color.cyan, 0.1f)
                     .SetLoops(2, LoopType.Yoyo)
                     .OnComplete(() => armorText.color = originalColor);
        }
    }
}