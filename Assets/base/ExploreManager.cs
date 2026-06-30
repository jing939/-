using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 탐사 씬(ExploreScene) 메인 매니저.
/// 림버스 거울던전 방식:
///  - 레이어(층) 기반 단방향 진행 (앞으로만 이동)
///  - 현재 노드의 connectedNodes(다음층) 중 하나를 선택해 이동
///  - 방문한 노드는 재방문 불가, 회색으로 비활성화
///  - 거점(Base)이 있을 경우 탐사 가능 범위 제한 적용
/// </summary>
public class ExploreManager : MonoBehaviour
{
    public static ExploreManager instance;

    // ─────────────────────────────────────────────────
    // Inspector 할당 필드
    // ─────────────────────────────────────────────────

    [Header("맵 오브젝트 참조")]
    public GameObject    nodePrefab;
    public RectTransform nodesParent;
    public RectTransform linesParent;
    public RectTransform playerIcon;

    [Header("노드 스프라이트")]
    public Sprite combatNodeSprite;
    public Sprite eventNodeSprite;
    public Sprite baseNodeSprite;
    public Sprite playerSprite;
    public Sprite mapBgSprite;
    public Sprite bossNodeSprite;
    public Sprite unknownNodeSprite;

    [Header("UI 패널")]
    public GameObject      entryPanel;
    public GameObject      resultPanel;
    public TextMeshProUGUI resultUIText;

    [Header("이벤트 UI")]
    public TextMeshProUGUI eventTitleText;
    public TextMeshProUGUI eventDescText;
    public Transform       eventChoiceContainer;
    public GameObject      eventChoiceButtonPrefab;
    public TMP_FontAsset   malgunFont;

    [Header("탐사 설정")]
    public int  minLayer         = 8;
    public int  maxLayer         = 10;
    public bool resetMapOnReturn = true;

    [HideInInspector] public int savedMapSeed;

    // ─────────────────────────────────────────────────
    // 런타임 상태
    // ─────────────────────────────────────────────────

    [HideInInspector] public List<List<Node>> layers          = new List<List<Node>>();
    [HideInInspector] public List<Node>       pathTaken       = new List<Node>();
    [HideInInspector] public List<string>     usedEventTitles = new List<string>();
    [HideInInspector] public MapDrag          mapDragCache;

    public Node CurrentNode => currentNode;

    Node currentNode;
    Node selectedNode;

    int material;
    int food;
    int record;

    int totalMaterialGained;
    int totalFoodGained;
    int totalRecordGained;

    ResourceUI resourceUI;
    GameObject dimPanel;
    Coroutine  panCoroutine;
    Coroutine  dimCoroutine;
    bool       pendingMove;
    List<GameObject> hiddenObjects = new List<GameObject>(); // 결과창 닫을 때 이동 대기 중

    EventChoice currentBattleChoice;

    public bool IsPendingMove => pendingMove;
    public Node GetSelectedNode() => selectedNode;
    public int GetMaterial() => material;
    public int GetFood() => food;
    public int GetRecord() => record;

    // ─────────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────────

    void Awake()
    {
        #if UNITY_EDITOR
        baseNodeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/base_node_icon.png");
        combatNodeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/combat_node_icon.png");
        eventNodeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/event_node_icon.png");
        mapBgSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/explore_bg.png");
        #endif

        instance = this;
        resourceUI   = Object.FindAnyObjectByType<ResourceUI>();
        mapDragCache = Object.FindAnyObjectByType<MapDrag>();

        SetupPlayerIcon();
        SetupMapBackground();
        AutoLinkEntryPanelRefs();
        InjectFonts();
    }

    void Start()
    {
        InitializeGame();
    }

    void SetupPlayerIcon()
    {
        if (playerIcon == null || playerSprite == null) return;
        Image pImg = playerIcon.GetComponent<Image>() ?? playerIcon.gameObject.AddComponent<Image>();
        pImg.sprite          = playerSprite;
        pImg.color           = Color.white;
        playerIcon.sizeDelta = new Vector2(72f, 72f);
    }

    void SetupMapBackground()
    {
        if (mapBgSprite == null || nodesParent == null || nodesParent.parent == null) return;
        if (nodesParent.parent.Find("MapBackground") != null) return;

        GameObject bgObj = new GameObject("MapBackground");
        bgObj.transform.SetParent(nodesParent.parent, false);
        bgObj.transform.SetAsFirstSibling();

        Image bgImg  = bgObj.AddComponent<Image>();
        bgImg.sprite = mapBgSprite;
        bgImg.color  = new Color(0.50f, 0.50f, 0.50f, 1f);

        RectTransform rt = bgObj.GetComponent<RectTransform>();
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = new Vector2(-4000f, -2000f);
        rt.offsetMax  = new Vector2( 4000f,  2000f);
    }

    void AutoLinkEntryPanelRefs()
    {
        if (entryPanel == null) return;
        if (eventTitleText      == null) eventTitleText      = entryPanel.transform.Find("EventTitle")?.GetComponent<TextMeshProUGUI>();
        if (eventDescText       == null) eventDescText       = entryPanel.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
        if (eventChoiceContainer == null) eventChoiceContainer = entryPanel.transform.Find("ChoiceContainer");
    }

