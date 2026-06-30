using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class FloatingSkillSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Slot Data")]
    public int slotIndex;
    public GameObject ownerPlayer;
    
    [Header("Current Assignments")]
    public BattleManager.SkillId assignedSkill = BattleManager.SkillId.None;
    public string cachedSkillName = "";
    public Enemy assignedTarget;

    [Header("UI References")]
    public Button slotButton;
    public TextMeshProUGUI slotText;

    [Header("Line Settings (선 굵기/모양 설정)")]
    public float lineWidth = 0.05f;       // 일반 선 굵기
    public float arrowBaseWidth = 0.25f;  // 화살표 넓이
    public float arrowHeadLength = 0.5f;  // 화살표 머리의 길이

    Camera cam;

    private LineRenderer targetLine;

    void Start()
    {
        cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();

        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClicked);
        }

        GameObject tLineObj = new GameObject("TargetLine");
        tLineObj.transform.SetParent(transform);
        targetLine = tLineObj.AddComponent<LineRenderer>();
        targetLine.positionCount = 2;
        targetLine.material = new Material(Shader.Find("Sprites/Default"));
        targetLine.startColor = new Color(1f, 0.5f, 0f, 0.8f); // 주황색
        targetLine.endColor = new Color(1f, 0.2f, 0f, 0.8f);
        targetLine.sortingOrder = 50;

        ClearSlot();
    }

    void Update()
    {
        // 설정된 플레이어(ownerPlayer)가 있으면 그 캐릭터의 머리 위를 실시간으로 따라다닙니다!
        if (ownerPlayer != null)
        {
            if (cam == null) { cam = Camera.main; return; }

            SpriteRenderer sr = ownerPlayer.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                // 캐릭터의 가장 높은 부분(머리)을 찾아서 화면 좌표로 변환
                Vector3 headPos = cam.WorldToScreenPoint(new Vector3(sr.bounds.center.x, sr.bounds.max.y, 0));
                
                // 캐릭터가 여러 개일 때 스킬 슬롯이 겹치지 않도록 간격을 줄여서 예쁘게 모아줍니다.
                float xOffset = 0f; // 슬롯이 항상 중앙에 오도록 오프셋 제거
                
                transform.position = headPos + new Vector3(xOffset, 40f, 0); // 머리 위쪽으로 띄움
            }
            else
            {
                // 스프라이트 렌더러가 없으면 그냥 중심점 기준
                Vector3 screenPos = cam.WorldToScreenPoint(ownerPlayer.transform.position);
                transform.position = screenPos + new Vector3(0, 60, 0);
            }
        }

        if (targetLine != null)
        {
            if (assignedTarget != null && assignedSkill != BattleManager.SkillId.None && dragLine == null)
            {
                targetLine.enabled = true;
                
                float depth = Mathf.Abs(cam.transform.position.z);
                Vector3 screenStart = transform.position;
                screenStart.z = depth;
                Vector3 startPos = cam.ScreenToWorldPoint(screenStart);
                startPos.z = 0;

                Vector3 targetPos = assignedTarget.transform.position;
                targetPos.z = 0;

                UpdateArrowCurve(targetLine, startPos, targetPos);
            }
            else
            {
                targetLine.enabled = false;
            }
        }
    }

    public void OnSlotClicked()
    {
        // 이제 슬롯을 눌렀을 때 스스로 변하지 않고, 팝업 패널을 띄워달라고 요청합니다.
        if (SkillSelectionUIManager.instance != null)
        {
            SkillSelectionUIManager.instance.OpenPanel(this);
        }
        else
        {
            Debug.LogError("씬에 SkillSelectionUIManager가 없습니다! 생성해주세요.");
        }
    }

    // [신규] 캐릭터(ownerPlayer)에서 스프라이트 가져오는 헬퍼 함수들
    private Sprite GetEmptySprite()
    {
        if (ownerPlayer == null) return null;
        var p = ownerPlayer.GetComponent<PlayerMove>();
        if (p != null) return p.emptySlotSprite;
        var rp = ownerPlayer.GetComponent<RangedPlayerMove>();
        if (rp != null) return rp.emptySlotSprite;
        return null;
    }

    private int GetSkillIndex(BattleManager.SkillId skill)
    {
        switch (skill)
        {
            case BattleManager.SkillId.Skill1: return 0;
            case BattleManager.SkillId.Skill2: return 1;
            case BattleManager.SkillId.Skill3: return 2;
            case BattleManager.SkillId.Skill4: return 3;
            default: return -1;
        }
    }

    private Sprite GetAssignedSprite()
    {
        if (ownerPlayer == null || assignedSkill == BattleManager.SkillId.None) return null;
        int idx = GetSkillIndex(assignedSkill);
        if (idx < 0) return null;

        var p = ownerPlayer.GetComponent<PlayerMove>();
        if (p != null && p.skills != null && idx < p.skills.Length) return p.skills[idx].assignedSlotSprite;
        
        var rp = ownerPlayer.GetComponent<RangedPlayerMove>();
        if (rp != null && rp.skills != null && idx < rp.skills.Length) return rp.skills[idx].assignedSlotSprite;
        
        return null;
    }

    private Sprite GetReadySprite()
    {
        if (ownerPlayer == null || assignedSkill == BattleManager.SkillId.None) return null;
        int idx = GetSkillIndex(assignedSkill);
        if (idx < 0) return null;

        var p = ownerPlayer.GetComponent<PlayerMove>();
        if (p != null && p.skills != null && idx < p.skills.Length) return p.skills[idx].readySlotSprite;
        
        var rp = ownerPlayer.GetComponent<RangedPlayerMove>();
        if (rp != null && rp.skills != null && idx < rp.skills.Length) return rp.skills[idx].readySlotSprite;
        
        return null;
    }

    public void AssignSkill(BattleManager.SkillId skill, string skillName)
    {
        assignedSkill = skill;
        cachedSkillName = skillName;
        if (slotText != null) slotText.text = ""; // 글씨 제거
        Sprite sp = GetAssignedSprite();
        if (slotButton != null && sp != null) slotButton.image.sprite = sp;
    }

    public void AssignTarget(Enemy enemy)
    {
        assignedTarget = enemy;
        if (slotText != null && enemy != null)
        {
            slotText.text = ""; // 글씨 제거
        }
        Sprite sp = GetReadySprite();
        if (slotButton != null && sp != null) slotButton.image.sprite = sp;
    }

    public void ClearSlot()
    {
        assignedSkill = BattleManager.SkillId.None;
        assignedTarget = null;
        cachedSkillName = "";
        if (slotText != null) slotText.text = ""; // 글씨 제거
        Sprite sp = GetEmptySprite();
        if (slotButton != null && sp != null) slotButton.image.sprite = sp;
    }

    LineRenderer dragLine;
    GameObject lineObj;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (assignedSkill == BattleManager.SkillId.None) return;

        lineObj = new GameObject("TargetingLine");
        dragLine = lineObj.AddComponent<LineRenderer>();
        dragLine.positionCount = 2;
        
        dragLine.material = new Material(Shader.Find("Sprites/Default"));
        dragLine.startColor = Color.red;
        dragLine.endColor = new Color(1f, 0f, 0f, 0.8f); 
        dragLine.sortingOrder = 100;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragLine != null)
        {
            // UI의 화면 좌표를 월드 좌표로 변환해야 선이 정확히 슬롯에서 시작됩니다.
            float depth = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 screenStart = transform.position;
            screenStart.z = depth;
            Vector3 startPos = Camera.main.ScreenToWorldPoint(screenStart);
            startPos.z = 0;

            Vector3 mouseScreen = Input.mousePosition;
            mouseScreen.z = depth;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreen);
            mouseWorldPos.z = 0; 
            
            UpdateArrowCurve(dragLine, startPos, mouseWorldPos);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragLine != null) Destroy(lineObj); 

        if (assignedSkill == BattleManager.SkillId.None) return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit.collider != null)
        {
            Enemy targetEnemy = hit.collider.GetComponent<Enemy>();
            if (targetEnemy != null)
            {
                AssignTarget(targetEnemy);
            }
        }
        else
        {
            assignedTarget = null;
            if (slotText != null) slotText.text = ""; // 글씨 제거
            Sprite sp = GetAssignedSprite();
            if (slotButton != null && sp != null) slotButton.image.sprite = sp;
        }
    }

    void UpdateArrowCurve(LineRenderer lr, Vector3 start, Vector3 end)
    {
        float dist = Vector3.Distance(start, end);
        if (dist <= 0.01f) 
        {
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            return;
        }

        float ratio = 1f - (arrowHeadLength / dist);
        if (ratio < 0f) ratio = 0f;

        // LineRenderer가 중간에 굵기를 바꿀 수 있도록 정점(Vertex)을 4개로 늘립니다.
        lr.positionCount = 4;
        lr.SetPosition(0, start);
        lr.SetPosition(1, Vector3.Lerp(start, end, ratio));
        lr.SetPosition(2, Vector3.Lerp(start, end, ratio + 0.001f));
        lr.SetPosition(3, end);

        AnimationCurve curve = new AnimationCurve();
        
        // 1. 시작점
        curve.AddKey(new Keyframe(0f, lineWidth, 0f, 0f));
        // 2. 화살표 머리 시작 직전
        curve.AddKey(new Keyframe(ratio, lineWidth, 0f, 0f));
        // 3. 화살표 머리 베이스 (넓어짐)
        curve.AddKey(new Keyframe(ratio + 0.001f, arrowBaseWidth, 0f, 0f));
        // 4. 화살표 끝 (뾰족함)
        curve.AddKey(new Keyframe(1f, 0f, 0f, 0f));

        lr.widthCurve = curve;
        lr.widthMultiplier = 1f;
    }
}








