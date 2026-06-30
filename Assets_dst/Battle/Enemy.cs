using UnityEngine;
using TMPro;

public class Enemy : MonoBehaviour
{
    public string characterName = "몬스터";

    [Header("Base Stats")]
    public int maxHp = 100;
    public int hp = 100;
    public int attackLevel = 25;
    public int defenseLevel = 25;
    public int minSpeed = 2;
    public int maxSpeed = 5;
    public int currentSpeed = 0;

    [Header("Skill Setup (Limbus Coin)")]
    public int eBasePower = 4;
    public int eCoinPower = 3;
    public int eCoinCount = 2; // 코인 갯수

    [Header("Pattern AI (선택사항)")]
    public EnemyPattern pattern;

    [Header("References (참조)")]
    public PlayerMove player;

    public TextMeshProUGUI hpText;
    public TextMeshProUGUI powerText;

    public GameObject damageTextPrefab;

    SpriteRenderer sr;

    Camera cam;

    void Start()
    {
        sr=
        GetComponentInChildren<SpriteRenderer>();

        cam=Camera.main;

        RollSpeed();
        UpdateHP();
    }

    public void RollSpeed()
    {
        currentSpeed = Random.Range(minSpeed, maxSpeed + 1);
    }

    void Update()
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        if(sr==null) return;

        Vector3 head=
        cam.WorldToScreenPoint(
        sr.bounds.max);

        Vector3 foot=
        cam.WorldToScreenPoint(
        sr.bounds.min);

        powerText.transform.position=
        head + new Vector3(0,20,0);

        hpText.transform.position=
        foot - new Vector3(0,20,0);
    }

    void OnMouseDown()
    {
        // 전투/스킬 UI 처리는 ClickManager & BattleUIManager/StatsUIManager에서만 담당한다.
        // 여기서는 Idle 상태에서의 스탯 UI만 StatsUIManager가 처리하도록 남겨둔다.
        StatsUIManager stats = FindFirstObjectByType<StatsUIManager>();
        if (stats != null && stats.state == StatsUIManager.GameState.Idle)
        {
            // StatsUIManager.Update()가 이미 Raycast로 처리하므로 여기서는 아무 것도 하지 않는다...
            // 라고 되어있었지만, ClickManager가 유저 마우스를 못 읽는 현상이 있어 여기서 직접 넘겨줍니다.
            Debug.Log("[Enemy] OnMouseDown in Idle -> ClickManager로 직접 이벤트 전달!");
            ClickManager cm = FindFirstObjectByType<ClickManager>();
            if (cm != null) cm.HandleClick(this.gameObject);
            return;
        }

        // 그 외 상태에서는 ClickManager가 Raycast로 처리하여야 하나, UI나 다른 레이어에 막히는 버그 방지를 위해 여기서 직접 넘겨줌.
        Debug.Log("[Enemy] OnMouseDown in Battle -> ClickManager로 전투 타겟팅 클릭 이벤트 강제 전달!");
        ClickManager clickManager = FindFirstObjectByType<ClickManager>();
        if (clickManager != null)
        {
            clickManager.HandleClick(this.gameObject);
        }
    }

    public void SelectSkill()
    {
        currentSpeed = Random.Range(minSpeed, maxSpeed + 1);

        if (pattern != null && BattleManager.instance != null)
        {
            // 패턴에 따라 위력을 받아오고, 이를 "단타 확정 코인"으로 변환해서 림버스 시스템에 던져줍니다.
            int turnCount = 1; // TurnManager에 숫자형 turnCount가 없으므로 임시로 1턴 고정
            int patternPower = pattern.GetSkillPower(this, BattleManager.instance, turnCount);
            
            eBasePower = patternPower;
            eCoinPower = 0; 
            eCoinCount = 1; // 단타 1코인 (무조건 patternPower 위력 고정)
            // powerText.text = eBasePower.ToString();
        }
        else
        {
            // 패턴이 안 꽂혀있으면 몬스터의 임시 스킬 지정 (가장 기본인 4 위력에 +3 위력을 가진 2연타 코인 스킬)
            eBasePower = 4;
            eCoinPower = 3;
            eCoinCount = 2;
        }

        if (powerText != null) powerText.text = "?";
    }

    public void TakeDamage(int damage)
    {
        hp -= damage;

        UpdateHP();

        ShowDamage(damage);

        if (hp <= 0)
            Destroy(gameObject);
    }

    public void Heal(int healAmount)
    {
        hp += healAmount;
        if (hp > maxHp) hp = maxHp;
        UpdateHP();
    }

    public void AttackPlayer()
    {
        // 최신 합(Clash) 방식이 완전 정착되었으므로 기존 구코드에서 발생할 에러 막음
    }

    void UpdateHP()
    {
        hpText.text=
        "HP : "+hp;
    }

    void ShowDamage(int damage)
    {
        if(damageTextPrefab==null)
        {
            Debug.LogWarning("[Enemy] damageTextPrefab is NULL (데미지 텍스트 생략)");
            return;
        }

        GameObject dmg=
        Instantiate(
        damageTextPrefab,
        transform.position,
        Quaternion.identity
        );

        DamageText dt=
        dmg.GetComponent<DamageText>();

        if(dt!=null)
        dt.SetDamage(damage);
    }
}