    void InjectFonts()
    {
        TMP_FontAsset libSans = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        TMP_FontAsset useFont = libSans != null ? libSans : malgunFont;
        if (useFont == null) return;

        if (entryPanel   != null) foreach (var t in entryPanel.GetComponentsInChildren<TextMeshProUGUI>(true)) t.font = useFont;
        if (resultUIText != null) resultUIText.font = useFont;
        if (resourceUI   != null)
        {
            if (resourceUI.foodText     != null) resourceUI.foodText.font     = useFont;
            if (resourceUI.materialText != null) resourceUI.materialText.font = useFont;
            if (resourceUI.recordText   != null) resourceUI.recordText.font   = useFont;
        }
    }

    // ─────────────────────────────────────────────────
    // 게임 초기화
    // ─────────────────────────────────────────────────

    void InitializeGame()
    {
        GameManager.instance?.LoadProgress();

        // MainMenu 진입 플래그가 있는 경우 세이브 삭제
        if (PlayerPrefs.GetInt("NewGameStart", 0) == 1)
        {
            PlayerPrefs.DeleteKey("NewGameStart");
            ExploreSaveData.Clear();
            if (BaseManager.instance != null)
            {
                BaseManager.instance.baseNodes.Clear();
            }
            PlayerPrefs.Save();
        }

        if (BaseManager.instance != null)
        {
            // 씬 재로드에 따른 Dangling Reference 방지
            BaseManager.instance.baseNodes.Clear();
        }

        if (GameManager.instance != null)
        {
            material = GameManager.instance.material;
            food     = GameManager.instance.food;
            record   = GameManager.instance.record;
        }

        totalMaterialGained = 0;
        totalFoodGained     = 0;
        totalRecordGained   = 0;
        pendingMove         = false;

        if (resultUIText != null) resultUIText.text = "";

        CreateDimPanel();

        if (entryPanel  != null) entryPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (dimPanel    != null) dimPanel.SetActive(false);

        if (TryRestoreSavedExplore())
            return;

        // 맵 생성
        ExploreMapGenerator.GenerateMap(this);

        if (layers.Count == 0 || layers[0].Count == 0)
        {
            Debug.LogError("[ExploreManager] 맵 생성 실패: 노드가 없습니다.");
            return;
        }

        SetupAfterMapReady(layers[0][0], clearPath: true);
    }

    void SetupAfterMapReady(Node startNode, bool clearPath)
    {
        LinkReturnButton();

        currentNode = startNode;
        if (clearPath)
        {
            pathTaken.Clear();
            pathTaken.Add(currentNode);
        }

        if (playerIcon != null)
            playerIcon.anchoredPosition = currentNode.GetComponent<RectTransform>().anchoredPosition;

        PanToNode(currentNode.GetComponent<RectTransform>(), instant: true);

        if (!currentNode.isVisited)
        {
            currentNode.Visit();
            currentNode.SetCurrentPosition(true);
        }
        else
        {
            currentNode.SetCurrentPosition(true);
        }

        InitNodeStates();
    }

    bool TryRestoreSavedExplore()
    {
        if (!ExploreSaveData.TryLoad(out ExploreSaveData save)) return false;

        ClearMapObjects();

        minLayer = save.minLayer;
        maxLayer = save.maxLayer;
        material = save.material;
        food     = save.food;
        record   = save.record;
        pendingMove = save.pendingMove;
        usedEventTitles = save.usedEventTitles ?? new List<string>();
        savedMapSeed = save.mapSeed;

        ExploreMapGenerator.GenerateMap(this, save.mapSeed);

        if (layers.Count == 0) return false;

        foreach (var entry in save.nodes)
        {
            if (entry.layer < 0 || entry.layer >= layers.Count) continue;
            if (entry.index < 0 || entry.index >= layers[entry.layer].Count) continue;

            Node node = layers[entry.layer][entry.index];
            node.eventType  = (NodeEventType)entry.eventType;
            node.difficulty = (RegionDifficulty)entry.difficulty;
            node.regionName = entry.regionName;
            node.isVisited  = entry.isVisited;
            node.state      = (RegionState)entry.state;
        }

        foreach (var entry in save.nodes)
        {
            if (entry.layer < 0 || entry.layer >= layers.Count) continue;
            if (entry.index < 0 || entry.index >= layers[entry.layer].Count) continue;

            Node node = layers[entry.layer][entry.index];
            node.connectedNodes.Clear();
            int nextLayer = entry.layer + 1;
            if (nextLayer >= layers.Count) continue;

            foreach (int connIdx in entry.connectedIndices)
            {
                if (connIdx >= 0 && connIdx < layers[nextLayer].Count)
                    node.connectedNodes.Add(layers[nextLayer][connIdx]);
            }
        }

        Node restoredCurrent = layers[save.currentLayer][save.currentIndex];
        SetupAfterMapReady(restoredCurrent, clearPath: false);

        if (save.selectedLayer >= 0 && save.selectedIndex >= 0
            && save.selectedLayer < layers.Count
            && save.selectedIndex < layers[save.selectedLayer].Count)
        {
            selectedNode = layers[save.selectedLayer][save.selectedIndex];
        }

        if (save.inBattle)
        {
            pendingMove = false;
            selectedNode = null;
            ExploreSaveData.ClearBattleFlag();
            ExploreSaveData.Save(this, inBattle: false);
            Debug.Log("[ExploreManager] 전투 중단 복원: 탐사 위치로 되돌렸습니다.");
        }
        else
        {
            Debug.Log("[ExploreManager] 저장된 탐사 진행을 복원했습니다.");
        }

        SyncToGameManager();
        return true;
    }

