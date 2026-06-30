using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 탐사 맵의 개별 노드.
/// 림버스 거울던전 방식: 현재 위치의 다음 레이어 노드만 클릭 가능.
/// connectedNodes는 단방향(앞 방향만) 저장됨.
/// </summary>
public class Node : MonoBehaviour
{
    public ExploreManager manager;
    public int layerIndex;

    /// <summary>앞 방향 연결 노드 목록 (단방향, layerIndex가 높은 쪽)</summary>
    public List<Node> connectedNodes = new List<Node>();

    // 지역 데이터
    public string        regionID;
    public string        regionName;
    public NodeEventType eventType  = NodeEventType.None;
    public RegionState   state      = RegionState.Locked;
    public RegionDifficulty difficulty = RegionDifficulty.Normal;
    public bool          hasBase    = false;

    public EventData nodeEvent;

    // 시각 상태
    public bool isRevealed        = false;
    public bool isVisited         = false;
    bool        isCurrentPosition = false;

    Button btn;

    void Awake()
    {
        btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(Click);
    }

    void Click()
    {
        if (manager == null) return;
        manager.SelectNode(gameObject);
    }

    public void Visit()
    {
        isVisited  = true;
        isRevealed = true;
    }

    public void SetCurrentPosition(bool isCurrent)
    {
        isCurrentPosition = isCurrent;
    }

    /// <summary>버튼 활성/비활성 (Locked 상태 제어)</summary>
    public void SetLocked(bool locked)
    {
        if (btn != null)
            btn.interactable = !locked;
    }

    // ──────────────────────────────────────────────
    // 노드 타입별 색상
    // ──────────────────────────────────────────────

    static Color GetTypeColor(NodeEventType type)
    {
        switch (type)
        {
            case NodeEventType.BaseCamp: return new Color(0.55f, 0.40f, 0.65f, 1f); // 보라 - 시작
            case NodeEventType.Resource: return new Color(0.25f, 0.60f, 0.85f, 1f); // 파랑 - 자원
            case NodeEventType.Hostile:  return new Color(0.80f, 0.30f, 0.30f, 1f); // 빨강 - 전투
            case NodeEventType.Elite:    return new Color(0.90f, 0.50f, 0.15f, 1f); // 주황 - 엘리트
            case NodeEventType.Neutral:  return new Color(0.85f, 0.75f, 0.25f, 1f); // 금색 - 이벤트
            case NodeEventType.Recruit:  return new Color(0.25f, 0.75f, 0.45f, 1f); // 초록 - 동료
            case NodeEventType.Boss:     return new Color(0.15f, 0.10f, 0.20f, 1f); // 진보라검정 - 보스
            default:                     return new Color(0.35f, 0.35f, 0.40f, 1f); // 회색
        }
    }

    static string GetTypeLabel(NodeEventType type)
    {
        switch (type)
        {
            case NodeEventType.BaseCamp: return "START";
            case NodeEventType.Resource: return "ITEM";
            case NodeEventType.Hostile:  return "ENEMY";
            case NodeEventType.Elite:    return "<b>ELITE</b>";
            case NodeEventType.Neutral:  return "EVENT";
            case NodeEventType.Recruit:  return "FRIEND";
            case NodeEventType.Boss:     return "<b>BOSS</b>";
            default:                     return "???";
        }
    }

    // ──────────────────────────────────────────────
    // UI 갱신
    // ──────────────────────────────────────────────

    static TMP_FontAsset cachedFont;
    bool      wasRevealed  = false;
    Coroutine fadeCoroutine;
    Coroutine pulseCoroutine;

