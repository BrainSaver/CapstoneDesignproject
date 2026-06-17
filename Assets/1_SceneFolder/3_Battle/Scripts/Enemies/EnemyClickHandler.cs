using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 적 호버 시 하이라이트 효과만 담당한다.
/// 클릭 처리는 BattleInputHandler에서 담당한다.
/// </summary>
public class EnemyClickHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Enemy enemy;
    private Image enemyImage;
    private Color originalColor;

    [Header("호버 설정")]
    [SerializeField] private Color hoverColor = new Color(1f, 0.85f, 0.85f, 1f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.5f, 0.5f, 1f);

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        enemyImage = GetComponent<Image>();
        if (enemyImage != null) originalColor = enemyImage.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (enemyImage == null) return;
        if (CardMovement.SelectedCard == null) return;

        var card = CardMovement.SelectedCard.cardData;

        // Attack/SingleEnemy 카드 선택 시 호버 하이라이트
        if (card.cardType == Card.CardType.Attack ||
            card.targetType == Card.TargetType.SingleEnemy)
        {
            enemyImage.DOColor(selectedColor, 0.15f);
        }

        // AllEnemies 카드 선택 시 전체 하이라이트
        if (card.targetType == Card.TargetType.AllEnemies)
            HighlightAllEnemies(selectedColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (enemyImage == null) return;

        if (CardMovement.SelectedCard?.cardData.targetType == Card.TargetType.AllEnemies)
            ResetAllEnemies();
        else
            enemyImage.DOColor(originalColor, 0.15f);
    }

    /// <summary>모든 적을 하이라이트한다.</summary>
    public void HighlightAllEnemies(Color color)
    {
        foreach (var e in EnemyManager.Instance.GetActiveEnemies())
        {
            if (e == null) continue;
            var img = e.GetComponent<Image>();
            img?.DOColor(color, 0.15f);
        }
    }

    /// <summary>모든 적 색상을 원래대로 복원한다.</summary>
    public void ResetAllEnemies()
    {
        foreach (var e in EnemyManager.Instance.GetActiveEnemies())
        {
            if (e == null) continue;
            var img = e.GetComponent<Image>();
            if (img != null) img.DOColor(originalColor, 0.15f);
        }
    }
}