    void ClearMapObjects()
    {
        foreach (var layer in layers)
            foreach (var node in layer)
                if (node != null) Destroy(node.gameObject);

        layers.Clear();

        if (linesParent != null)
            foreach (Transform child in linesParent) Destroy(child.gameObject);
    }

    void LinkReturnButton()
    {
        Button returnBtn = null;
        if (entryPanel != null)
            returnBtn = entryPanel.transform.parent?.Find("UI/ReturnButton")?.GetComponent<Button>();
        if (returnBtn == null)
            returnBtn = GameObject.Find("ReturnButton")?.GetComponent<Button>();
        if (returnBtn != null)
        {
            returnBtn.gameObject.SetActive(true);
            returnBtn.onClick.RemoveAllListeners();
            returnBtn.onClick.AddListener(ShowFinalSettlement);
        }
    }

    /// <summary>
    /// 시작 시 및 이동 완료 후 노드 상태 일괄 초기화.
    /// 거울던전 규칙: currentNode의 connectedNodes만 Available, 나머지 Locked.
    /// 방문 완료 노드는 Cleared 유지.
    /// </summary>
    void InitNodeStates()
    {
        // 모든 노드 상태 리셋
        foreach (var layer in layers)
            foreach (var node in layer)
                node.state = node.isVisited ? RegionState.Cleared : RegionState.Locked;

        // 현재 노드
        currentNode.state = RegionState.Cleared;

        // 거점 기반 탐사 범위 제한이 있을 경우
        if (BaseManager.instance != null && BaseManager.instance.baseNodes.Count > 0)
        {
            // 거점으로부터 BFS로 도달 가능한 앞방향 노드만 Available
            SetAvailableByBaseRange();
        }
        else
        {
            // BaseManager 없음: 현재 노드의 직접 연결 노드만 Available
            foreach (Node next in currentNode.connectedNodes)
                if (!next.isVisited) next.state = RegionState.Available;
        }

        UpdateNodeVisuals();
    }

    /// <summary>거점 사거리 기반 BFS로 도달 가능한 앞방향 노드에만 Available 부여</summary>
    void SetAvailableByBaseRange()
    {
        int range = BaseManager.instance != null ? BaseManager.instance.baseExplorationRange : 4;

        // 모든 거점 기준 BFS (앞 방향만)
        HashSet<Node> reachable = new HashSet<Node>();
        foreach (Node bNode in BaseManager.instance.baseNodes)
        {
            var queue = new Queue<(Node node, int dist)>();
            queue.Enqueue((bNode, 0));
            reachable.Add(bNode);
            while (queue.Count > 0)
            {
                var (cur, dist) = queue.Dequeue();
                if (dist >= range) continue;
                foreach (Node neighbor in cur.connectedNodes)
                {
                    if (!reachable.Contains(neighbor))
                    {
                        reachable.Add(neighbor);
                        queue.Enqueue((neighbor, dist + 1));
                    }
                }
            }
        }

        // currentNode의 connectedNodes 중 reachable이면 Available
        foreach (Node next in currentNode.connectedNodes)
        {
            if (!next.isVisited)
                next.state = reachable.Contains(next) ? RegionState.Available : RegionState.Locked;
        }
    }

    // ─────────────────────────────────────────────────
    // 노드 시각 업데이트 (안개 + UI + 선)
    // ─────────────────────────────────────────────────

    public void UpdateNodeStates()
    {
        InitNodeStates();
    }

    void UpdateNodeVisuals()
    {
        // 안개: 방문했거나 Available이면 공개
        foreach (var layer in layers)
            foreach (var node in layer)
                node.isRevealed = node.isVisited || (node.state == RegionState.Available);

        // 현재 노드의 다음 연결 노드는 항상 공개 (타입 표시)
        if (currentNode != null)
        {
            currentNode.isRevealed = true;
            foreach (Node next in currentNode.connectedNodes)
                next.isRevealed = true;
        }

        // 버튼 잠금 & UI 갱신
        foreach (var layer in layers)
            foreach (var node in layer)
            {
                // Available 상태인 노드만 클릭 가능 (앞으로만 이동)
                bool clickable = (node.state == RegionState.Available);
                node.SetLocked(!clickable);
                node.RefreshUI();
            }

        ExploreMapGenerator.DrawLines(this);
    }



    // ─────────────────────────────────────────────────
    // 탐색 재시작
    // ─────────────────────────────────────────────────

