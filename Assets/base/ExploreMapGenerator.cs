using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 탐사 맵 생성 전담 클래스.
/// 림버스 거울던전 방식: 레이어(층) 기반, 앞으로만 진행.
/// connectedNodes는 단방향 (현재층 → 다음층) 만 저장.
/// </summary>
public static class ExploreMapGenerator
{
    // ──────────────────────────────────────────────
    // 맵 생성
    // ──────────────────────────────────────────────

    public static void GenerateMap(ExploreManager manager, int? fixedSeed = null)
    {
        int seed = fixedSeed ?? Random.Range(0, int.MaxValue);
        manager.savedMapSeed = seed;
        var prevState = Random.state;
        Random.InitState(seed);

        int layerCount = Random.Range(manager.minLayer, manager.maxLayer + 1);
        float xGap       = 180f;
        float yGap       = 120f;
        float totalWidth = (layerCount - 1) * xGap;

        for (int i = 0; i < layerCount; i++)
        {
            manager.layers.Add(new List<Node>());

            int nodeCount;
            if (i == 0 || i == layerCount - 1)
                nodeCount = 1;
            else if (i == layerCount - 2)
                nodeCount = 2;
            else
                nodeCount = (i > 0 && manager.layers[i - 1].Count == 3) ? 2 : Random.Range(2, 4);

            for (int j = 0; j < nodeCount; j++)
            {
                GameObject    obj = Object.Instantiate(manager.nodePrefab, manager.nodesParent);
                RectTransform rt  = obj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(100f, 100f);
                rt.anchoredPosition = new Vector2(
                    (i * xGap) - (totalWidth / 2f),
                    (j - (nodeCount - 1) / 2f) * yGap
                );

                Node node       = obj.GetComponent<Node>();
                node.manager    = manager;
                node.layerIndex = i;

                AssignNodeData(node, i, layerCount);
                EventScenarioBuilder.GenerateRandomEvent(node, manager.usedEventTitles);
                manager.layers[i].Add(node);
            }
        }

        // MapDrag 범위 설정
        if (manager.mapDragCache != null)
        {
            float half = totalWidth / 2f;
            manager.mapDragCache.minX = -(half + 400f);
            manager.mapDragCache.maxX =  (half + 400f);
        }

        // 단방향 연결 (cur → next 방향만)
        ConnectNodes(manager);

        Random.state = prevState;
    }

    // ──────────────────────────────────────────────
    // 노드 데이터 할당
    // ──────────────────────────────────────────────

    static void AssignNodeData(Node node, int i, int layerCount)
    {
        // 시작 노드
        if (i == 0)
        {
            node.eventType  = NodeEventType.BaseCamp;
            node.regionName = "A구역";
            node.difficulty = RegionDifficulty.Easy;
            return;
        }

        // 보스 노드
        if (i == layerCount - 1)
        {
            node.eventType  = NodeEventType.Boss;
            node.difficulty = RegionDifficulty.Extreme;
            node.regionName = "최종 구역";
            return;
        }

        // 지역 이름 & 난이도 (층 기반)
        string[] lowTier     = { "B구역", "M구역", "K구역" };
        string[] midTier     = { "C구역", "E구역", "F구역", "O구역" };
        string[] highTier    = { "D구역", "G구역", "H구역", "P구역" };
        string[] extremeTier = { "I구역", "J구역", "L구역" };

        if      (i <= 2) { node.regionName = lowTier    [Random.Range(0, lowTier.Length)];     node.difficulty = RegionDifficulty.Easy; }
        else if (i <= 5) { node.regionName = midTier    [Random.Range(0, midTier.Length)];     node.difficulty = RegionDifficulty.Normal; }
        else if (i <= 8) { node.regionName = highTier   [Random.Range(0, highTier.Length)];    node.difficulty = RegionDifficulty.Hard; }
        else             { node.regionName = extremeTier[Random.Range(0, extremeTier.Length)]; node.difficulty = RegionDifficulty.Extreme; }

        // 이벤트 타입 가중치 (깊이에 따라 전투 비중 증가)
        float t         = (float)i / (layerCount - 1);
        float wResource = Mathf.Lerp(5f, 1f, t);
        float wHostile  = Mathf.Lerp(2f, 7f, t);
        float wElite    = Mathf.Lerp(0f, 3f, t);
        float wNeutral  = Mathf.Lerp(3f, 2f, t);
        float wRecruit  = 1.5f;
        float total     = wResource + wHostile + wElite + wNeutral + wRecruit;
        float roll      = Random.Range(0f, total);

        if      (roll < wResource)                             node.eventType = NodeEventType.Resource;
        else if (roll < wResource + wHostile)                  node.eventType = NodeEventType.Hostile;
        else if (roll < wResource + wHostile + wElite)         node.eventType = NodeEventType.Elite;
        else if (roll < wResource + wHostile + wElite + wNeutral) node.eventType = NodeEventType.Neutral;
        else                                                   node.eventType = NodeEventType.Recruit;

        node.regionID = $"R_{i}_{node.regionName}_{Random.Range(0, 9999)}";
    }

    // ──────────────────────────────────────────────
    // 단방향 노드 연결 (cur → next 방향만)
    // ──────────────────────────────────────────────

