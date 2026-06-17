using System;
using System.Collections.Generic;

/// <summary>
/// 읽기 전용 덱 제공자 인터페이스. 변경 알림 포함.
/// </summary>
public interface IDeckProvider<TCard>
{
    IReadOnlyList<TCard> GetDeck();
    event Action DeckChanged;
}