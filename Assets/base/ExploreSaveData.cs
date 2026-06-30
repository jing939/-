using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 탐사 진행 상태를 디스크에 저장/복원하기 위한 데이터.
/// 전투 진입 전 스냅샷을 남기고, 승리 후 같은 위치로 복귀할 때 사용한다.
/// </summary>
[Serializable]
public class ExploreSaveData
{
    public int mapSeed;
    public int layerCount;
    public int minLayer;
    public int maxLayer;

    public int currentLayer;
    public int currentIndex;
    public int selectedLayer = -1;
    public int selectedIndex = -1;

    public bool pendingMove;
    public bool inBattle;

    public int material;
    public int food;
    public int record;

    public List<string> usedEventTitles = new List<string>();

    public List<NodeSaveEntry> nodes = new List<NodeSaveEntry>();

    const string SaveKey = "ExploreSaveData";

    [Serializable]
    public class NodeSaveEntry
    {
        public int layer;
        public int index;
        public int eventType;
        public int difficulty;
        public string regionName;
        public bool isVisited;
        public int state;
        public List<int> connectedIndices = new List<int>();
    }

    public static void Save(ExploreManager manager, bool inBattle)
    {
        if (manager == null || manager.layers == null || manager.layers.Count == 0) return;

        var data = new ExploreSaveData
        {
            mapSeed     = manager.savedMapSeed,
            layerCount  = manager.layers.Count,
            minLayer    = manager.minLayer,
            maxLayer    = manager.maxLayer,
            pendingMove = manager.IsPendingMove,
            inBattle    = inBattle,
            material    = manager.GetMaterial(),
            food        = manager.GetFood(),
            record      = manager.GetRecord(),
            usedEventTitles = new List<string>(manager.usedEventTitles)
        };

        if (manager.CurrentNode != null)
        {
            data.currentLayer = manager.CurrentNode.layerIndex;
            data.currentIndex = manager.layers[data.currentLayer].IndexOf(manager.CurrentNode);
        }

        Node selected = manager.GetSelectedNode();
        if (selected != null)
        {
            data.selectedLayer = selected.layerIndex;
            data.selectedIndex = manager.layers[data.selectedLayer].IndexOf(selected);
        }

        for (int li = 0; li < manager.layers.Count; li++)
        {
            for (int ni = 0; ni < manager.layers[li].Count; ni++)
            {
                Node node = manager.layers[li][ni];
                var entry = new NodeSaveEntry
                {
                    layer       = li,
                    index       = ni,
                    eventType   = (int)node.eventType,
                    difficulty  = (int)node.difficulty,
                    regionName  = node.regionName,
                    isVisited   = node.isVisited,
                    state       = (int)node.state
                };

                foreach (Node conn in node.connectedNodes)
                    entry.connectedIndices.Add(manager.layers[conn.layerIndex].IndexOf(conn));

                data.nodes.Add(entry);
            }
        }

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static bool TryLoad(out ExploreSaveData data)
    {
        data = null;
        if (!PlayerPrefs.HasKey(SaveKey)) return false;

        string json = PlayerPrefs.GetString(SaveKey);
        if (string.IsNullOrEmpty(json)) return false;

        data = JsonUtility.FromJson<ExploreSaveData>(json);
        return data != null && data.nodes != null && data.nodes.Count > 0;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }

    public static void ClearBattleFlag()
    {
        if (!TryLoad(out ExploreSaveData data)) return;
        data.inBattle = false;
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }
}
