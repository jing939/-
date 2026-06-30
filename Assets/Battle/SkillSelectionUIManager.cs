using UnityEngine;

public class SkillSelectionUIManager : MonoBehaviour
{
    public static SkillSelectionUIManager instance;

    [Header("UI 연결 (유니티에서 만드신 스킬 패널을 드래그하세요)")]
    public GameObject skillPanel;

    // 현재 스킬을 배정하려고 패널을 호출한 대상 슬롯 (1번 버튼인지 2번 버튼인지 기억)
    private FloatingSkillSlot activeSlot;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (skillPanel != null) skillPanel.SetActive(false); // 시작 시 꺼둠
    }

    // FloatingSkillSlot에서 호출하는 함수
    public void OpenPanel(FloatingSkillSlot slot)
    {
        activeSlot = slot;
        if (skillPanel != null) skillPanel.SetActive(true);
    }

    // 닫기 버튼용 (선택 안하고 취소할 때)
    public void ClosePanel()
    {
        if (skillPanel != null) skillPanel.SetActive(false);
        activeSlot = null;
    }

    // ============================================
    // 유저님이 스킬 버튼의 OnClick()에 연결할 함수들
    // ============================================
    
    public void SelectSkill1() { ApplySkill(BattleManager.SkillId.Skill1); }
    public void SelectSkill2() { ApplySkill(BattleManager.SkillId.Skill2); }
    public void SelectSkill3() { ApplySkill(BattleManager.SkillId.Skill3); }
    public void SelectSkill4() { ApplySkill(BattleManager.SkillId.Skill4); }

    void ApplySkill(BattleManager.SkillId skill)
    {
        if (activeSlot != null && activeSlot.ownerPlayer != null)
        {
            int index = (int)skill - 1;
            string sNm = "기본 공격"; // 기본 이름

            if (index >= 0)
            {
                PlayerMove pm = activeSlot.ownerPlayer.GetComponent<PlayerMove>();
                RangedPlayerMove rpm = activeSlot.ownerPlayer.GetComponent<RangedPlayerMove>();

                if (pm != null && index < pm.skills.Length && pm.skills[index] != null) 
                    sNm = pm.skills[index].skillName;
                else if (rpm != null && index < rpm.skills.Length && rpm.skills[index] != null) 
                    sNm = rpm.skills[index].skillName;
            }

            activeSlot.AssignSkill(skill, sNm);
        }

        ClosePanel();
    }
}
