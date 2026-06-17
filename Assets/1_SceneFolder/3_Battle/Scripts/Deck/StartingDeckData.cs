using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 시작 덱 카드 에셋 목록을 담는 ScriptableObject.
/// 문자열 이름 대신 Card 에셋을 직접 참조한다.
/// </summary>
[CreateAssetMenu(fileName = "StartingDeck", menuName = "Cards/Starting Deck")]
public class StartingDeckData : ScriptableObject
{
    [Tooltip("게임 시작 시 플레이어가 보유할 Card 에셋 목록.")]
    public List<Card> startingCards = new List<Card>();
}