    public void RestartExploration()
    {
        ExploreSaveData.Clear();

        foreach (var layer in layers)
            foreach (var node in layer)
                if (node != null) Destroy(node.gameObject);

        layers.Clear();
        usedEventTitles.Clear();

        if (eventChoiceContainer != null)
            foreach (Transform child in eventChoiceContainer) Destroy(child.gameObject);

        if (linesParent != null)
            foreach (Transform child in linesParent) Destroy(child.gameObject);

        InitializeGame();
        Debug.Log("[ExploreManager] 탐사 재시작.");
    }

    // ─────────────────────────────────────────────────
    // 카메라 패닝
    // ─────────────────────────────────────────────────

    public void StopPanning()
    {
        if (panCoroutine != null) { StopCoroutine(panCoroutine); panCoroutine = null; }
    }

    public void PanToNode(RectTransform target, bool instant = false)
    {
        if (mapDragCache == null || mapDragCache.map == null) return;
        StopPanning();

        float   targetX   = Mathf.Clamp(-target.anchoredPosition.x, mapDragCache.minX, mapDragCache.maxX);
        Vector2 targetPos = new Vector2(targetX, mapDragCache.map.anchoredPosition.y);

        if (instant) mapDragCache.map.anchoredPosition = targetPos;
        else         panCoroutine = StartCoroutine(PanRoutine(targetPos));
    }

