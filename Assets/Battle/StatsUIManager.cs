using UnityEngine;
using TMPro;

public class StatsUIManager : MonoBehaviour
{
    public static StatsUIManager instance;

    private PlayerMove player;
    private RangedPlayerMove rangedPlayer;
    private Enemy enemy;

    [Header("유니티에서 직접 만드신 패널을 드래그 앤 드롭 하세요")]
    public GameObject playerStatsPanel;
    public GameObject enemyStatsPanel;

    [Header("유니티에서 직접 만드신 텍스트를 드래그 앤 드롭 하세요")]
    public TextMeshProUGUI playerStatsText;
    public TextMeshProUGUI enemyStatsText;

    [Header("상태이상 상세 패널 (자동 생성됨)")]
    public GameObject statusDetailsPanel;
    public Transform statusContentArea;
    
    [Header("효과 목록 팝업 설정")]
    public Vector2 statusDetailsPopupSize = new Vector2(580, 350);
    public float titleFontSize = 32;
    public float rowNameFontSize = 22;
    public float rowDescFontSize = 16;
    public float confirmBtnFontSize = 26;

    private TextMeshProUGUI titleTextComp;
    private TextMeshProUGUI confirmBtnTextComp;
    private bool showPlayerStats = false;
    private bool showEnemyStats = false;
    private bool showStatusDetails = false;
    private StatusEffect currentTargetStatus;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // 만약 유저님이 인스펙터에 패널 연결을 깜빡하셨거나, 캔버스가 깨져서 안 보인다면 코드가 강제로 살려냅니다!
        if (playerStatsPanel == null || playerStatsText == null)
        {
            Debug.LogWarning("인스펙터에 스탯 패널이 없어서 코드로 긴급 생성합니다!");
            CreateFallbackUI();
        }

        // 유저가 기존 패널은 연결했지만 상태이상 패널은 연결 안 했을 경우
        if (statusDetailsPanel == null || statusContentArea == null)
        {
            CreateStatusDetailsFallbackUI();
        }

