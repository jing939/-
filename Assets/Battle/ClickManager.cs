using UnityEngine;
using UnityEngine.EventSystems;

public class ClickManager : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.zero);

            if (hit.collider != null)
            {
                // 캐릭터나 몬스터를 클릭한 경우엔 각 오브젝트의 OnMouseDown이 처리하도록 무시합니다.
                if (hit.collider.GetComponent<Enemy>() != null || 
                    hit.collider.GetComponent<PlayerMove>() != null || 
                    hit.collider.GetComponent<RangedPlayerMove>() != null ||
                    hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player"))
                {
                    return; 
                }
                else
                {
                    // 허공이 아닌 다른 엉뚱한 오브젝트 클릭 시 스탯창 닫기
                    if (StatsUIManager.instance != null) StatsUIManager.instance.HideAll();
                }
            }
            else
            {
                if (StatsUIManager.instance != null) StatsUIManager.instance.HideAll();
            }
        }
    }
}