    IEnumerator PanRoutine(Vector2 targetPos)
    {
        if (mapDragCache == null || mapDragCache.map == null) yield break;
        Vector2 startPos = mapDragCache.map.anchoredPosition;
        float   t        = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            mapDragCache.map.anchoredPosition = Vector2.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            yield return null;
        }
        mapDragCache.map.anchoredPosition = targetPos;
    }

    // ─────────────────────────────────────────────────
    // Dim 패널
    // ─────────────────────────────────────────────────

    void CreateDimPanel()
    {
        if (dimPanel != null) return;
        Transform existing = transform.parent != null ? transform.parent.Find("DimPanel") : null;
        if (existing != null) { dimPanel = existing.gameObject; return; }

        dimPanel = new GameObject("DimPanel");
        dimPanel.transform.SetParent(entryPanel != null ? entryPanel.transform.parent : transform, false);
        RectTransform rt = dimPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        Image img = dimPanel.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.75f);
        img.raycastTarget = true;
        dimPanel.SetActive(false);
    }

    void SetDim(float alpha, float duration)
    {
        if (dimCoroutine != null) StopCoroutine(dimCoroutine);
        dimCoroutine = StartCoroutine(FadeDim(alpha, duration));
    }

    IEnumerator FadeDim(float targetAlpha, float duration)
    {
        if (dimPanel == null) yield break;
        Image img = dimPanel.GetComponent<Image>();
        if (img == null) yield break;

        dimPanel.SetActive(true);
        float start = img.color.a, t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(duration, 0.01f);
            img.color = new Color(0f, 0f, 0f, Mathf.Lerp(start, targetAlpha, t));
            yield return null;
        }
        img.color = new Color(0f, 0f, 0f, targetAlpha);
        if (targetAlpha <= 0f) dimPanel.SetActive(false);
    }

    // ─────────────────────────────────────────────────
    // 노드 선택 (거울던전: 앞으로만 이동)
    // ─────────────────────────────────────────────────

    public void SelectNode(GameObject obj)
    {
        if (obj == null) return;
        Node node = obj.GetComponent<Node>();
        if (node == null) return;

        // 현재 노드 클릭 → 무시 (앞으로만 이동)
        if (node == currentNode) return;

        // 방문한 노드 → 무시
        if (node.isVisited) return;

        // 잠긴 노드 → 무시
        if (node.state == RegionState.Locked) return;

        // currentNode의 connectedNodes(다음층)에 있는지 확인
        if (!currentNode.connectedNodes.Contains(node)) return;

        selectedNode = node;
        PanToNode(node.GetComponent<RectTransform>());
        ShowEntryPanel(node);
    }

    void ShowEntryPanel(Node node)
    {
        SetDim(0.70f, 0.20f);

        if (dimPanel   != null) dimPanel.transform.SetAsLastSibling();
        if (entryPanel != null)
        {
            entryPanel.transform.SetAsLastSibling();
            entryPanel.SetActive(true);
            entryPanel.transform.localScale = Vector3.one * 0.90f;
            StartCoroutine(AnimateScale(entryPanel.transform, Vector3.one, 0.15f));
        }

        SetupEventUI(node);
    }

    // ─────────────────────────────────────────────────
    // 이벤트 UI 구성
    // ─────────────────────────────────────────────────

    void SetupEventUI(Node node)
    {
        if (node == null) return;

        bool isBoss    = node.eventType == NodeEventType.Boss;
        bool hasEvent  = node.nodeEvent != null && node.nodeEvent.choices.Count > 0 && !node.isVisited;

        // 타이틀 표시 여부
        if (eventTitleText != null)
        {
            eventTitleText.gameObject.SetActive(true);
            eventTitleText.text      = node.nodeEvent?.eventTitle ?? GetNodeTypeTitle(node.eventType);
            eventTitleText.fontSize  = 26f;
            eventTitleText.alignment = TextAlignmentOptions.Center;
        }

        // 설명 텍스트
        if (eventDescText != null)
        {
            string desc = hasEvent
                ? (node.nodeEvent?.eventDescription ?? "")
                : GetDefaultDesc(node);
            eventDescText.text        = desc;
            eventDescText.fontSize    = 17f;
            eventDescText.lineSpacing = 1.2f;
            eventDescText.alignment   = TextAlignmentOptions.Center;
        }

        // 선택지 컨테이너
        if (eventChoiceContainer != null)
            eventChoiceContainer.gameObject.SetActive(hasEvent);

        Transform yesBtn = entryPanel != null ? entryPanel.transform.Find("YesButton") : null;
        Transform noBtn  = entryPanel != null ? entryPanel.transform.Find("NoButton")  : null;

        if (hasEvent)
        {
            if (yesBtn != null) yesBtn.gameObject.SetActive(false);
            BuildChoiceButtons(node, yesBtn);
        }
        else
        {
            // 선택지 없음: Yes/No 버튼
            if (yesBtn != null)
            {
                yesBtn.gameObject.SetActive(true);
                Button b = yesBtn.GetComponent<Button>();
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(EnterNode);
                TextMeshProUGUI btnText = yesBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = isBoss ? "도전!" : "진입";
            }
        }

        if (noBtn != null) noBtn.gameObject.SetActive(true);

        if (entryPanel != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(entryPanel.GetComponent<RectTransform>());
    }

    string GetNodeTypeTitle(NodeEventType type)
    {
        switch (type)
        {
            case NodeEventType.Hostile:  return "적 발견";
            case NodeEventType.Elite:    return "강적 발견";
            case NodeEventType.Boss:     return "최종 보스";
            case NodeEventType.Resource: return "보급품 발견";
            case NodeEventType.Neutral:  return "특수 이벤트";
            case NodeEventType.Recruit:  return "생존자 발견";
            default: return "탐색";
        }
    }

    string GetDefaultDesc(Node node)
    {
        if (node.eventType == NodeEventType.Boss)
            return "던전의 끝. 최후의 적이 기다리고 있다.\n준비가 됐는가?";
        return $"{node.regionName}\n이 구역으로 이동하겠습니까?";
    }

    void BuildChoiceButtons(Node node, Transform yesBtn)
    {
        if (eventChoiceContainer == null) return;
        foreach (Transform child in eventChoiceContainer) Destroy(child.gameObject);

        GameObject template = eventChoiceButtonPrefab != null
            ? eventChoiceButtonPrefab
            : yesBtn?.gameObject;

        if (template == null) return;

        foreach (EventChoice choice in node.nodeEvent.choices)
        {
            GameObject     btnObj  = Instantiate(template, eventChoiceContainer);
            btnObj.SetActive(true);

            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text             = choice.choiceText;
                btnText.enableAutoSizing = true;
                btnText.fontSizeMin      = 10;
                btnText.fontSizeMax      = 17;
            }

            RectTransform btnRT = btnObj.GetComponent<RectTransform>();
            if (btnRT != null) btnRT.sizeDelta = new Vector2(320f, 42f);

            EventChoice captured = choice;
            Button      b        = btnObj.GetComponent<Button>();
            if (b != null)
            {
                b.onClick = new Button.ButtonClickedEvent();
                b.onClick.AddListener(() => OnChoiceSelected(captured));
            }
        }
    }

    // ─────────────────────────────────────────────────
    // 선택지 처리
    // ─────────────────────────────────────────────────

    public void OnChoiceSelected(EventChoice choice)
    {
        if (choice == null) return;

        string outcomeText = choice.outcomes != null && choice.outcomes.Count > 0
            ? choice.outcomes[Random.Range(0, choice.outcomes.Count)]
            : choice.choiceText;

        pendingMove = true;

        if (entryPanel != null) entryPanel.SetActive(false);

        if (choice.triggersBattle)
        {
            StartCoroutine(StartCombatRoutine(selectedNode, choice));
            return;
        }

        // 전투가 아닌 선택지: 즉시 보상 적용
        ParseAndApplyRewards(outcomeText, choice);
        HandleSpecialTags(outcomeText);
        SyncToGameManager();

        if (food <= 0)
        {
            ForceReturnToBase();
            return;
        }

        ShowResult(outcomeText);
    }

    void ParseAndApplyRewards(string text, EventChoice choice)
    {
        if (choice.outcomes != null && choice.outcomes.Count > 0)
        {
            // 텍스트에서 수치 파싱
            ParseRewardTag(text, "Material+", ref material, ref totalMaterialGained, false);
            ParseRewardTag(text, "Material-", ref material, ref totalMaterialGained, true);
            ParseRewardTag(text, "Food+",     ref food,     ref totalFoodGained,     false);
            ParseRewardTag(text, "Food-",     ref food,     ref totalFoodGained,     true);
            ParseRewardTag(text, "Record+",   ref record,   ref totalRecordGained,   false);
            ParseRewardTag(text, "Record-",   ref record,   ref totalRecordGained,   true);
        }
        else
        {
            // 정적 보상
            material            += choice.resourceReward;
            food                += choice.foodReward;
            record              += choice.recordReward;
            totalMaterialGained += Mathf.Max(0, choice.resourceReward);
            totalFoodGained     += Mathf.Max(0, choice.foodReward);
            totalRecordGained   += Mathf.Max(0, choice.recordReward);
        }

        material = Mathf.Max(0, material);
        food     = Mathf.Max(0, food);
        record   = Mathf.Max(0, record);
    }

    void ParseRewardTag(string text, string keyword, ref int target, ref int total, bool isPenalty)
    {
        if (!text.Contains(keyword)) return;
        try
        {
            int start = text.IndexOf(keyword) + keyword.Length;
            int end   = start;
            while (end < text.Length && char.IsDigit(text[end])) end++;
            if (end > start)
            {
                int val = int.Parse(text.Substring(start, end - start));
                if (isPenalty) { target = Mathf.Max(0, target - val); }
                else           { target += val; total += val; }
            }
        }
        catch { }
    }

    void HandleSpecialTags(string outcome)
    {
        if (GameManager.instance == null) return;

        // HP 피해
        if (outcome.Contains("[HPDamage:"))
        {
            try
            {
                int s = outcome.IndexOf("[HPDamage:") + 10;
                int e = outcome.IndexOf("]", s);
                int dmg = int.Parse(outcome.Substring(s, e - s));
                GameManager.instance.hp -= dmg;
            }
            catch { GameManager.instance.hp -= 10; }
            if (GameManager.instance.hp <= 0) ForceReturnToBase();
        }

        // HP 회복
        if (outcome.Contains("[HPRecover]"))
            GameManager.instance.hp = Mathf.Min(GameManager.instance.maxHp, GameManager.instance.hp + 20);

        // 동료 합류
        if (outcome.Contains("[Recruit:"))
        {
            try
            {
                int s = outcome.IndexOf("[Recruit:") + 9;
                int e = outcome.IndexOf("]", s);
                string[] parts = outcome.Substring(s, e - s).Split(':');
                if (parts.Length >= 2 && int.TryParse(parts[1], out int power))
                    GameManager.instance.allies.Add(new AllyData(parts[0], power, selectedNode?.regionName));
            }
            catch { }
        }

        // 기술 습득
        if (outcome.Contains("[Skill:"))
        {
            try
            {
                int s = outcome.IndexOf("[Skill:") + 7;
                int e = outcome.IndexOf("]", s);
                string skillName = outcome.Substring(s, e - s);
                if (!GameManager.instance.acquiredSkills.Contains(skillName))
                    GameManager.instance.acquiredSkills.Add(skillName);
            }
            catch { }
        }

        // 기록(Lore) 수집
        if (outcome.Contains("[Lore:"))
        {
            try
            {
                int s = outcome.IndexOf("[Lore:") + 6;
                int e = outcome.IndexOf("]", s);
                string[] parts = outcome.Substring(s, e - s).Split(':');
                if (parts.Length >= 3)
                    GameManager.instance.collectedLores.Add(new LoreData(parts[0], parts[1], parts[2], selectedNode?.regionName));
            }
            catch { }
        }
    }

    // ─────────────────────────────────────────────────
    // 전투
    // ─────────────────────────────────────────────────

    IEnumerator StartCombatRoutine(Node targetNode, EventChoice choice)
    {
        if (entryPanel != null) entryPanel.SetActive(true);
        if (eventChoiceContainer != null) eventChoiceContainer.gameObject.SetActive(false);

        if (eventTitleText != null) eventTitleText.text = "<color=#FF4444>전투 진행 중...</color>";
        if (eventDescText  != null) eventDescText.text  = "전투 씬 로딩 중...";

        yield return new WaitForSeconds(1.5f);

        if (entryPanel != null) entryPanel.SetActive(false);
        if (dimPanel   != null) dimPanel.SetActive(false);

        currentBattleChoice = choice;
        ExploreSaveData.Save(this, inBattle: true);
        GameManager.instance?.SaveProgress();

        ToggleExploreSceneActive(false);

        string sceneName = (targetNode.eventType == NodeEventType.Boss) ? "BossBattleScene" : "BattleScene";
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName,
            UnityEngine.SceneManagement.LoadSceneMode.Additive);
    }

    public void OnBattleFinished(bool win)
    {
        ToggleExploreSceneActive(true);

        ExploreSaveData.ClearBattleFlag();

        SetDim(0.70f, 0.25f);
        if (entryPanel    != null) entryPanel.SetActive(true);
        if (eventTitleText != null) eventTitleText.gameObject.SetActive(true);

        if (win)
        {
            if (eventTitleText != null) eventTitleText.text = "<color=#44FF88>승리!</color>";

            string winMsg = "전투에서 승리했습니다!";
            if (currentBattleChoice?.outcomes != null && currentBattleChoice.outcomes.Count > 0)
            {
                string outcome = currentBattleChoice.outcomes[Random.Range(0, currentBattleChoice.outcomes.Count)];
                winMsg += "\n\n" + outcome;
                ParseAndApplyRewards(outcome, currentBattleChoice);
                HandleSpecialTags(outcome);
            }

            SyncToGameManager();
            GameManager.instance?.SaveProgress();
            ExploreSaveData.Save(this, inBattle: false);

            pendingMove = true;
            ShowResult(winMsg);
        }
        else
        {
            if (eventTitleText != null) eventTitleText.text = "<color=#FF4444>패배...</color>";
            ApplyDefeatPenalty();
            RetreatToNearestBase();
            selectedNode = null;
            pendingMove = false;
            ExploreSaveData.Save(this, inBattle: false);
            GameManager.instance?.SaveProgress();
            if (entryPanel  != null) entryPanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
            SetDim(0f, 0.5f);
        }
    }

    void ApplyDefeatPenalty()
    {
        int lostMat  = Mathf.RoundToInt(material * 0.20f);
        int lostFood = Mathf.RoundToInt(food     * 0.20f);
        material = Mathf.Max(0, material - lostMat);
        food     = Mathf.Max(0, food     - lostFood);
        SyncToGameManager();
        Debug.Log($"[DEFEAT] 물자 -{lostMat}, 식량 -{lostFood}");
    }

    void RetreatToNearestBase()
    {
        Node retreat = layers[0][0]; // 기본: 시작 노드

        if (BaseManager.instance != null && BaseManager.instance.baseNodes.Count > 0)
        {
            retreat = BaseManager.instance.baseNodes[0];
            int minDist = Mathf.Abs(currentNode.layerIndex - retreat.layerIndex);
            foreach (var bNode in BaseManager.instance.baseNodes)
            {
                int d = Mathf.Abs(currentNode.layerIndex - bNode.layerIndex);
                if (d < minDist) { minDist = d; retreat = bNode; }
            }
        }

        currentNode = retreat;
        if (playerIcon != null)
            playerIcon.anchoredPosition = currentNode.GetComponent<RectTransform>().anchoredPosition;
        PanToNode(currentNode.GetComponent<RectTransform>());
        InitNodeStates();
    }

    // ─────────────────────────────────────────────────
    // 진입 확인 / 결과창 처리
    // ─────────────────────────────────────────────────

    /// <summary>
    /// 이벤트 없는 노드 진입 확인 버튼 (Yes 버튼).
    /// 선택지 이벤트가 있는 경우엔 호출되지 않음.
    /// </summary>
    public void EnterNode()
    {
        if (selectedNode != null)
        {
            bool isCombatNode = selectedNode.eventType == NodeEventType.Hostile || 
                                selectedNode.eventType == NodeEventType.Elite || 
                                selectedNode.eventType == NodeEventType.Boss;
            
            if (isCombatNode)
            {
                EventChoice battleChoice = null;
                if (selectedNode.nodeEvent != null && selectedNode.nodeEvent.choices != null)
                {
                    battleChoice = selectedNode.nodeEvent.choices.Find(c => c.triggersBattle);
                }

                if (battleChoice == null)
                {
                    battleChoice = new EventChoice
                    {
                        choiceText = "전투 시작",
                        triggersBattle = true
                    };
                }

                OnChoiceSelected(battleChoice);
                return;
            }
        }

        if (entryPanel != null) entryPanel.SetActive(false);
        if (dimPanel   != null) dimPanel.SetActive(false);

        pendingMove = true;
        FinalizeNodeTransition(selectedNode);
        ExploreSaveData.Save(this, inBattle: false);
        GameManager.instance?.SaveProgress();
        InitNodeStates();
    }

    /// <summary>결과창 닫기. 이 시점에 실제 노드 이동 수행.</summary>
    public void CloseResult()
    {
        bool wasFinished = currentNode != null && currentNode.layerIndex == layers.Count - 1;

        if (resultPanel != null) resultPanel.SetActive(false);
        SetDim(0f, 0.2f);

        if (wasFinished || (resultUIText != null && resultUIText.text.Contains("탐사 완료")))
        {
            HandleExplorationEnd();
            return;
        }

        if (pendingMove && selectedNode != null && selectedNode != currentNode)
            FinalizeNodeTransition(selectedNode);

        pendingMove = false;
        ExploreSaveData.Save(this, inBattle: false);
        GameManager.instance?.SaveProgress();
        InitNodeStates();
    }

    /// <summary>No 버튼 (취소).</summary>
    public void CancelNode()
    {
        if (entryPanel != null) entryPanel.SetActive(false);
        SetDim(0f, 0.2f);
        selectedNode = null;
    }

    void HandleExplorationEnd()
    {
        ExploreSaveData.Clear();

        if (resetMapOnReturn)
            RestartExploration();
        else
        {
            if (BaseManager.instance != null && BaseManager.instance.baseNodes.Count > 0)
            {
                FinalizeNodeTransition(BaseManager.instance.baseNodes[0]);
                InitNodeStates();
            }
        }
    }

    // ─────────────────────────────────────────────────
    // 노드 이동 확정
    // ─────────────────────────────────────────────────

    void FinalizeNodeTransition(Node target)
    {
        if (target == null) return;

        // 이전 위치 처리
        if (currentNode != null)
        {
            currentNode.SetCurrentPosition(false);
            currentNode.RefreshUI();
        }

        currentNode = target;
        if (!pathTaken.Contains(currentNode)) pathTaken.Add(currentNode);

        currentNode.Visit();
        currentNode.SetCurrentPosition(true);
        currentNode.state = RegionState.Cleared;

        // 거점 건설 조건 체크 (현재 노드 도착 직후)
        if (BaseManager.instance != null)
            BaseManager.instance.RecalculateRegionStates();

        // 아이콘 이동
        if (playerIcon != null)
            StartCoroutine(MoveIcon(currentNode.GetComponent<RectTransform>().anchoredPosition));

        PanToNode(currentNode.GetComponent<RectTransform>());

        // 보스 도달 시 자동 탐사 완료
        if (currentNode.eventType == NodeEventType.Boss && currentNode.isVisited)
            ShowFinalSettlement();
    }

    IEnumerator MoveIcon(Vector2 target)
    {
        if (playerIcon == null) yield break;
        Vector2 start = playerIcon.anchoredPosition;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            playerIcon.anchoredPosition = Vector2.Lerp(start, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            yield return null;
        }
        playerIcon.anchoredPosition = target;
    }

    // ─────────────────────────────────────────────────
    // 결과 표시
    // ─────────────────────────────────────────────────

    void ShowResult(string text)
    {
        if (resultPanel == null) return;
        resultPanel.SetActive(true);
        resultPanel.transform.SetAsLastSibling();
        resultPanel.transform.localScale = Vector3.one * 0.85f;
        StartCoroutine(AnimateScale(resultPanel.transform, Vector3.one, 0.18f));

        if (resultUIText != null)
        {
            resultUIText.text             = text;
            resultUIText.enableAutoSizing = true;
            resultUIText.fontSizeMin      = 13;
            resultUIText.fontSizeMax      = 22;
        }
    }

    // ─────────────────────────────────────────────────
    // 탐사 종료 / 강제 귀환
    // ─────────────────────────────────────────────────

    void ShowFinalSettlement()
    {
        if (resultPanel == null || resultUIText == null) return;
        resultPanel.SetActive(true);
        resultPanel.transform.SetAsLastSibling();

        string summary = "<size=120%><color=#88FF88>탐사 완료!</color></size>\n\n";
        summary += $"획득 물자:  {Mathf.Max(0, totalMaterialGained)}\n";
        summary += $"획득 식량:  {Mathf.Max(0, totalFoodGained)}\n";
        summary += $"획득 기록:  {Mathf.Max(0, totalRecordGained)}\n\n";
        summary += "안전하게 거점으로 귀환합니다.";
        resultUIText.text = summary;
    }

    void ForceReturnToBase()
    {
        ShowResult("<color=#FF4444>식량 소진!\n강제 귀환합니다.</color>");
        pendingMove = false;
        selectedNode = null;
        if (BaseManager.instance != null && BaseManager.instance.baseNodes.Count > 0)
            FinalizeNodeTransition(BaseManager.instance.baseNodes[0]);
        else if (layers.Count > 0 && layers[0].Count > 0)
            FinalizeNodeTransition(layers[0][0]);
    }

    // ─────────────────────────────────────────────────
    // GameManager 동기화
    // ─────────────────────────────────────────────────

    void SyncToGameManager()
    {
        if (GameManager.instance == null) return;
        GameManager.instance.material = material;
        GameManager.instance.food     = food;
        GameManager.instance.record   = record;
        GameManager.instance.SaveProgress();
    }

    // ─────────────────────────────────────────────────
    // 유틸
    // ─────────────────────────────────────────────────

    IEnumerator AnimateScale(Transform target, Vector3 targetScale, float dur)
    {
        Vector3 start = target.localScale;
        float   t     = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(dur, 0.01f);
            target.localScale = Vector3.Lerp(start, targetScale, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        target.localScale = targetScale;
    }

    void ToggleExploreSceneActive(bool active)
    {
        if (!active)
        {
            hiddenObjects.Clear();
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach(var c in cameras) if(c.gameObject.scene == gameObject.scene && c.gameObject.activeSelf) { hiddenObjects.Add(c.gameObject); c.gameObject.SetActive(false); }

            var eventSystems = Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);
            foreach(var ev in eventSystems) if(ev.gameObject.scene == gameObject.scene && ev.gameObject.activeSelf) { hiddenObjects.Add(ev.gameObject); ev.gameObject.SetActive(false); }

            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach(var c in canvases) if(c.gameObject.scene == gameObject.scene && c.gameObject.activeSelf) { hiddenObjects.Add(c.gameObject); c.gameObject.SetActive(false); }
        }
        else
        {
            foreach(var obj in hiddenObjects)
            {
                if(obj != null) obj.SetActive(true);
            }
            hiddenObjects.Clear();
        }
    }
}