        HideAll();
    }

    void CreateStatusDetailsFallbackUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // 1. 전체 배경 패널 (최상단으로 띄우기 위해 Canvas/GraphicRaycaster 추가)
        statusDetailsPanel = new GameObject("StatusDetailsPopup", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster));
        statusDetailsPanel.transform.SetParent(canvas.transform, false);
        statusDetailsPanel.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.95f);
        
        Canvas popupCanvas = statusDetailsPanel.GetComponent<Canvas>();
        popupCanvas.overrideSorting = true;
        popupCanvas.sortingOrder = 999; // 다른 모든 UI보다 위로 띄움

        RectTransform statusRt = statusDetailsPanel.GetComponent<RectTransform>();
        statusRt.anchorMin = new Vector2(0.5f, 0.5f);
        statusRt.anchorMax = new Vector2(0.5f, 0.5f);
        statusRt.pivot = new Vector2(0.5f, 0.5f);
        statusRt.anchoredPosition = Vector2.zero;
        statusRt.sizeDelta = statusDetailsPopupSize;

        // 2. 테두리
        GameObject border = new GameObject("Border", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        border.transform.SetParent(statusDetailsPanel.transform, false);
        UnityEngine.UI.Image borderImg = border.GetComponent<UnityEngine.UI.Image>();
        borderImg.color = new Color(0.35f, 0.25f, 0.15f, 1f);
        RectTransform borderRt = border.GetComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero; borderRt.anchorMax = Vector2.one;
        borderRt.sizeDelta = new Vector2(6, 6);

        // 3. 타이틀 ("효과 목록")
        GameObject titleObj = new GameObject("TitlePanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        titleObj.transform.SetParent(statusDetailsPanel.transform, false);
        titleObj.GetComponent<UnityEngine.UI.Image>().color = new Color(0.2f, 0.12f, 0.05f, 1f);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1); titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -10);
        titleRt.sizeDelta = new Vector2(-24, 60);

        GameObject titleTextObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleTextObj.transform.SetParent(titleObj.transform, false);
        titleTextComp = titleTextObj.GetComponent<TextMeshProUGUI>();
        titleTextComp.text = "효과 목록";
        titleTextComp.fontSize = titleFontSize;
        titleTextComp.color = new Color(1, 0.9f, 0.7f);
        titleTextComp.alignment = TextAlignmentOptions.Center;
        RectTransform ttRt = titleTextObj.GetComponent<RectTransform>();
        ttRt.anchorMin = Vector2.zero; ttRt.anchorMax = Vector2.one; ttRt.sizeDelta = Vector2.zero;

        // 4. 스크롤 영역 (드래그 가능하게 구성)
        GameObject scrollObj = new GameObject("ScrollArea", typeof(RectTransform), typeof(UnityEngine.UI.ScrollRect));
        scrollObj.transform.SetParent(statusDetailsPanel.transform, false);
        UnityEngine.UI.ScrollRect scrollRect = scrollObj.GetComponent<UnityEngine.UI.ScrollRect>();
        RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0, 0.25f); scrollRt.anchorMax = new Vector2(1, 0.8f);
        scrollRt.anchoredPosition = Vector2.zero;
        scrollRt.sizeDelta = new Vector2(-30, 0);

        // Viewport (잘리는 영역)
        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Mask));
        viewportObj.transform.SetParent(scrollObj.transform, false);
        viewportObj.GetComponent<UnityEngine.UI.Image>().color = new Color(1, 1, 1, 0.01f);
        viewportObj.GetComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
        RectTransform viewRt = viewportObj.GetComponent<RectTransform>();
        viewRt.anchorMin = Vector2.zero; viewRt.anchorMax = Vector2.one; viewRt.sizeDelta = Vector2.zero;

        // Content (실제 항목이 쌓이는 곳)
        GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(UnityEngine.UI.VerticalLayoutGroup), typeof(UnityEngine.UI.ContentSizeFitter));
        contentObj.transform.SetParent(viewportObj.transform, false);
        statusContentArea = contentObj.transform;
        
        scrollRect.content = contentObj.GetComponent<RectTransform>();
        scrollRect.viewport = viewRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 25;

        UnityEngine.UI.VerticalLayoutGroup vlg = contentObj.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        vlg.spacing = 15;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = false; vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
        vlg.padding = new RectOffset(10, 10, 10, 10);

        UnityEngine.UI.ContentSizeFitter csf = contentObj.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        csf.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0, 0);

        // 5. 확인 버튼
        GameObject btnObj = new GameObject("ConfirmButton", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        btnObj.transform.SetParent(statusDetailsPanel.transform, false);
        btnObj.GetComponent<UnityEngine.UI.Image>().color = new Color(0.25f, 0.15f, 0.05f, 1f);
        
        GameObject btnTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextObj.transform.SetParent(btnObj.transform, false);
        confirmBtnTextComp = btnTextObj.GetComponent<TextMeshProUGUI>();
        confirmBtnTextComp.text = "확인";
        confirmBtnTextComp.fontSize = confirmBtnFontSize;
        confirmBtnTextComp.color = Color.white;
        confirmBtnTextComp.alignment = TextAlignmentOptions.Center;
        
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0); btnRt.anchorMax = new Vector2(0.5f, 0);
        btnRt.pivot = new Vector2(0.5f, 0);
        btnRt.anchoredPosition = new Vector2(0, 25);
        btnRt.sizeDelta = new Vector2(180, 60);

        btnObj.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(HideAll);
    }

    void CreateFallbackUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("EmergencyCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        // 플레이어 긴급 패널 (왼쪽 위)
        playerStatsPanel = new GameObject("EmergencyPlayerPanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        playerStatsPanel.transform.SetParent(canvas.transform, false);
        playerStatsPanel.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.8f);
        
        GameObject pTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        pTextObj.transform.SetParent(playerStatsPanel.transform, false);
        playerStatsText = pTextObj.GetComponent<TextMeshProUGUI>();
        playerStatsText.fontSize = 40;
        playerStatsText.color = Color.white;

        // 적 긴급 패널 (오른쪽 위)
        enemyStatsPanel = new GameObject("EmergencyEnemyPanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        enemyStatsPanel.transform.SetParent(canvas.transform, false);
        enemyStatsPanel.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.8f);

        GameObject eTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        eTextObj.transform.SetParent(enemyStatsPanel.transform, false);
        enemyStatsText = eTextObj.GetComponent<TextMeshProUGUI>();
        enemyStatsText.fontSize = 40;
        enemyStatsText.color = Color.white;
    }

    public void TogglePlayerStats(PlayerMove p)
    {
        if (BattleManager.instance != null && BattleManager.instance.isClashing) return; // 전투 중에는 스탯창 안 뜨게 막기
        if (showPlayerStats && player == p) { HideAll(); return; }
        player = p;
        rangedPlayer = null;
        showPlayerStats = true;
        showEnemyStats = false;
        
        SetPlayerAnchor();
        UpdateVisibility();
    }

    public void ToggleRangedStats(RangedPlayerMove rp)
    {
        if (BattleManager.instance != null && BattleManager.instance.isClashing) return; // 전투 중 차단
        if (showPlayerStats && rangedPlayer == rp) { HideAll(); return; }
        rangedPlayer = rp;
        player = null;
        showPlayerStats = true;
        showEnemyStats = false;
        
        SetPlayerAnchor();
        UpdateVisibility();
    }

    void SetPlayerAnchor()
    {
        if (playerStatsPanel != null)
        {
            RectTransform rt = playerStatsPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(30, -30); // 왼쪽 위 모서리에서 살짝 떨어짐
                rt.sizeDelta = new Vector2(400, 300); // 넉넉한 크기
            }
        }
        
        if (playerStatsText != null)
        {
            RectTransform textRt = playerStatsText.GetComponent<RectTransform>();
            if (textRt != null)
            {
                textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
                textRt.offsetMin = new Vector2(20, 20); textRt.offsetMax = new Vector2(-20, -20);
            }
        }
    }

    public void ToggleEnemyStats(Enemy e)
    {
        if (BattleManager.instance != null && BattleManager.instance.isClashing) return; // 전투 중 차단
        if (showEnemyStats && enemy == e) { HideAll(); return; }
        enemy = e;
        showEnemyStats = true;
        showPlayerStats = false;

        // 확실하게 화면 '오른쪽 위(Top-Right)'로 고정시킵니다.
        if (enemyStatsPanel != null)
        {
            RectTransform rt = enemyStatsPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(1, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(1, 1);
                rt.anchoredPosition = new Vector2(-30, -30); // 오른쪽 위 모서리에서 살짝 떨어짐
                rt.sizeDelta = new Vector2(400, 300);
            }
        }

        if (enemyStatsText != null)
        {
            RectTransform textRt = enemyStatsText.GetComponent<RectTransform>();
            if (textRt != null)
            {
                textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
                textRt.offsetMin = new Vector2(20, 20); textRt.offsetMax = new Vector2(-20, -20);
            }
        }

        UpdateVisibility();
    }

    public void ToggleStatusDetails(StatusEffect target)
    {
        if (BattleManager.instance != null && BattleManager.instance.isClashing) return;

        if (showStatusDetails && currentTargetStatus == target)
        {
            HideAll();
            return;
        }

        // 인스펙터 설정값 실시간 반영
        if (statusDetailsPanel != null)
        {
            statusDetailsPanel.GetComponent<RectTransform>().sizeDelta = statusDetailsPopupSize;
        }
        if (titleTextComp != null) titleTextComp.fontSize = titleFontSize;
        if (confirmBtnTextComp != null) confirmBtnTextComp.fontSize = confirmBtnFontSize;

        currentTargetStatus = target;
        showPlayerStats = false;
        showEnemyStats = false;
        showStatusDetails = true;

        // 리스트 갱신
        RefreshStatusList(target);

        UpdateVisibility();
    }

    void RefreshStatusList(StatusEffect target)
    {
        if (statusContentArea == null) return;

        // 기존 항목 삭제
        foreach (Transform child in statusContentArea)
        {
            Destroy(child.gameObject);
        }

        // 상태이상 추가
        if (target.bleedCount > 0)
        {
            AddStatusRow("출혈", target.bleedPotency, target.bleedCount, 
                "코인을 굴릴 때 마다 위력만큼 피해를 입고 횟수가 감소합니다.", Color.red, target.bleedSprite);
        }
        if (target.poisonCount > 0)
        {
            AddStatusRow("독", target.poisonPotency, target.poisonCount, 
                "턴 종료 시 위력만큼 피해를 입고 횟수가 감소합니다.", new Color(0.6f, 0f, 0.8f), target.poisonSprite);
        }
        if (target.ruptureCount > 0)
        {
            AddStatusRow("파열", target.rupturePotency, target.ruptureCount, 
                "피격 시 위력만큼 고정 피해를 입고 횟수가 감소합니다.", Color.yellow, target.ruptureSprite);
        }
        if (target.sinkingCount > 0)
        {
            AddStatusRow("침잠", target.sinkingPotency, target.sinkingCount, 
                "피격 시 위력만큼 정신력(SP)이 감소하고 횟수가 감소합니다.", new Color(0.2f, 0.4f, 0.8f), target.sinkingSprite);
        }
    }

    void AddStatusRow(string name, int potency, int count, string desc, Color themeColor, Sprite iconSprite = null)
    {
        GameObject row = new GameObject("Row", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        row.transform.SetParent(statusContentArea, false);
        row.GetComponent<UnityEngine.UI.Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.6f);
        RectTransform rRt = row.GetComponent<RectTransform>();
        rRt.sizeDelta = new Vector2(0, 80);

        // 아이콘 (색상 박스로 대체하거나 텍스트로)
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        iconObj.transform.SetParent(row.transform, false);
        
        UnityEngine.UI.Image imgComp = iconObj.GetComponent<UnityEngine.UI.Image>();
        imgComp.color = iconSprite != null ? Color.white : themeColor;
        if (iconSprite != null) imgComp.sprite = iconSprite;
        
        RectTransform iRt = iconObj.GetComponent<RectTransform>();
        iRt.anchorMin = new Vector2(0, 0.5f); iRt.anchorMax = new Vector2(0, 0.5f);
        iRt.anchoredPosition = new Vector2(30, 0);
        iRt.sizeDelta = new Vector2(40, 40);

        // 이름 및 수치
        GameObject headObj = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
        headObj.transform.SetParent(row.transform, false);
        TextMeshProUGUI headText = headObj.GetComponent<TextMeshProUGUI>();
        headText.text = $"{name}  <size={rowNameFontSize * 0.8f}>위력 <color=white>{potency}</color>  횟수 <color=white>{count}</color></size>";
        headText.fontSize = rowNameFontSize;
        headText.color = themeColor;
        RectTransform hRt = headObj.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0, 1); hRt.anchorMax = new Vector2(1, 1);
        hRt.pivot = new Vector2(0, 1);
        hRt.anchoredPosition = new Vector2(80, -5);
        hRt.sizeDelta = new Vector2(-90, 30);

        // 설명
        GameObject descObj = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
        descObj.transform.SetParent(row.transform, false);
        TextMeshProUGUI descText = descObj.GetComponent<TextMeshProUGUI>();
        descText.text = desc;
        descText.fontSize = rowDescFontSize;
        descText.color = new Color(0.8f, 0.8f, 0.8f);
        RectTransform dRt = descObj.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0, 0); dRt.anchorMax = new Vector2(1, 0);
        dRt.pivot = new Vector2(0, 0);
        dRt.anchoredPosition = new Vector2(80, 5);
        dRt.sizeDelta = new Vector2(-90, 40);
    }

    public void HideAll()
    {
        showPlayerStats = false;
        showEnemyStats = false;
        showStatusDetails = false;
        currentTargetStatus = null;
        UpdateVisibility();
    }

    void UpdateVisibility()
    {
        if (playerStatsPanel != null) playerStatsPanel.SetActive(showPlayerStats);
        if (enemyStatsPanel != null) enemyStatsPanel.SetActive(showEnemyStats);
        if (statusDetailsPanel != null) statusDetailsPanel.SetActive(showStatusDetails);
    }

    void Update()
    {
        // === 텍스트 정보 실시간 갱신 ===
        if (showPlayerStats && playerStatsText != null)
        {
            if (player != null)
            {
                playerStatsText.text = $"<color=#00FF99>{player.gameObject.name}</color>\n\n" +
                                       $"체력(HP) : {player.hp} / {player.maxHp}\n\n" +
                                       $"정신력(SP) : {player.sp}\n\n" +
                                       $"공격 레벨 : {player.attackLevel}\n\n" +
                                       $"방어 레벨 : {player.defenseLevel}\n\n" +
                                       $"속도 범위 : {player.minSpeed} ~ {player.maxSpeed}";
            }
            else if (rangedPlayer != null)
            {
                playerStatsText.text = $"<color=#00FF99>{rangedPlayer.gameObject.name}</color>\n\n" +
                                       $"체력(HP) : {rangedPlayer.hp} / {rangedPlayer.maxHp}\n\n" +
                                       $"정신력(SP) : {rangedPlayer.sp}\n\n" +
                                       $"공격 레벨 : {rangedPlayer.attackLevel}\n\n" +
                                       $"방어 레벨 : {rangedPlayer.defenseLevel}\n\n" +
                                       $"속도 범위 : {rangedPlayer.minSpeed} ~ {rangedPlayer.maxSpeed}";
            }
        }

        if (showEnemyStats && enemyStatsText != null && enemy != null)
        {
            enemyStatsText.text = $"<color=#FF5555>{enemy.gameObject.name}</color>\n\n" +
                                  $"체력(HP) : {enemy.hp} / {enemy.maxHp}\n\n" +
                                  $"정신력(SP) : {enemy.sp}\n\n" +
                                  $"공격 레벨 : {enemy.attackLevel}\n\n" +
                                  $"방어 레벨 : {enemy.defenseLevel}\n\n" +
                                  $"속도 범위 : {enemy.minSpeed} ~ {enemy.maxSpeed}";
        }
    }
}
