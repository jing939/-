using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusUIBar : MonoBehaviour
{
    private Image hpFill;
    private TextMeshProUGUI hpText;
    private Image spFill;
    private TextMeshProUGUI spText;
    
    public Image hpIcon;
    public Image spIcon;
    private GameObject owner;

    private float targetHpFill = 1f;
    private float targetSpFill = 1f;

    public static StatusUIBar Create(Transform parent, GameObject owner, Sprite hpSprite = null, Sprite spSprite = null)
    {
        GameObject go = new GameObject("StatusUIBar");
        go.transform.SetParent(parent, false);
        go.transform.SetAsFirstSibling(); // 다른 UI 패널(스킬 선택창 등)보다 뒤에 렌더링되도록 순서 변경
        StatusUIBar ui = go.AddComponent<StatusUIBar>();
        ui.owner = owner;
        ui.BuildUI(hpSprite, spSprite);
        return ui;
    }

    private CanvasGroup canvasGroup;
    private static Sprite blankSprite;

    private void BuildUI(Sprite hpSprite, Sprite spSprite)
    {
        if (blankSprite == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            blankSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }

        canvasGroup = gameObject.AddComponent<CanvasGroup>();

        RectTransform rt = gameObject.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(180, 80);
        rt.localScale = new Vector3(0.55f, 0.55f, 1f); // 전체적인 크기 축소 (캐릭터 크기에 맞게)

        VerticalLayoutGroup vlg = gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 3;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;

        CreateBar("HPBar", new Color(0.3f, 0.05f, 0.05f, 0.9f), new Color(0.7f, 0.15f, 0.15f, 1f), out hpIcon, out hpFill, out hpText);
        CreateBar("SPBar", new Color(0.1f, 0.05f, 0.15f, 0.9f), new Color(0.4f, 0.3f, 0.5f, 1f), out spIcon, out spFill, out spText);

        if (hpSprite != null) { hpIcon.sprite = hpSprite; hpIcon.color = Color.white; }
        if (spSprite != null) { spIcon.sprite = spSprite; spIcon.color = Color.white; }
    }

    private void CreateBar(string name, Color bgColor, Color fillColor, out Image iconImg, out Image fillImg, out TextMeshProUGUI textOut)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(transform, false);
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = 8;
        hlg.childControlHeight = false;
        hlg.childControlWidth = false;

        // Icon Area
        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(row.transform, false);
        iconImg = icon.AddComponent<Image>();
        // 림버스 스타일 아이콘: 기본적으로 하얀색, 유저가 인스펙터에서 교체할 수 있도록 둡니다
        iconImg.color = fillColor; 
        RectTransform iconRt = icon.GetComponent<RectTransform>();
        iconRt.sizeDelta = new Vector2(30, 30);

        // Bar Container
        GameObject bar = new GameObject("BarContainer");
        bar.transform.SetParent(row.transform, false);
        RectTransform barRt = bar.AddComponent<RectTransform>();
        barRt.sizeDelta = new Vector2(140, 30);
        
        Image bgImg = bar.AddComponent<Image>();
        bgImg.sprite = blankSprite;
        bgImg.color = bgColor;
        
        // 황금색 테두리
        Outline outline = bar.AddComponent<Outline>();
        outline.effectColor = new Color(0.8f, 0.7f, 0.4f); 
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // Fill Area
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(bar.transform, false);
        fillImg = fill.AddComponent<Image>();
        fillImg.sprite = blankSprite;
        fillImg.color = fillColor;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;
        fillRt.offsetMin = new Vector2(2, 2);
        fillRt.offsetMax = new Vector2(-2, -2); // 테두리 안쪽 여백

        // Text Area
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(bar.transform, false);
        textOut = txtObj.AddComponent<TextMeshProUGUI>();
        textOut.alignment = TextAlignmentOptions.Center;
        textOut.fontSize = 18;
        textOut.color = Color.white;
        textOut.fontStyle = FontStyles.Bold;
        
        // 글자 가독성을 위한 검은색 외곽선
        Outline txtOutline = txtObj.AddComponent<Outline>();
        txtOutline.effectColor = Color.black;
        txtOutline.effectDistance = new Vector2(1, -1);

        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;
    }

    private bool wasDimmed = false;
    private bool wasClashing = false;

    void Update()
    {
        if (canvasGroup == null) return;

        bool isDimmed = false;
        bool isCurrentlyClashing = false;

        if (SkillSelectionUIManager.instance != null && SkillSelectionUIManager.instance.skillPanel != null && SkillSelectionUIManager.instance.skillPanel.activeSelf)
        {
            isDimmed = true;
        }
        else if (BattleUIManager.instance != null && BattleUIManager.instance.clashPanel != null && BattleUIManager.instance.clashPanel.activeSelf)
        {
            if (owner != null && (owner == BattleUIManager.instance.currentClashPlayer || owner == BattleUIManager.instance.currentClashEnemy))
            {
                isDimmed = false;
                isCurrentlyClashing = true;
            }
            else
            {
                isDimmed = true;
            }
        }

        // 부드럽게 투명도를 전환
        float targetAlpha = isDimmed ? 0.2f : 1f;
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * 10f);

        // 하이어라키 순서 조절 (깜빡임 방지를 위해 상태가 변할 때만 1회 호출)
        if (isDimmed != wasDimmed || isCurrentlyClashing != wasClashing)
        {
            if (isCurrentlyClashing)
            {
                transform.SetAsLastSibling(); // 공격자는 맨 앞으로 (제일 늦게 그려져서 덮음)
            }
            else
            {
                transform.SetAsFirstSibling(); // 그 외는 맨 뒤로 (제일 먼저 그려져서 깔림)
            }
            
            wasDimmed = isDimmed;
            wasClashing = isCurrentlyClashing;
        }

        // 부드럽게 체력바 게이지 이동 (Lerp)
        if (hpFill != null) hpFill.fillAmount = Mathf.Lerp(hpFill.fillAmount, targetHpFill, Time.deltaTime * 10f);
        if (spFill != null) spFill.fillAmount = Mathf.Lerp(spFill.fillAmount, targetSpFill, Time.deltaTime * 10f);
    }

    public void UpdateHP(int current, int max)
    {
        targetHpFill = (float)current / max;
        if (hpText != null) hpText.text = $"{current} / {max} HP";
    }

    public void UpdateSP(int current, int max, bool isEnemy = false)
    {
        // 정신력은 0일 때 비어있고, 양수면 원래 색, 음수면 어두운 색으로 채워집니다.
        targetSpFill = Mathf.Abs(current) / 45f;
        
        if (spFill != null)
        {
            if (current >= 0)
                spFill.color = new Color(0.4f, 0.3f, 0.5f, 1f); // 원래 색상 (보라색)
            else
                spFill.color = new Color(0.2f, 0.15f, 0.25f, 1f); // 어두운 색상 (탁한 보라/회색)
        }

        if (spText != null) 
        {
            spText.text = $"{current} / 45 SP"; 
        }
    }
}
