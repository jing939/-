using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CardUI : MonoBehaviour
{
    public GameObject cardPanel;        // 카드를 담고 있는 패널
    public Image cardFront;            // 카드 앞면 이미지 (제공된 에셋)
    public Image cardBack;             // 카드 뒷면 이미지 (필요 시)
    public TextMeshProUGUI resultText; // "성공!", "추가 대미지 +10" 등의 텍스트
    public float displayDuration = 2f; // 카드가 표시되는 시간

    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = cardPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = cardPanel.AddComponent<CanvasGroup>();
        
        cardPanel.SetActive(false);
    }

    public void ShowCardResult(CardData card, bool success)
    {
        StopAllCoroutines();
        StartCoroutine(AnimateCard(card, success));
    }

    private IEnumerator AnimateCard(CardData card, bool success)
    {
        cardPanel.SetActive(true);
        canvasGroup.alpha = 0;

        // 카드 이미지 설정
        if (card.cardImage != null)
        {
            cardFront.sprite = card.cardImage;
        }

        // 결과 텍스트 설정
        if (success)
        {
            resultText.text = $"<color=yellow>HIT!</color>\n+{card.extraDamage} Bonus Damage";
        }
        else
        {
            resultText.text = "<color=red>MISS</color>";
        }

        // 간단한 페이드 인
        float elapsed = 0;
        float fadeTime = 0.5f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / fadeTime);
            yield return null;
        }

        // 잠시 대기
        yield return new WaitForSeconds(displayDuration);

        // 페이드 아웃
        elapsed = 0;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / fadeTime);
            yield return null;
        }

        cardPanel.SetActive(false);
    }
}