    static void ConnectNodes(ExploreManager manager)
    {
        for (int i = 0; i < manager.layers.Count - 1; i++)
        {
            var cur  = manager.layers[i];
            var next = manager.layers[i + 1];
            int m = cur.Count, n = next.Count;

            // 각 현재층 노드를 다음층 노드와 연결 (최소 1개 보장)
            for (int a = 0; a < m; a++)
            {
                int b = Mathf.RoundToInt((float)a / Mathf.Max(1, m - 1) * (n - 1));
                ConnectForward(cur[a], next[b]);
            }
            // 다음층 노드가 누락되지 않도록 역방향으로도 최소 연결
            for (int b = 0; b < n; b++)
            {
                int a = Mathf.RoundToInt((float)b / Mathf.Max(1, n - 1) * (m - 1));
                ConnectForward(cur[a], next[b]);
            }

            // 랜덤 추가 분기 (흥미로운 경로 변화)
            for (int a = 0; a < m; a++)
            {
                if (Random.value > 0.5f)
                {
                    int b = Mathf.RoundToInt((float)a / Mathf.Max(1, m - 1) * (n - 1));
                    if (b + 1 < n) ConnectForward(cur[a], next[b + 1]);
                    if (b - 1 >= 0) ConnectForward(cur[a], next[b - 1]);
                }
            }
        }
    }

    /// <summary>단방향 연결: a → b (b에서 a로는 연결하지 않음)</summary>
    static void ConnectForward(Node a, Node b)
    {
        if (!a.connectedNodes.Contains(b))
            a.connectedNodes.Add(b);
        // ※ 역방향(b→a) 연결 안 함 → 앞으로만 이동 가능
    }

    // ──────────────────────────────────────────────
    // 선 그리기
    // ──────────────────────────────────────────────

    public static void DrawLines(ExploreManager manager)
    {
        if (manager.CurrentNode == null) return;

        int lineIndex = 0;

        for (int i = 0; i < manager.layers.Count - 1; i++)
        {
            foreach (Node n in manager.layers[i])
            {
                foreach (Node next in n.connectedNodes)
                {
                    // 단방향이므로 next.layerIndex 체크 불필요하지만 안전장치
                    if (next.layerIndex <= n.layerIndex) continue;

                    bool isPathLine     = manager.pathTaken.Contains(n) && manager.pathTaken.Contains(next);
                    bool isReachable    = (n == manager.CurrentNode && next.state == RegionState.Available);

                    Color lineColor;
                    float thickness;

                    if (isPathLine)
                    {
                        lineColor = new Color(0.20f, 0.90f, 0.90f, 1.00f);  // 네온 청록 - 지나온 경로
                        thickness = 10f;
                    }
                    else if (isReachable)
                    {
                        lineColor = new Color(0.95f, 0.85f, 0.20f, 0.95f);  // 네온 노랑 - 이동 가능
                        thickness = 6f;
                    }
                    else
                    {
                        lineColor = new Color(0.35f, 0.35f, 0.45f, 0.30f);  // 흐린 회색 - 잠금
                        thickness = 2f;
                    }

                    CreateLine(manager, n, next, lineColor, thickness, ref lineIndex);
                }
            }
        }

        // 사용하지 않는 남은 라인은 비활성화 처리
        int totalChildren = manager.linesParent.childCount;
        for (int i = lineIndex; i < totalChildren; i++)
        {
            manager.linesParent.GetChild(i).gameObject.SetActive(false);
        }
    }

    static void CreateLine(ExploreManager manager, Node a, Node b, Color color, float thickness, ref int lineIndex)
    {
        Vector2 posA = a.GetComponent<RectTransform>().anchoredPosition;
        Vector2 posB = b.GetComponent<RectTransform>().anchoredPosition;
        Vector2 dir  = posB - posA;
        float   dist = dir.magnitude;
        if (dist < 1f) return;

        float   nodeRadius = 32f;
        float   lineLen    = Mathf.Max(0f, dist - nodeRadius * 2f);
        Vector2 center     = (posA + posB) * 0.5f;
        float   angle      = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        GameObject line;
        Image img;
        RectTransform rt;

        if (lineIndex < manager.linesParent.childCount)
        {
            line = manager.linesParent.GetChild(lineIndex).gameObject;
            line.SetActive(true);
            img  = line.GetComponent<Image>() ?? line.AddComponent<Image>();
            rt   = line.GetComponent<RectTransform>();
        }
        else
        {
            line = new GameObject("UILine");
            line.transform.SetParent(manager.linesParent, false);
            img  = line.AddComponent<Image>();
            rt   = line.GetComponent<RectTransform>();
        }

        // 형제 순서 일치화
        line.transform.SetSiblingIndex(lineIndex);

        img.color         = color;
        img.raycastTarget = false;

        rt.anchorMin      = new Vector2(0.5f, 0.5f);
        rt.anchorMax      = new Vector2(0.5f, 0.5f);
        rt.pivot          = new Vector2(0.5f, 0.5f);
        rt.sizeDelta      = new Vector2(lineLen, thickness);
        rt.anchoredPosition = center;
        rt.localRotation  = Quaternion.Euler(0f, 0f, angle);

        lineIndex++;
    }
}

