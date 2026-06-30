using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleUIManager : MonoBehaviour
{
    public static BattleUIManager instance;

    public enum BattleState
    {
        Idle,
        SelectPlayer,
        SelectSkill,
        SelectTarget,
        ReadyToAttack,
        Attacking
    }

    [Header("References")]
    public BattleManager battleManager;
    public StatsUIManager statsUIManager;

    [Header("UI")]
    public Canvas canvas;
    public Button battleButton;          // 화면 아래 "전투 시작" 버튼 부활!
    public GameObject skillPanel;        // 스킬 선택 UI 루트
    public Button skill1Button;
    public Button skill2Button;
    public Button skill3Button;          // 테스트용 스킬 3 버튼 (주사위 1)
    public Button skill4Button;          // 테스트용 즉사기 버틍 (주사위 100~200)
    public GameObject clearTextObj;      // 승리(Clear) 시 띄울 중앙 텍스트 오브젝트
    public GameObject defeatTextObj;     // 패배(Defeat) 시 띄울 중앙 텍스트 오브젝트
    
    [Header("Slot Management")]
    public FloatingSkillSlot editingSlot; // 현재 설정 중인 슬롯

    [Header("Monsters")]
    public List<Enemy> monsters = new List<Enemy>();
    public Color selectedColor = Color.red;
    public Color normalColor = Color.white;

    [Header("State")]
    public BattleState state = BattleState.Idle;
    public Enemy selectedMonster;
    public BattleManager.SkillId selectedSkill = BattleManager.SkillId.None;

    [Header("Tuning")]
    public float attackEndDelay = 0.2f;

    [Header("Clash UI (합 시스템)")]
    public GameObject clashPanel;
    public TextMeshProUGUI playerClashText;
    public TextMeshProUGUI enemyClashText;
    public TextMeshProUGUI clashResultText;

    [Header("Dynamic Coin UI")]
    public Transform playerCoinContainer; 
    public Transform enemyCoinContainer;
    List<Image> pCoinImages = new List<Image>();
    List<Image> eCoinImages = new List<Image>();
    
    public enum CoinState { Ready, Head, Tail, Broken }

    Camera cam;

    void Awake()
    {
        instance = this;
        cam = Camera.main;
        if (battleManager == null) battleManager = BattleManager.instance;
        if (battleManager == null) battleManager = FindFirstObjectByType<BattleManager>();
        if (statsUIManager == null) statsUIManager = FindFirstObjectByType<StatsUIManager>();
    }

    void OnEnable()
    {
        HookButtons(true);
    }

    void OnDisable()
    {
        HookButtons(false);
    }

    void Start()
    {
        if (skillPanel != null) skillPanel.SetActive(false);
        if (clearTextObj != null) clearTextObj.SetActive(false);
        if (defeatTextObj != null) defeatTextObj.SetActive(false);
        ShowClashUI(false);
        SetState(BattleState.Idle);
        CleanupMonsterList();
    }

    public void ShowClashUI(bool show)
    {
        if (clashPanel != null) clashPanel.SetActive(show);
        if (show && clashResultText != null) clashResultText.text = "합 지정 중...";
    }

    public void UpdateClashNumbers(int pRoll, int eRoll)
    {
        if (playerClashText != null) playerClashText.text = pRoll.ToString();
        if (enemyClashText != null) enemyClashText.text = eRoll.ToString();
    }

    public void SetClashResultText(string msg)
    {
        if (clashResultText != null) clashResultText.text = msg;
    }

    void HookButtons(bool hook)
    {
        if (battleButton != null)
        {
            if (hook) battleButton.onClick.AddListener(OnBattleButtonClicked);
            else battleButton.onClick.RemoveListener(OnBattleButtonClicked);
        }

        if (skill1Button != null)
        {
            if (hook) skill1Button.onClick.AddListener(OnSkill1Clicked);
            else skill1Button.onClick.RemoveListener(OnSkill1Clicked);
        }

        if (skill2Button != null)
        {
            if (hook) skill2Button.onClick.AddListener(OnSkill2Clicked);
            else skill2Button.onClick.RemoveListener(OnSkill2Clicked);
        }

        if (skill3Button != null)
        {
            if (hook) skill3Button.onClick.AddListener(OnSkill3Clicked);
            else skill3Button.onClick.RemoveListener(OnSkill3Clicked);
        }

        if (skill4Button != null)
        {
            if (hook) skill4Button.onClick.AddListener(OnSkill4Clicked);
            else skill4Button.onClick.RemoveListener(OnSkill4Clicked);
        }
    }

    public void SetState(BattleState next)
    {
        state = next;
        Debug.Log($"[BattleUIManager] State -> {state}");

        if (skillPanel != null)
        {
            skillPanel.SetActive(state == BattleState.SelectSkill);
        }

        // Idle일 때만 StatsUIManager가 클릭 처리하도록 동기화
        if (statsUIManager != null)
        {
            statsUIManager.SetState(state == BattleState.Idle
                ? StatsUIManager.GameState.Idle
                : StatsUIManager.GameState.Battle);
        }
    }

    // =========================
    // Slot UI callbacks
    // =========================

    public void OpenSkillSelectionForSlot(FloatingSkillSlot slot)
    {
        if (state == BattleState.Attacking) return;
        
        ClearSelection();
        editingSlot = slot;
        
        // 주인공 지정 후 스킬 패널 표시
        if (battleManager != null) battleManager.activePlayer = slot.ownerPlayer;
        SetState(BattleState.SelectSkill);
    }

    public void ReturnToIdle()
    {
        ClearSelection();
        selectedSkill = BattleManager.SkillId.None;
        SetState(BattleState.Idle);

        CheckWinCondition();
        CheckDefeatCondition();
    }

    public void CheckWinCondition()
    {
        // 리스트 수동 관리 오류를 원천 차단하기 위해 씬에 존재하는 살아있는 Enemy를 실시간으로 직접 셉니다!
        var aliveEnemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        
        if (aliveEnemies.Length == 0)
        {
            Debug.Log("[BattleUIManager] 살아있는 적이 0마리 입니다. CLEAR!!");
            if (clearTextObj != null)
            {
                clearTextObj.SetActive(true);
            }
        }
    }

    public void CheckDefeatCondition()
    {
        var pMoves = FindObjectsByType<PlayerMove>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var rpMoves = FindObjectsByType<RangedPlayerMove>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        
        bool allDead = true;
        foreach (var p in pMoves) if (!p.isDead) allDead = false;
        foreach (var rp in rpMoves) if (!rp.isDead) allDead = false;
        
        // 씬에 플레이어들이 아예 없는건 무시. 누군가 있었는데 다 죽었을 때만 패배.
        if ((pMoves.Length > 0 || rpMoves.Length > 0) && allDead)
        {
            Debug.Log("[BattleUIManager] 우리편 전멸! DEFEAT!!");
            if (defeatTextObj != null)
            {
                defeatTextObj.SetActive(true);
            }
        }
    }

    public void OnSkill1Clicked()
    {
        if (!CanUseSkill() || editingSlot == null) return;

        editingSlot.AssignSkill(BattleManager.SkillId.Skill1, "스킬 1");
        ApplySelectedSkillToPlayer("스킬 1 선택됨");
        editingSlot = null; // 수정 완료 (이후 드래그로 타겟 지정)
        SetState(BattleState.Idle);
    }

    public void OnSkill2Clicked()
    {
        if (!CanUseSkill() || editingSlot == null) return;

        editingSlot.AssignSkill(BattleManager.SkillId.Skill2, "스킬 2");
        ApplySelectedSkillToPlayer("스킬 2 선택됨");
        editingSlot = null; // 수정 완료 (이후 드래그로 타겟 지정)
        SetState(BattleState.Idle);
    }

    public void OnSkill3Clicked()
    {
        if (!CanUseSkill() || editingSlot == null) return;

        editingSlot.AssignSkill(BattleManager.SkillId.Skill3, "자폭 스킬");
        ApplySelectedSkillToPlayer("일부러 맞아주기...");
        editingSlot = null; // 수정 완료
        SetState(BattleState.Idle);
    }

    public void OnSkill4Clicked()
    {
        if (!CanUseSkill() || editingSlot == null) return;

        editingSlot.AssignSkill(BattleManager.SkillId.Skill4, "필살기(100~200)");
        ApplySelectedSkillToPlayer("차원을 가르는 필살기!");
        editingSlot = null; // 수정 완료
        SetState(BattleState.Idle);
    }

    bool CanUseSkill()
    {
        if (battleManager == null)
        {
            Debug.LogError("[BattleUIManager] battleManager is NULL");
            return false;
        }

        if (state != BattleState.SelectSkill)
        {
            Debug.LogWarning($"[BattleUIManager] 스킬 사용 불가 상태: {state}");
            return false;
        }

        return true;
    }

    void ApplySelectedSkillToPlayer(string skillName)
    {
        if (battleManager == null) return;
        
        GameObject activeObj = battleManager.activePlayer;
        if (activeObj == null)
        {
            Debug.LogWarning("[BattleUIManager] activePlayer가 NULL 이라 스킬 텍스트를 표시할 수 없습니다.");
            return;
        }

        PlayerMove p = activeObj.GetComponent<PlayerMove>();
        RangedPlayerMove rp = activeObj.GetComponent<RangedPlayerMove>();

        if (p != null) p.ShowSkillName(skillName);
        else if (rp != null) rp.ShowSkillName(skillName);
    }

    // =========================
    // Player / Monster 선택 (ClickManager에서 호출)
    // =========================

    public void OnPlayerClicked(GameObject playerObj)
    {
        if (state == BattleState.Idle)
        {
            // Idle 클릭은 StatsUIManager만 처리
            return;
        }

        if (state == BattleState.SelectPlayer)
        {
            Debug.Log("[BattleUIManager] SelectPlayer: 플레이어 클릭 -> 스킬 패널 표시");
            if (battleManager != null) battleManager.activePlayer = playerObj;
            SetState(BattleState.SelectSkill);
            return;
        }

        Debug.Log($"[BattleUIManager] state={state} 에서 플레이어 클릭 무시");
    }

    public void OnMonsterClicked(Enemy enemy)
    {
        if (enemy == null || state != BattleState.SelectTarget || editingSlot == null) return;

        // 리스트 검증
        CleanupMonsterList();
        if (monsters != null && monsters.Count > 0 && monsters.Contains(enemy) == false) return;

        // 슬롯에 타겟 넘겨주기
        editingSlot.AssignTarget(enemy);
        editingSlot = null; // 하나의 동작 지정 끝!
        
        SetState(BattleState.Idle); // 다음 슬롯 클릭을 기다리기 위해 화면 비우기

        CheckAndStartAutoCombat();
    }

    public void CheckAndStartAutoCombat()
    {
        // 자동 전투 폐지! (FloatingSkillSlot이 이걸 호출하더라도 아무 짓도 안 함)
        // 유저가 모든 세팅을 마치고 웅장하게 '전투 버튼'을 직접 눌렀을 때만 출발하게 바꿨습니다.
    }

    public void OnBattleButtonClicked()
    {
        if (state == BattleState.Attacking || battleManager == null) return;

        var allSlots = FindObjectsByType<FloatingSkillSlot>(FindObjectsSortMode.None);
        List<FloatingSkillSlot> activeSlots = new List<FloatingSkillSlot>();
        int requiredSlotCount = 0;
        
        foreach (var s in allSlots)
        {
            bool ownerDead = false;
            if (s.ownerPlayer != null)
            {
                PlayerMove p = s.ownerPlayer.GetComponent<PlayerMove>();
                if (p != null && p.isDead) ownerDead = true;
                RangedPlayerMove rp = s.ownerPlayer.GetComponent<RangedPlayerMove>();
                if (rp != null && rp.isDead) ownerDead = true;
            }
            else
            {
                ownerDead = true; // 플레이어 삭제됨
            }

            // 살아있는 플레이어의 슬롯만 계산에 포함! 죽은 애들은 배제.
            if (!ownerDead)
            {
                requiredSlotCount++;
                if (s.assignedSkill != BattleManager.SkillId.None && s.assignedTarget != null)
                {
                    activeSlots.Add(s);
                }
            }
        }

        // 유저 요청: "살아있는 모든 캐릭터의 슬롯이 다 장전되어야만" 전투 시작!
        if (requiredSlotCount > 0 && activeSlots.Count == requiredSlotCount)
        {
            SetState(BattleState.Attacking); // 다중 클릭 방지
            StartCoroutine(battleManager.ExecuteActionQueue(activeSlots));
        }
        else
        {
            Debug.LogWarning("[BattleUIManager] 모든 스킬칸에 스킬과 몬스터(타겟)를 조준해야 전투를 시작할 수 있습니다!");
        }
    }

    void ClearSelection()
    {
        SetMonsterColor(selectedMonster, normalColor);
        selectedMonster = null;
    }

    void CleanupMonsterList()
    {
        if (monsters == null) return;
        monsters.RemoveAll(m => m == null);
    }

    void SetMonsterColor(Enemy enemy, Color color)
    {
        if (enemy == null) return;
        SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
        if (sr == null) sr = enemy.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = color;
    }

    // =========================
    // Dynamic Coin UI System
    // =========================
    
    public void SetupClashCoins(int pCount, int eCount)
    {
        EnsureCoinContainers();

        // 플레이어 코인 초기화
        foreach(var img in pCoinImages) { if (img != null) Destroy(img.gameObject); }
        pCoinImages.Clear();
        for(int i=0; i<pCount; i++)
        {
            pCoinImages.Add(CreateCoinImage(playerCoinContainer));
        }

        // 적 코인 초기화
        foreach(var img in eCoinImages) { if (img != null) Destroy(img.gameObject); }
        eCoinImages.Clear();
        for(int i=0; i<eCount; i++)
        {
            eCoinImages.Add(CreateCoinImage(enemyCoinContainer));
        }
    }

    public void UpdatePlayerCoin(int index, CoinState state) { ApplyCoinState(pCoinImages, index, state); }
    public void UpdateEnemyCoin(int index, CoinState state) { ApplyCoinState(eCoinImages, index, state); }

    void ApplyCoinState(List<Image> list, int index, CoinState state)
    {
        if (index < 0 || index >= list.Count) return;
        Image img = list[index];
        if (img == null) return;

        switch(state)
        {
            case CoinState.Ready: img.color = Color.black; break;             // 기본 스탠바이 (검은색)
            case CoinState.Head: img.color = new Color(1f, 0.8f, 0f); break;  // 앞면 (황금색)
            case CoinState.Tail: img.color = Color.gray; break;               // 뒷면 (회색)
            case CoinState.Broken: img.color = Color.clear; break;            // 깨짐 (투명 처리)
        }
    }

    Image CreateCoinImage(Transform parent)
    {
        GameObject obj = new GameObject("DynamicCoin", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(30, 30); // 코인 크기 30x30

        Image img = obj.GetComponent<Image>();
        img.color = Color.black; // 초기색상 Ready (검은색)
        
        return img;
    }

    void EnsureCoinContainers()
    {
        if (playerClashText == null || enemyClashText == null) return;

        if (playerCoinContainer == null)
        {
            GameObject pContainer = new GameObject("PlayerCoinContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            pContainer.transform.SetParent(playerClashText.transform.parent, false);
            
            RectTransform rt = pContainer.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(100f, -60f); // 텍스트 아래 공간

            HorizontalLayoutGroup layout = pContainer.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 15;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false; layout.childControlHeight = false;
            
            playerCoinContainer = pContainer.transform;
            Debug.Log("[BattleUIManager] Player Coin Container 자동 생성");
        }

        if (enemyCoinContainer == null)
        {
            GameObject eContainer = new GameObject("EnemyCoinContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            eContainer.transform.SetParent(enemyClashText.transform.parent, false);

            RectTransform rt = eContainer.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0.5f); rt.anchorMax = new Vector2(1, 0.5f);
            rt.anchoredPosition = new Vector2(-100f, -60f); 

            HorizontalLayoutGroup layout = eContainer.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 15;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false; layout.childControlHeight = false;

            enemyCoinContainer = eContainer.transform;
            Debug.Log("[BattleUIManager] Enemy Coin Container 자동 생성");
        }
    }
}

