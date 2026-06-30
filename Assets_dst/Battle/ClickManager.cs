using UnityEngine;

public class ClickManager : MonoBehaviour
{
    public BattleManager battle;
    public BattleUIManager battleUI;

    GameObject lastEnemy;

    void Awake()
    {
        if (battle == null) battle = BattleManager.instance;
        if (battle == null) battle = FindFirstObjectByType<BattleManager>();

        if (battleUI == null) battleUI = FindFirstObjectByType<BattleUIManager>();
    }

    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            // UI를 클릭했을 경우 클릭 처리를 무시 (스탯창을 클릭했을 때 꺼지지 않도록)
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector2 pos2D = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            GameObject clickedObject = null;

            // 1. 2D 콜라이더 체크
            Collider2D[] hits2D = Physics2D.OverlapPointAll(pos2D);
            foreach (var hitInfo in hits2D)
            {
                if (hitInfo.CompareTag("Player") || hitInfo.CompareTag("Enemy") || hitInfo.GetComponent<Enemy>() != null || hitInfo.GetComponent<PlayerMove>() != null || hitInfo.GetComponent<RangedPlayerMove>() != null)
                {
                    clickedObject = hitInfo.gameObject;
                    break;
                }
            }
            if (clickedObject == null && hits2D.Length > 0) clickedObject = hits2D[0].gameObject;

            // 2. 3D 콜라이더 체크 (2D에서 못 찾았을 경우, BoxCollider 등 3D 컴포넌트 처리)
            if (clickedObject == null)
            {
                RaycastHit[] hits3D = Physics.RaycastAll(ray, 100f);
                foreach (var hitInfo in hits3D)
                {
                    if (hitInfo.collider.CompareTag("Player") || hitInfo.collider.CompareTag("Enemy") || hitInfo.collider.GetComponent<Enemy>() != null || hitInfo.collider.GetComponent<PlayerMove>() != null || hitInfo.collider.GetComponent<RangedPlayerMove>() != null)
                    {
                        clickedObject = hitInfo.collider.gameObject;
                        break;
                    }
                }
                if (clickedObject == null && hits3D.Length > 0) clickedObject = hits3D[0].collider.gameObject;
            }

            if(clickedObject != null)
            {
                HandleClick(clickedObject);
            }
            else
            {
                // 빈공간 클릭 시 스탯창 닫기
                if (battleUI != null && battleUI.state == BattleUIManager.BattleState.Idle && battleUI.statsUIManager != null)
                {
                    battleUI.statsUIManager.HideAllStats();
                }
            }
        }
    }

    public void HandleClick(GameObject go)
    {
        if (battleUI == null)
        {
            Debug.LogWarning("[ClickManager] battleUI is NULL (BattleUIManager가 필요합니다)");
            return;
        }

        // 상태에 따라 클릭 동작 분기
        var state = battleUI.state;

        bool isPlayer = go.CompareTag("Player") || go.GetComponent<PlayerMove>() != null || go.GetComponent<RangedPlayerMove>() != null;
        bool isEnemy = go.CompareTag("Enemy") || go.GetComponent<Enemy>() != null;

        // Idle 상태: StatsUIManager 에 클릭 정보 전달 후 종료
        if (state == BattleUIManager.BattleState.Idle)
        {
            Debug.Log("[ClickManager] Idle 상태: 스탯창 활성화를 시도합니다.");
            if (battleUI.statsUIManager != null)
            {
                if (isPlayer)
                {
                    battleUI.statsUIManager.ShowCharacterStats(go);
                }
                else if (isEnemy)
                {
                    battleUI.statsUIManager.ShowMonsterStats(go);
                }
                else
                {
                    // 플레이어나 몬스터가 아닌 다른 게임오브젝트 클릭 시 스탯창 닫기
                    battleUI.statsUIManager.HideAllStats();
                }
            }
            return; // 전투 시스템 처리 중단
        }

        // 전투 중 상태 로직
        if (isPlayer)
        {
            if (state == BattleUIManager.BattleState.SelectPlayer)
            {
                battleUI.OnPlayerClicked(go);
            }
            else
            {
                Debug.Log($"[ClickManager] state={state} 에서 Player 클릭 무시");
            }
        }
        else if (isEnemy)
        {
            Enemy enemy = go.GetComponent<Enemy>();
            if (enemy == null) return;

            if (state == BattleUIManager.BattleState.SelectTarget)
            {
                // 이전 선택 몬스터 색상 복원
                if(lastEnemy != null)
                {
                    SpriteRenderer lastSr = lastEnemy.GetComponentInChildren<SpriteRenderer>();
                    if(lastSr != null) lastSr.color = Color.white;
                }

                lastEnemy = go;

                // 새 몬스터 빨간색 하이라이트
                SpriteRenderer sr = lastEnemy.GetComponentInChildren<SpriteRenderer>();
                if(sr != null) sr.color = Color.red;

                battleUI.OnMonsterClicked(enemy);
            }
            else
            {
                Debug.Log($"[ClickManager] state={state} 에서 Enemy 클릭 무시");
            }
        }
    }
}
