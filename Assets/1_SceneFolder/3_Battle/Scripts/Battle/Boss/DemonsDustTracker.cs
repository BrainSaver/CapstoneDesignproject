using System.Collections.Generic;

/// <summary>
/// 악마의 티끌 카드가 discard될 때 덱에 추가되는 수를 추적한다.
/// 카드 1장당 보스 공격력이 bonusPerCard만큼 증가한다.
/// </summary>
public static class DemonsDustTracker
{
    /// <summary>현재 덱에 누적된 악마의 티끌 장수.</summary>
    public static int DustCount { get; private set; } = 0;

    /// <summary>카드 1장당 보스 공격력 증가량 (인스펙터 대신 BossAI에서 설정).</summary>
    public static int BonusPerCard { get; set; } = 3;

    /// <summary>악마의 티끌이 discard될 때 호출. 덱에 추가 + 보스 공격력 증가.</summary>
    public static void OnDustDiscarded(Card dustCard)
    {
        DustCount++;
        BossStrengthTracker.AddBonus(BonusPerCard);

        // 덱에 카드 추가
        PlayerDeck.Instance?.AddCardToDeck(dustCard);

        Logger.Log($"[DemonsDustTracker] 악마의 티끌 덱에 추가. 누적: {DustCount}장, " +
                   $"보스 공격력 +{BonusPerCard}. 총 보너스: {BossStrengthTracker.CurrentBonus}");
    }

    /// <summary>전투 시작/종료 시 초기화한다.</summary>
    public static void Reset()
    {
        DustCount = 0;
        Logger.Log("[DemonsDustTracker] 초기화.");
    }
}