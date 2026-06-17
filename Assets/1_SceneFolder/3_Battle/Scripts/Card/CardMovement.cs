using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Threading.Tasks;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardMovement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private RectTransform rectTransform;
    private Vector3 originalPosition;  // 레이아웃 기준 원본 위치 (절대 변경 X)
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private int originalSiblingIndex;

    [Header("손패 상태")]
    public bool isInHand = true;

    [Header("선택 비주얼")]
    [SerializeField] private float hoverScaleMultiplier = 1.1f;
    [SerializeField] private float selectedLiftY = 40f;
    [SerializeField] private GameObject glowEffect;

    [Header("보존 상태")]
    public bool isRetained = false; // 턴 종료 시 버려지지 않음

    public Card cardData;

    public static CardMovement SelectedCard { get; private set; }

    private bool isSelected = false;
    private bool isHovered = false;


    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        var img = GetComponent<Image>();
        if (img != null && !img.raycastTarget)
            img.raycastTarget = true;
    }

    private void Start()
    {
        if (cardData == null)
            cardData = GetComponent<CardDisplay>()?.cardData;

        SaveOriginalTransform();

        if (glowEffect != null)
            glowEffect.SetActive(false);
    }

    private void OnDestroy()
    {
        if (SelectedCard == this) SelectedCard = null;
        DOTween.Kill(gameObject, complete: true);
    }

    /// <summary>
    /// HandManager 레이아웃 갱신 후 호출.
    /// originalPosition은 여기서만 갱신한다.
    /// </summary>
    public void SaveOriginalTransform()
    {
        if (rectTransform == null) return; // ✅ null 체크 추가
        if (isHovered || isSelected) return;

        originalPosition = rectTransform.localPosition;
        originalRotation = rectTransform.localRotation;
        originalScale = rectTransform.localScale;
    }

    // ── 포인터 이벤트 ────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Blocked() || isSelected) return;
        if (isHovered) return;

        isHovered = true;
        originalSiblingIndex = transform.GetSiblingIndex();
        transform.SetAsLastSibling();

        DOTween.Kill(gameObject);
        rectTransform.DOScale(originalScale * hoverScaleMultiplier, 0.15f).SetEase(Ease.OutQuad);
        rectTransform.DOLocalMoveY(originalPosition.y + selectedLiftY, 0.15f).SetEase(Ease.OutQuad);
        rectTransform.DOLocalRotateQuaternion(Quaternion.identity, 0.15f);

        AudioManager.Instance?.PlaySFX("Card_Hover");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isSelected) return;
        if (!isHovered) return;

        isHovered = false;

        DOTween.Kill(gameObject);
        rectTransform.DOScale(originalScale, 0.15f).SetEase(Ease.OutQuad);
        rectTransform.DOLocalMove(originalPosition, 0.15f).SetEase(Ease.OutQuad);
        rectTransform.DOLocalRotateQuaternion(originalRotation, 0.15f).SetEase(Ease.OutQuad);

        transform.SetSiblingIndex(originalSiblingIndex);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Blocked()) return;

        if (isSelected)
        {
            Deselect();
        }
        else
        {
            if (SelectedCard != null && SelectedCard != this)
                SelectedCard.Deselect();

            Select();
        }
    }

    // ── 선택/해제 ────────────────────────────────────────────────

    public void Select()
    {
        if (isSelected) return;

        isSelected = true;
        isHovered = false;
        SelectedCard = this;

        AudioManager.Instance?.PlaySFX("Card_Select");

        DOTween.Kill(gameObject);
        rectTransform.DOScale(originalScale * hoverScaleMultiplier, 0.2f).SetEase(Ease.OutBack);
        rectTransform.DOLocalMoveY(originalPosition.y + selectedLiftY, 0.2f).SetEase(Ease.OutBack);
        rectTransform.DOLocalRotateQuaternion(Quaternion.identity, 0.2f);

        if (glowEffect != null)
        {
            glowEffect.SetActive(true);
            var gi = glowEffect.GetComponent<Image>();
            if (gi != null)
            {
                gi.color = GetColorByCardType();
                gi.DOFade(0.8f, 0.2f);
            }
        }

        if (cardData.cardType == Card.CardType.Attack &&
            !cardData.IsUsableAtCurrentDistance())
        {
            ShowOutOfRangeFeedback();
        }
    }

    public void Deselect()
    {
        if (!isSelected) return;

        isSelected = false;
        isHovered = false;
        if (SelectedCard == this) SelectedCard = null;

        if (glowEffect != null)
        {
            var gi = glowEffect.GetComponent<Image>();
            if (gi != null)
                gi.DOFade(0f, 0.15f).OnComplete(() => glowEffect.SetActive(false));
        }

        DOTween.Kill(gameObject);
        rectTransform.DOScale(originalScale, 0.2f).SetEase(Ease.OutQuad);
        rectTransform.DOLocalMove(originalPosition, 0.2f).SetEase(Ease.OutQuad);
        rectTransform.DOLocalRotateQuaternion(originalRotation, 0.2f).SetEase(Ease.OutQuad);

        transform.SetSiblingIndex(originalSiblingIndex);
    }

    // ── 카드 사용 ────────────────────────────────────────────────

    public void UseCardOnTarget(Enemy target)
    {
        Logger.Log($"[CardMovement] UseCardOnTarget 호출. target={target?.enemyName ?? "null"}");

        if (!isSelected || cardData == null) return;

        if (cardData.cardType == Card.CardType.Curse)
        {
            Logger.LogWarning($"[제한] 저주카드 '{cardData.cardName}'은(는) 직접 플레이할 수 없습니다.");
            ShakeCard();
            Deselect();
            return;
        }

        if (!PlayerManager.Instance.CanPlayCard(cardData))
        {
            Logger.Log($"[CardMovement] '{cardData.cardName}' 에너지 부족.");
            ShakeCard();
            return;
        }

        if (cardData.cardType == Card.CardType.Attack &&
            !cardData.IsUsableAtCurrentDistance())
        {
            Logger.Log($"[CardMovement] '{cardData.cardName}' 사거리 밖.");
            ShakeCard();
            return;
        }

        StartCoroutine(ApplyEffectsInSequence(target));
    }

    public void UseCardNoTarget()
    {
        if (!isSelected || cardData == null) return;

        if (cardData.cardType == Card.CardType.Curse)
        {
            Logger.LogWarning($"[제한] 저주카드 '{cardData.cardName}'은(는) 직접 플레이할 수 없습니다.");
            ShakeCard();
            Deselect();
            return;
        }

        if (!PlayerManager.Instance.CanPlayCard(cardData))
        {
            Logger.Log($"[CardMovement] '{cardData.cardName}' 에너지 부족.");
            ShakeCard();
            return;
        }

        StartCoroutine(ApplyEffectsInSequence(null));
    }

    // ── 이펙트 적용 ──────────────────────────────────────────────

    private System.Collections.IEnumerator ApplyEffectsInSequence(Enemy target)
    {
        SelectedCard = null;
        isSelected = false;
        isHovered = false;

        // ★ [더블 래리어트용 에너지 백업]: PlayerStats의 소문자 energy 참조
        int currentRemainingEnergy = (PlayerStats.Instance != null) ? PlayerStats.Instance.energy : 0;

        float relicMultiplier = RelicManager.Instance != null
            ? RelicManager.Instance.OnGetDamageMultiplier(cardData, true) : 1f;

        PlayerManager.Instance.UseCard(cardData);
        RelicManager.Instance?.OnCardPlayed(cardData);
        HandManager.Instance.RemoveCardFromHand(gameObject, destroyGO: false);
        isInHand = false;

        if (glowEffect != null)
        {
            var gi = glowEffect.GetComponent<Image>();
            if (gi != null) DOTween.Kill(gi, complete: true);
            glowEffect.SetActive(false);
        }

        DOTween.Kill(gameObject, complete: true);

        var selfImg = GetComponent<Image>();
        if (selfImg != null) selfImg.raycastTarget = false;

        var rt = GetComponent<RectTransform>();
        if (rt != null) rt.localScale = Vector3.zero;

        foreach (var gfx in GetComponentsInChildren<Graphic>(true))
            gfx.enabled = false;

        // 실제 효과가 발동되는 부분
        foreach (CardEffectInfo effect in cardData.GetCardEffects())
        {
            int repeat = Mathf.Max(1, effect.hitCount);
            int calculatedAmount = cardData.GetCalculatedAmount(effect);

            // ★ 더블 래리어트 전용 연타 횟수 변조
            if (effect.effectType == CardEffectType.RandomXHitsDamage)
            {
                repeat = currentRemainingEnergy;
                Logger.Log($"[더블 래리어트] 연타 횟수가 현재 소모 에너지 수치인 {repeat}회로 설정되었습니다.");
            }

            for (int i = 0; i < repeat; i++)
            {
                switch (effect.effectType)
                {
                    case CardEffectType.Damage:
                        if (target != null)
                        {
                            int dmg = Mathf.RoundToInt(
                                (calculatedAmount + PlayerStats.Instance.TotalStrength) * relicMultiplier);
                            dmg += RevengeTracker.ConsumeRevengeDamage();
                            dmg *= DoubleAttackTracker.ConsumeMultiplier();
                            dmg = DullnessTracker.ApplyToDamage(dmg);
                            dmg = Mathf.Max(0, dmg); // 음수 데미지 방지

                            target.TakeDamage(dmg);
                            GameSession.Instance?.AddDamageDealt(dmg);
                            target.enemyDisplay?.ShowDamagePopup(dmg);
                            AudioManager.Instance?.PlaySFX("Enemy_Hit");
                            ShowScratchEffect(target);
                        }
                        break;

                    // ★ 더블 래리어트 무작위 연타 공격 실행부
                    case CardEffectType.RandomXHitsDamage:
                        if (EnemyManager.Instance != null)
                        {
                            var activeEnemies = EnemyManager.Instance.GetActiveEnemies();
                            if (activeEnemies == null || activeEnemies.Count == 0) break;

                            int randomIndex = UnityEngine.Random.Range(0, activeEnemies.Count);
                            Enemy randomTarget = activeEnemies[randomIndex];

                            if (randomTarget != null)
                            {
                                int dmg = Mathf.RoundToInt((calculatedAmount + PlayerStats.Instance.TotalStrength) * relicMultiplier);
                                dmg = DullnessTracker.ApplyToDamage(dmg);
                                dmg = Mathf.Max(0, dmg);

                                randomTarget.TakeDamage(dmg);
                                GameSession.Instance?.AddDamageDealt(dmg);
                                randomTarget.enemyDisplay?.ShowDamagePopup(dmg);
                                AudioManager.Instance?.PlaySFX("Enemy_Hit");
                                ShowScratchEffect(randomTarget);

                                Logger.Log($"[래리어트 연타] 무작위 대상 '{randomTarget.enemyName}'에게 {dmg} 피해 가함 ({i + 1}/{repeat})");
                            }
                        }
                        yield return new WaitForSeconds(0.12f); // 연타 감각용 딜레이
                        break;

                    case CardEffectType.AOEDamage:
                        var activeEnemiesList = EnemyManager.Instance?.GetActiveEnemies();
                        if (activeEnemiesList != null)
                        {
                            foreach (var e in activeEnemiesList)
                            {
                                if (e == null) continue;
                                int dmg = Mathf.RoundToInt(
                                    (calculatedAmount + PlayerStats.Instance.TotalStrength) * relicMultiplier);
                                dmg = DullnessTracker.ApplyToDamage(dmg);
                                dmg = Mathf.Max(0, dmg);

                                e.TakeDamage(dmg);
                                GameSession.Instance?.AddDamageDealt(dmg);
                                e.enemyDisplay?.ShowDamagePopup(dmg);
                                ShowScratchEffect(e);
                            }
                            AudioManager.Instance?.PlaySFX("Enemy_Hit");
                        }
                        break;

                    case CardEffectType.Armor:
                        PlayerStats.Instance?.AddArmor(calculatedAmount);
                        AudioManager.Instance?.PlaySFX("Block_Gain");
                        FindAnyObjectByType<PlayerStatsUI>()?.ShowArmorGainEffect();
                        break;

                    case CardEffectType.Heal:
                        if (effect.targetType == Card.TargetType.Self)
                            PlayerDataManager.Instance?.ModifyHP(effect.amount);
                        else
                            target?.HealHP(effect.amount);
                        break;

                    case CardEffectType.DrawCard:
                        yield return HandManager.Instance.DrawCardsRoutine(effect.amount);
                        break;

                    case CardEffectType.GainEnergy:
                        PlayerStats.Instance?.GainEnergy(effect.amount);
                        break;

                    case CardEffectType.LoseHealth:
                        if (effect.targetType == Card.TargetType.Self)
                            PlayerStats.Instance?.LoseHealthDirect(effect.amount);
                        else
                            target?.LoseHealthDirect(effect.amount);
                        break;

                    case CardEffectType.SetHealth:
                        if (effect.targetType == Card.TargetType.Self)
                            PlayerStats.Instance?.SetCurrentHealth(effect.amount);
                        else if (target != null)
                        {
                            target.currentHP = Mathf.Clamp(effect.amount, 0, target.maxHP);
                            target.enemyDisplay?.UpdateDisplay(target.currentHP, target.maxHP);
                        }
                        break;

                    case CardEffectType.ApplyBleed:
                        target?.AddBleed(effect.amount);
                        break;

                    case CardEffectType.ApplyDullnessToEnemy:
                        if (effect.applyToAll)
                        {
                            foreach (var e in EnemyManager.Instance.GetActiveEnemies())
                            {
                                e?.ApplyDullness(effect.duration);
                                e?.UpdateIntentDisplay(); // ✅ 추가
                            }
                        }
                        else
                        {
                            target?.ApplyDullness(effect.duration);
                            target?.UpdateIntentDisplay(); // ✅ 추가
                        }
                        break;

                    case CardEffectType.ApplyExposedToEnemy:
                        if (effect.applyToAll)
                        {
                            foreach (var e in EnemyManager.Instance.GetActiveEnemies())
                                e?.ApplyExposed(effect.duration);
                        }
                        else
                            target?.ApplyExposed(effect.duration);
                        break;

                    case CardEffectType.GainStrength:
                        PlayerStats.Instance?.AddStrength(effect.amount);
                        Logger.Log($"[CardMovement] 플레이어 힘 +{effect.amount}");
                        break;

                    case CardEffectType.GainDexterity:
                        PlayerStats.Instance?.AddDexterity(effect.amount);
                        Logger.Log($"[CardMovement] 플레이어 민첩 +{effect.amount}");
                        break;

                    case CardEffectType.ApplyStrengthToEnemy:
                        if (effect.applyToAll)
                        {
                            foreach (var e in EnemyManager.Instance.GetActiveEnemies())
                            {
                                if (e == null) continue;
                                e.AddStrength(effect.amount); // ✅ AddStrength 내부에서 UI+인텐트 갱신
                            }
                        }
                        else
                        {
                            target?.AddStrength(effect.amount);
                        }
                        break;

                    case CardEffectType.DamageEqualArmor:
                        if (target != null && PlayerStats.Instance != null)
                        {
                            int myArmor = PlayerStats.Instance.Armor;
                            target.TakeDamage(myArmor);
                            target.enemyDisplay?.ShowDamagePopup(myArmor);
                            ShowScratchEffect(target);
                        }
                        break;

                    case CardEffectType.IgnoreArmorDamage:
                        target?.LoseHealthDirect(effect.amount);
                        break;

                    case CardEffectType.AntiArmorDamage:
                        target?.TakeAntiArmorDamage(effect.amount);
                        break;

                    case CardEffectType.DoubleBleed:
                        target?.DoubleBleedStacks();
                        break;

                    case CardEffectType.ConditionalDraw:
                        int drawCount = GetConditionalDrawCount(target, effect);
                        yield return HandManager.Instance.DrawCardsRoutine(drawCount);
                        break;

                    case CardEffectType.CopyEnemyArmor:
                        if (target != null) PlayerStats.Instance?.AddArmor(target.Armor);
                        break;

                    case CardEffectType.DamageReduction:
                        DamageReductionTracker.ApplyReduction(effect.floatValue, effect.duration);
                        break;

                    // ★ [수정완료] 지연 대미지(DelayedDamage) 타겟 판별 및 안전 예약 분기
                    case CardEffectType.DelayedDamage:
                        var coroutineRunner = (MonoBehaviour)BattleManager.Instance ?? TurnManager.Instance;
                        if (coroutineRunner != null)
                        {
                            // 전체 타겟이거나 인스펙터 일괄 적용 체크 시
                            if (effect.applyToAll || cardData.targetType == Card.TargetType.AllEnemies)
                            {
                                var allEnemies = EnemyManager.Instance?.GetActiveEnemies();
                                if (allEnemies != null && allEnemies.Count > 0)
                                {
                                    foreach (var e in allEnemies)
                                    {
                                        if (e == null) continue;
                                        coroutineRunner.StartCoroutine(DelayedDamageRoutine(e, calculatedAmount, effect.duration));
                                        Logger.Log($"[DelayedDamage] (전체) {e.enemyName}에게 {effect.duration}턴 뒤 {calculatedAmount} 대미지 예약 완료.");
                                    }
                                }
                            }
                            // 단일 타겟일 때
                            else if (target != null)
                            {
                                coroutineRunner.StartCoroutine(DelayedDamageRoutine(target, calculatedAmount, effect.duration));
                                Logger.Log($"[DelayedDamage] (단일) {target.enemyName}에게 {effect.duration}턴 뒤 {calculatedAmount} 대미지 예약 완료.");
                            }
                        }
                        break;

                    case CardEffectType.DiscardRandomCard:
                        yield return DiscardRandomCardRoutine();
                        break;

                    case CardEffectType.DoubleNextAttack:
                        DoubleAttackTracker.Activate();
                        break;

                    case CardEffectType.EqualizeHealth:
                        ApplyEqualizeHealth(target);
                        break;

                    case CardEffectType.GainCardNextTurn:
                        if (effect.effectCard != null) GainCardNextTurn(effect.effectCard);
                        break;

                    case CardEffectType.GainRandomSkill:
                        GainRandomSkill(effect.cardPool);
                        break;

                    case CardEffectType.Immobilize:
                        ImmobilizeTracker.Apply(effect.duration);
                        break;

                    case CardEffectType.MoveBonus:
                        MoveBonusTracker.Instance?.AddBonus(effect.amount);
                        break;

                    case CardEffectType.Move:
                        ApplyMoveEffect(effect.amount);
                        break;

                    case CardEffectType.PerfectBlockBonus:
                        PerfectBlockTracker.OnPerfectBlock +=
                            () => PerfectBlockTracker.AddFreeCardsNextTurn(effect.amount);
                        break;

                    case CardEffectType.RecoverAllAndFillHand:
                        yield return RecoverAllAndFillHandRoutine();
                        break;

                    case CardEffectType.RecoverExhausted:
                        ExhaustPile.RecoverCards(effect.amount);
                        break;

                    case CardEffectType.ReduceCost:
                        ApplyReduceCost(effect.amount, effect.applyToAll);
                        break;

                    case CardEffectType.RetainHand:
                        RetainHandTracker.Activate();
                        break;

                    case CardEffectType.Revenge:
                        RevengeTracker.ActivateRevenge();
                        break;

                    case CardEffectType.DemonsMuscle:
                        PlayerStats.Instance?.GainEnergy(1);
                        BossStrengthTracker.AddBonus(effect.amount);
                        Logger.Log($"[CardMovement] 악마의 근육 발동. 보스 공격력 +{effect.amount}");

                        // ✅ 적 인텐트 + 힘 UI 즉시 갱신
                        if (EnemyManager.Instance != null)
                        {
                            foreach (var e in EnemyManager.Instance.GetActiveEnemies())
                            {
                                if (e == null) continue;
                                e.UpdateIntentDisplay();
                                e.enemyDisplay?.UpdateStrengthDisplay(e.strength + BossStrengthTracker.CurrentBonus);
                            }
                        }
                        break;

                    case CardEffectType.DemonsDust:
                        Logger.Log($"[CardMovement] 악마의 티끌 사용.");
                        RegisterDemonsDustOnDiscard(cardData);

                        // 적 인텐트 + 힘 UI 즉시 갱신
                        if (EnemyManager.Instance != null)
                        {
                            foreach (var e in EnemyManager.Instance.GetActiveEnemies())
                            {
                                if (e == null) continue;
                                e.UpdateIntentDisplay();
                                e.enemyDisplay?.UpdateStrengthDisplay(e.strength + BossStrengthTracker.CurrentBonus);
                            }
                        }
                        break;

                    default:
                        Debug.Log($"[CardMovement] 발동 로직이 아직 작성되지 않은 효과입니다: {effect.effectType}");
                        break;
                }
            }

            // ★ X코스트 더블 래리어트 에너지 최종 차감 정산
            if (effect.effectType == CardEffectType.RandomXHitsDamage && PlayerStats.Instance != null)
            {
                PlayerStats.Instance.UseEnergy(currentRemainingEnergy);
                Logger.Log($"[더블 래리어트] 연타 완료로 플레이어 에너지 {currentRemainingEnergy} 소모.");
            }
        }

        if (cardData.moveAmount != 0)
        {
            DistanceManager.Instance?.Move(cardData.moveAmount);
        }

        yield return null;
    }

    // ── 보조 메서드 ──────────────────────────────────────────────

    private void ShowScratchEffect(Enemy target)
    {
        GameObject scratchPrefab = Resources.Load<GameObject>("Effects/ScratchEffect");
        if (scratchPrefab != null && target?.enemyDisplay?.enemyImage != null)
        {
            GameObject instance = Instantiate(
                scratchPrefab, target.enemyDisplay.enemyImage.transform);
            instance.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            instance.GetComponent<ScratchEffect>()?.PlayEffect();
        }
    }

    private int GetConditionalDrawCount(Enemy target, CardEffectInfo effect)
    {
        int playerHP = PlayerDataManager.Instance.currentHP;
        int enemyHP = target != null ? target.currentHP : 0;
        if (target == null)
        {
            var enemies = EnemyManager.Instance?.GetActiveEnemies();
            foreach (var e in enemies)
                if (e != null) enemyHP = Mathf.Max(enemyHP, e.currentHP);
        }
        return playerHP < enemyHP ? effect.amount : effect.duration;
    }

    // ★ [수정완료] 외부 매니저 위임형 프레임 대기식 지연 대미지 핵심 코루틴
    private System.Collections.IEnumerator DelayedDamageRoutine(Enemy target, int damage, int delay)
    {
        int turnsRemaining = delay;

        // 적 오브젝트가 실시간으로 살아있는 동안에만 카운트 다운을 추적합니다.
        while (turnsRemaining > 0 && target != null && target.gameObject != null && target.currentHP > 0)
        {
            bool nextTurnReached = false;

            System.Action handleTurn = null;
            handleTurn = () => { nextTurnReached = true; };

            TurnManager.Instance.OnPlayerTurnStart += handleTurn;

            // 이벤트가 발행되어 트루 신호를 가로챌 때까지 프레임 단위 유휴 대기 (안정성 확보)
            while (!nextTurnReached)
            {
                yield return null;
            }

            if (TurnManager.Instance != null)
                TurnManager.Instance.OnPlayerTurnStart -= handleTurn;

            turnsRemaining--;
            Logger.Log($"[DelayedDamage] 턴 경과함. 남은 턴: {turnsRemaining} (대상: {target.enemyName})");
        }

        // 대기 만료 후 최종 뚝배기 파괴 연산
        if (target != null && target.gameObject != null && target.currentHP > 0)
        {
            Logger.Log($"[DelayedDamage] ★너는 이미 죽어있다 발동★ {target.enemyName}에게 {damage} 지연 피해 작렬!");

            target.TakeDamage(damage);
            target.enemyDisplay?.ShowDamagePopup(damage);
            GameSession.Instance?.AddDamageDealt(damage);

            ShowScratchEffect(target);
        }
        else
        {
            Logger.Log("[DelayedDamage] 타이머가 만료되었으나 대상이 이미 조기 사망하여 불발 처리.");
        }
    }

    private System.Collections.IEnumerator DiscardRandomCardRoutine()
    {
        var hand = HandManager.Instance;
        if (hand.CardsInHand.Count > 0)
        {
            yield return new WaitForSeconds(0.2f);
            int idx = UnityEngine.Random.Range(0, hand.CardsInHand.Count);
            yield return hand.AnimateDiscardAndRemoveCard(hand.CardsInHand[idx]);
        }
    }

    private void ApplyEqualizeHealth(Enemy target)
    {
        if (target != null && PlayerDataManager.Instance != null)
        {
            float playerRatio =
                (float)PlayerDataManager.Instance.currentHP / PlayerDataManager.Instance.maxHP;
            target.currentHP = Mathf.RoundToInt(target.maxHP * playerRatio);
            target.enemyDisplay?.UpdateDisplay(target.currentHP, target.maxHP);
        }
    }

    private void GainCardNextTurn(Card card)
    {
        System.Action onTurnStart = null;
        onTurnStart = () =>
        {
            TurnManager.Instance.OnPlayerTurnStart -= onTurnStart;
            HandManager.Instance?.AddCardToHandDirectly(card);
        };
        TurnManager.Instance.OnPlayerTurnStart += onTurnStart;
    }

    private void GainRandomSkill(List<Card> pool)
    {
        if (pool == null || pool.Count == 0) return;
        Card randomCard = pool[UnityEngine.Random.Range(0, pool.Count)];
        if (randomCard != null)
        {
            FreeCostTracker.AddFreeCard(randomCard);
            HandManager.Instance?.AddCardToHandDirectly(randomCard);
        }
    }

    // ★ [수정완료] 이동 포인트 감소 시스템 원천 제거 버전
    private void ApplyMoveEffect(int amount)
    {
        if (DistanceManager.Instance != null)
        {
            DistanceManager.Instance.Move(amount);
            Logger.Log($"[CardMovement] Move 효과 발동: 이동 포인트 소모 없이 {amount}만큼 거리가 변경되었습니다.");
        }
    }

    private System.Collections.IEnumerator RecoverAllAndFillHandRoutine()
    {
        ExhaustPile.RecoverCards(0);
        DeckManager.Instance?.ShuffleDeck();
        yield return null;
        int spaceLeft = HandManager.Instance.MaxHandSize - HandManager.Instance.CurrentHandSize;
        if (spaceLeft > 0)
            yield return HandManager.Instance.DrawCardsRoutine(spaceLeft);
    }

    private void ApplyReduceCost(int amount, bool all)
    {
        var hand = HandManager.Instance?.CardsInHand;
        if (hand == null || hand.Count == 0) return;
        if (all)
        {
            foreach (var cardObj in hand)
            {
                var display = cardObj?.GetComponent<CardDisplay>();
                if (display?.cardData != null)
                {
                    display.cardData.energyCost = Mathf.Max(0, display.cardData.energyCost - amount);
                    display.UpdateCardDisplay();
                }
            }
        }
        else
        {
            var display = hand[UnityEngine.Random.Range(0, hand.Count)]?.GetComponent<CardDisplay>();
            if (display?.cardData != null)
            {
                display.cardData.energyCost = Mathf.Max(0, display.cardData.energyCost - amount);
                display.UpdateCardDisplay();
            }
        }
    }

    private void RegisterDemonsDustOnDiscard(Card dustCard)
    {
        System.Action<Card> onDiscard = null;
        onDiscard = (discarded) =>
        {
            if (discarded == dustCard)
            {
                DeckManager.OnDiscardCard -= onDiscard;
                DemonsDustTracker.OnDustDiscarded(dustCard);
            }
        };
        DeckManager.OnDiscardCard += onDiscard;
    }

    /// <summary>외부에서 강제 초기화. Awake 전에 호출될 때 사용.</summary>
    public void ForceInitialize()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
    }

    // ── 유틸 ─────────────────────────────────────────────────────

    public void SetEnabled(bool value) => enabled = value;

    private void ShakeCard()
    {
        DOTween.Kill(gameObject);
        rectTransform.DOShakePosition(0.3f, strength: 10f, vibrato: 20);
    }

    private void ShowOutOfRangeFeedback()
    {
        var img = GetComponent<Image>();
        if (img == null) return;

        img.DOColor(new Color(1f, 0.4f, 0.4f, 1f), 0.15f)
           .SetLoops(2, LoopType.Yoyo)
           .OnComplete(() => img.color = Color.white);
    }

    private Color GetColorByCardType() => cardData.cardType switch
    {
        Card.CardType.Attack => new Color32(180, 28, 34, 255),
        Card.CardType.Skill => new Color32(0, 37, 194, 255),
        Card.CardType.Curse => new Color32(90, 20, 120, 255),
        _ => Color.white
    };

    private bool Blocked()
    {
        if (BattleManager.Instance != null && BattleManager.Instance.IsPlayerInputLocked) return true;
        if (TurnManager.Instance != null && !TurnManager.Instance.IsPlayerTurn) return true;
        if (HandManager.Instance != null && HandManager.Instance.IsDrawing) return true;
        if (!isInHand) return true;
        return false;
    }
}