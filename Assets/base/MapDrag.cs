using UnityEngine;

/// <summary>
/// 탐사 맵 우클릭 드래그 이동 스크립트.
/// 우클릭 드래그로 맵을 이동하고, 좌클릭은 노드 클릭에 사용된다.
/// </summary>
public class MapDrag : MonoBehaviour
{
    public RectTransform map;

    public float minX = -900f;
    public float maxX =  900f;

    Vector2 lastMouse;
    bool dragging;

    /// <summary>드래그 중 여부 (외부에서 참조 가능, 우클릭이므로 노드 좌클릭과 충돌 없음)</summary>
    public bool IsDragging => dragging;

    void Update()
    {
        if (Input.GetMouseButtonDown(1))  // 우클릭 시작
        {
            lastMouse = Input.mousePosition;
            dragging  = true;

            var em = ExploreManager.instance;
            if (em != null) em.StopPanning();
        }

        if (Input.GetMouseButtonUp(1))    // 우클릭 끝
        {
            dragging = false;
        }

        if (!dragging || map == null) return;

        Vector2 cur   = Input.mousePosition;
        Vector2 delta = cur - lastMouse;

        map.anchoredPosition += new Vector2(delta.x, 0f);
        float x = Mathf.Clamp(map.anchoredPosition.x, minX, maxX);
        map.anchoredPosition = new Vector2(x, map.anchoredPosition.y);

        lastMouse = cur;
    }
}