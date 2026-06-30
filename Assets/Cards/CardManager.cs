using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    public static CardManager instance;

    [Header("모든 사용 가능한 카드 데이타")]
    public List<CardData> cardDeck;

    [Header("시스템 설정")]
    [Range(0, 1)]
    public float baseSuccessChance = 0.5f;

    public CardUI cardUI;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 카드를 굴려 성공 여부와 결과 카드를 반환합니다.
    /// </summary>
    public CardRollResult RollCard()
    {
        if (cardDeck == null || cardDeck.Count == 0)
        {
            Debug.LogWarning("카드 덱이 비어있습니다.");
            return new CardRollResult { success = false };
        }

        // 랜덤 카드 선택
        int randomIndex = Random.Range(0, cardDeck.Count);
        CardData drawnCard = cardDeck[randomIndex];

        // 성공 여부 판정 (카드 자체의 확률 * 시스템 기본 확률)
        bool success = Random.value <= (drawnCard.successChance * baseSuccessChance);

        // UI 연출 시작
        if (cardUI != null)
        {
            cardUI.ShowCardResult(drawnCard, success);
        }

        return new CardRollResult
        {
            success = success,
            card = drawnCard,
            additionalDamage = success ? drawnCard.extraDamage : 0
        };
    }
}

public struct CardRollResult
{
    public bool success;
    public CardData card;
    public int additionalDamage;
}
