using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class FloatingSkillSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Slot Data")]
    public int slotIndex; // 슬롯 순서 (0: 첫번째 타격, 1: 두번째 타격)
    public GameObject ownerPlayer; // 이 슬롯을 소유한 플레이어 오브젝트
    
    [Header("Current Assignments")]
    public BattleManager.SkillId assignedSkill = BattleManager.SkillId.None;
    public string cachedSkillName = "";
    public Enemy assignedTarget;

    [Header("UI References")]
    public Button slotButton;
    public TextMeshProUGUI slotText;
    
    [Header("Dynamic Coin UI")]
    public Transform coinContainer;
    System.Collections.Generic.List<Image> coinImages = new System.Collections.Generic.List<Image>();

    void Start()
    {
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClicked);
        }
        ClearSlot();
    }

    public void OnSlotClicked()
    {
        if (BattleUIManager.instance == null) return;
        BattleUIManager.instance.OpenSkillSelectionForSlot(this);
    }

    public void AssignSkill(BattleManager.SkillId skill, string skillName)
    {
        assignedSkill = skill;
        cachedSkillName = skillName;
        if (slotText != null) slotText.text = skillName + "\n(드래그!)";

        // 스킬 할당 시 UI에 장전된 코인 갯수 생성하기 
        if (BattleManager.instance != null)
        {
            BattleManager.instance.GetSkillInfo(skill, out int bP, out int cP, out int cCount, out string sNm);
            SetupSlotCoins(cCount);
        }
    }

    public void AssignTarget(Enemy enemy)
    {
        assignedTarget = enemy;
        if (slotText != null && enemy != null)
        {
            slotText.text = "준비완료!\n>> " + enemy.name;
        }
    }

    public void ClearSlot()
    {
        assignedSkill = BattleManager.SkillId.None;
        assignedTarget = null;
        cachedSkillName = "";
        if (slotText != null) slotText.text = "스킬칸 " + (slotIndex + 1) + "\n(클릭)";
        SetupSlotCoins(0); // 슬롯 비울 때 코인 지우기
    }

    void SetupSlotCoins(int count)
    {
        if (coinContainer == null)
        {
            if (slotText != null)
            {
                GameObject container = new GameObject("SlotCoinContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                container.transform.SetParent(slotText.transform.parent, false);
                
                RectTransform rt = container.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0, 25f); 

                HorizontalLayoutGroup layout = container.GetComponent<HorizontalLayoutGroup>();
                layout.spacing = 5; layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = false; layout.childControlHeight = false;
                
                coinContainer = container.transform;
            }
        }

        foreach(var img in coinImages) { if (img != null) Destroy(img.gameObject); }
        coinImages.Clear();

        if (coinContainer == null) return;

        for(int i = 0; i < count; i++)
        {
            GameObject obj = new GameObject("MiniCoin", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(coinContainer, false);
            
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(15, 15); // 작은 코인 15x15

            Image img = obj.GetComponent<Image>();
            img.color = Color.black; // 검은색으로 대기 코인 표시 (굴리기 전)
            coinImages.Add(img);
        }
    }

    LineRenderer dragLine;
    GameObject lineObj;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (assignedSkill == BattleManager.SkillId.None) return; // 스킬이 없으면 타겟팅 불가

        // 부드럽고 예쁜 타겟팅 선(LineRenderer) 생성
        lineObj = new GameObject("TargetingLine");
        dragLine = lineObj.AddComponent<LineRenderer>();
        dragLine.positionCount = 2;
        dragLine.startWidth = 0.05f;
        dragLine.endWidth = 0.02f;
        
        // 빨간색 스프라이트 전용 머티리얼 적용
        dragLine.material = new Material(Shader.Find("Sprites/Default"));
        dragLine.startColor = Color.red;
        dragLine.endColor = new Color(1f, 0f, 0f, 0.5f); // 끝부분은 살짝 투명하게

        dragLine.sortingOrder = 100; // 가장 위에 렌더링
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragLine != null)
        {
            // 선의 시작점은 이 슬롯 버튼의 위치
            dragLine.SetPosition(0, transform.position);
            
            // 선의 끝점은 마우스 위치 (2D 좌표 변환)
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0; // 2D 환경 맞춤
            dragLine.SetPosition(1, mouseWorldPos);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragLine != null)
        {
            Destroy(lineObj); // 허공에 놨든 맞췄든 무조건 화살표 지우기
        }

        if (assignedSkill == BattleManager.SkillId.None) return;

        // 마우스를 놓은 지점에 몬스터가 있는지 레이캐스트 검사
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit.collider != null)
        {
            Enemy targetEnemy = hit.collider.GetComponent<Enemy>();
            if (targetEnemy != null)
            {
                // 타겟을 맞췄다! (슬롯 장전 완료)
                AssignTarget(targetEnemy);
                Debug.Log($"[Targeting] 슬롯 {slotIndex} 타겟 지정 확정: {targetEnemy.name}");
                
                // 큐 검사해서 꽉 찼으면 자동 실행 시작!
                if (BattleUIManager.instance != null)
                {
                    BattleUIManager.instance.CheckAndStartAutoCombat();
                }
            }
        }
        else
        {
            // 허공에 놓은 경우 (타겟 취소)
            // 유저 요청: 스킬 지정은 그대로 남아있고, 다시 드래그해서 적을 쏠 기회를 준다!
            assignedTarget = null;
            if (slotText != null) slotText.text = cachedSkillName + "\n(드래그!)";
            Debug.Log($"[Targeting] 슬롯 {slotIndex} 조준 취소 (재시도 대기중).");
        }
    }
}