    public void RefreshUI()
    {
        Image bg = GetComponent<Image>();

        // ── 색상/스프라이트 ──────────────────────
        if (bg != null)
        {
            if (!isRevealed)
            {
                // 안개 속 - 어두운 미지의 노드
                StopFade();
                bg.sprite = (manager != null) ? manager.unknownNodeSprite : null;
                bg.color  = (bg.sprite != null) ? Color.white : new Color(0.12f, 0.12f, 0.15f, 1f);
                wasRevealed = false;
            }
            else
            {
                // 스프라이트 선택
                bg.sprite = GetSpriteForType();

                Color targetColor;
                if (isCurrentPosition)
                {
                    // 현재 위치 - 밝게
                    targetColor = bg.sprite != null ? Color.white : GetTypeColor(eventType);
                }
                else if (isVisited)
                {
                    // 방문 완료 - 어둡게
                    Color baseColor = bg.sprite != null ? Color.white : GetTypeColor(eventType);
                    targetColor = new Color(baseColor.r * 0.4f, baseColor.g * 0.4f, baseColor.b * 0.4f, 0.7f);
                }
                else if (state == RegionState.Available)
                {
                    // 이동 가능 - 선명하게
                    targetColor = bg.sprite != null ? Color.white : GetTypeColor(eventType);
                }
                else
                {
                    // 잠김 (안개 공개됐지만 이동 불가)
                    targetColor = new Color(0.20f, 0.20f, 0.25f, 0.8f);
                }

                if (!wasRevealed)
                {
                    wasRevealed = true;
                    StopFade();
                    fadeCoroutine = StartCoroutine(FadeToColor(bg, targetColor, 0.4f));
                }
                else
                {
                    bg.color = targetColor;
                }
            }
        }

        // ── 스케일 & 펄스 애니메이션 ──────────────
        if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }

        if (isCurrentPosition)
        {
            transform.localScale = Vector3.one * 1.20f;
            pulseCoroutine = StartCoroutine(PulseScale(1.20f, 1.30f, 0.7f));
        }
        else if (state == RegionState.Available && !isVisited)
        {
            transform.localScale = Vector3.one * 1.05f;
        }
        else
        {
            transform.localScale = Vector3.one;
        }

        // ── 텍스트 ──────────────────────────────
        TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
        if (allTexts == null || allTexts.Length == 0) return;

        if (cachedFont == null)
            cachedFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        TMP_FontAsset useFont = cachedFont ?? (manager != null ? manager.malgunFont : null);

        string mainText;
        string subText = "";

        if (!isRevealed)
        {
            mainText = "???";
        }
        else
        {
            string visitMark = (isVisited && !isCurrentPosition) ? "<color=#888888>[V]</color>\n" : "";
            mainText  = visitMark + GetTypeLabel(eventType);
            subText   = string.IsNullOrEmpty(regionName) ? "" : regionName;
        }

        for (int i = 0; i < allTexts.Length; i++)
        {
            allTexts[i].gameObject.SetActive(true);
            if (useFont != null) allTexts[i].font = useFont;

            if (i == 0)
            {
                allTexts[i].text      = mainText;
                allTexts[i].color     = Color.white;
                allTexts[i].fontSize  = 11f;
                allTexts[i].alignment = TextAlignmentOptions.Center;
            }
            else if (i == 1)
            {
                allTexts[i].text      = subText;
                allTexts[i].color     = new Color(1f, 1f, 1f, 0.75f);
                allTexts[i].fontSize  = 9f;
                allTexts[i].alignment = TextAlignmentOptions.Center;
            }
            else
            {
                allTexts[i].gameObject.SetActive(false);
            }
        }
    }

    Sprite GetSpriteForType()
    {
        if (manager == null) return null;
        if (hasBase || eventType == NodeEventType.BaseCamp) return manager.baseNodeSprite;
        if (eventType == NodeEventType.Boss && manager.bossNodeSprite != null)
            return manager.bossNodeSprite;
        if (eventType == NodeEventType.Hostile || eventType == NodeEventType.Elite || eventType == NodeEventType.Boss)
            return manager.combatNodeSprite;
        if (eventType == NodeEventType.Neutral || eventType == NodeEventType.Resource || eventType == NodeEventType.Recruit)
            return manager.eventNodeSprite;
        return null;
    }

    void StopFade()
    {
        if (fadeCoroutine != null) { StopCoroutine(fadeCoroutine); fadeCoroutine = null; }
    }

    IEnumerator FadeToColor(Image img, Color target, float duration)
    {
        Color start = img.color;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            img.color = Color.Lerp(start, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            yield return null;
        }
        img.color = target;
    }

    IEnumerator PulseScale(float minScale, float maxScale, float period)
    {
        while (true)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / (period * 0.5f);
                float s = Mathf.Lerp(minScale, maxScale, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
                transform.localScale = Vector3.one * s;
                yield return null;
            }
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / (period * 0.5f);
                float s = Mathf.Lerp(maxScale, minScale, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
                transform.localScale = Vector3.one * s;
                yield return null;
            }
        }
    }
}
