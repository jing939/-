using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 거점(Base) 관리자.
/// 거점 설치/파괴, 탐사 가능 범위 제공.
/// ExploreManager.InitNodeStates()에서 거점 범위를 참고해 Available 노드 결정.
/// </summary>
public class BaseManager : MonoBehaviour
{
    public static BaseManager instance;

    public List<Node> baseNodes = new List<Node>();

    [Tooltip("거점에서 탐사 가능한 노드 거리 (레이어 수 기준)")]
    public int baseExplorationRange = 4;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void BuildBase(Node node)
    {
        node.hasBase = true;
        if (!baseNodes.Contains(node))
            baseNodes.Add(node);

        RecalculateRegionStates();
    }

    public void DestroyBase(Node node)
    {
        if (!baseNodes.Contains(node)) return;
        node.hasBase = false;
        baseNodes.Remove(node);
        RecalculateRegionStates();
    }

    /// <summary>
    /// 거점 기반 탐사 범위 재계산.
    /// ExploreManager.InitNodeStates()를 호출해 상태 갱신을 위임.
    /// </summary>
    public void RecalculateRegionStates()
    {
        ExploreManager em = ExploreManager.instance;
        if (em == null) return;

        // ExploreManager의 InitNodeStates가 BaseManager.baseNodes를 참조해
        // 직접 범위 계산을 수행하므로 여기선 단순히 호출만 함
        em.UpdateNodeStates();
    }
}
