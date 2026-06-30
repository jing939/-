using UnityEngine;
using TMPro;

public class Enemy : MonoBehaviour
{
    [Header("Base Stats")]
    public int maxHp = 100;
    public int hp = 100;
    public int shield = 0;
    public int attackLevel = 25;
    public int defenseLevel = 25;
    public int minSpeed = 3;
    public int maxSpeed = 6;
    public int currentSpeed;

    [Header("정신력 (Sanity)")]
    [Range(-45, 45)] public int sp = 0; // -45 ~ +45

    [Header("코인 확률 (0.0 ~ 1.0)")]
    // 정신력이 0이면 50%, 45면 95%, -45면 5%
    public float headProbability => 0.5f + (sp / 100f);

    [Header("스킬 데이터")]
    public SkillData[] skills = new SkillData[2];

    // 현재 선택된 스킬 정보 (매 턴 갱신됨)
    [HideInInspector] public int eBasePower;
    [HideInInspector] public int eCoinPower;
    [HideInInspector] public int eCoinCount;
    [HideInInspector] public SkillEffect[] eActiveSkillEffects;
    [HideInInspector] public CoinEffect[] eActiveCoinEffects;
    [HideInInspector] public string eActiveSkillName;
    [HideInInspector] public bool eActiveSkillIsDefense;
    [HideInInspector] public int eActiveDefenseLevel;

    [Header("References")]
    public PlayerMove player;
    public RangedPlayerMove rangedPlayer;

    public TextMeshProUGUI hpText;
    public Sprite hpIconSprite;
    public Sprite spIconSprite;
    private StatusUIBar statusUIBar;
    public TextMeshProUGUI powerText;
    public GameObject damageTextPrefab;
    private TextMeshProUGUI spText; // 실시간 정신력 UI
    private TextMeshProUGUI shieldText; // 보호막 UI

    [Header("Ring UI (발밑 림버스 스타일)")]
    public RectTransform ringUIRoot; // 링 UI 전체 부모
    public UnityEngine.UI.Image hpFillImage; // 'HpFill' 이름의 자식 이미지
    public UnityEngine.UI.Image spFillImage; // 'SpFill' 이름의 자식 이미지
    
    public TextMeshProUGUI statusText;
    [Header("Status Effect UI")]
    public Vector3 statusUIOffset = new Vector3(0, 75, 0);
    public Texture2D bleedIcon;
    public Texture2D poisonIcon;
    public Texture2D ruptureIcon;
    public Texture2D sinkingIcon;
    [HideInInspector] public StatusEffect statusEffect;

    [HideInInspector] public Vector3 startPosition;

    SpriteRenderer sr;
    Camera cam;

    void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        startPosition = transform.position;

        // 몬스터의 스킬 이름이 지정되지 않았거나 '공1' 등으로 되어있으면 '할퀴기'로 변경합니다.
        if (skills != null)
        {
            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i] != null && (string.IsNullOrEmpty(skills[i].skillName) || skills[i].skillName == "공1" || skills[i].skillName == "몬스터 공격"))
                {
                    skills[i].skillName = "할퀴기";
                }
            }
        }
        
        // 각 몬스터가 자신만의 독립적인 UI를 가지도록 복제
        if (hpText != null)
        {
            statusUIBar = StatusUIBar.Create(hpText.transform.parent, gameObject, hpIconSprite, spIconSprite);
            hpText.gameObject.SetActive(false); // 원본 템플릿 숨기기
        }

        if (ringUIRoot != null)
        {
            RectTransform origRing = ringUIRoot;
            ringUIRoot = Instantiate(ringUIRoot, ringUIRoot.parent);
            ringUIRoot.gameObject.SetActive(true);
            
            // 이름으로 Fill Image 찾아서 할당
            Transform hpT = ringUIRoot.Find("HpFill");
            if (hpT != null) hpFillImage = hpT.GetComponent<UnityEngine.UI.Image>();
            
            Transform spT = ringUIRoot.Find("SpFill");
            if (spT != null) spFillImage = spT.GetComponent<UnityEngine.UI.Image>();
            
            if (origRing.gameObject.scene.rootCount > 0) origRing.gameObject.SetActive(false);
        }
        if (powerText != null)
        {
            TextMeshProUGUI orig = powerText;
            powerText = Instantiate(powerText, powerText.transform.parent);
            powerText.gameObject.SetActive(false); // 구버전 파워 텍스트 숨김 (림버스 UI 사용)
            powerText.raycastTarget = false;
            orig.gameObject.SetActive(false); // 원본 템플릿 숨기기
        }
        
        statusEffect = gameObject.AddComponent<StatusEffect>();
        if (powerText != null)
        {
            statusEffect.SetupUI(powerText.transform.parent, powerText, bleedIcon, poisonIcon, ruptureIcon, sinkingIcon);
        }
        
        UpdateHP();
    }

    void Update()
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        if(sr==null) return;
        if(cam==null) { cam = Camera.main; return; }

        Vector3 head = cam.WorldToScreenPoint(new Vector3(sr.bounds.center.x, sr.bounds.max.y, 0));
        Vector3 foot = cam.WorldToScreenPoint(new Vector3(sr.bounds.center.x, sr.bounds.min.y, 0));

        if (powerText != null) powerText.transform.position= head + new Vector3(0,20,0);
        if (statusUIBar != null) { statusUIBar.transform.position = foot + new Vector3(0, -25, 0); }
        
        if (ringUIRoot != null) ringUIRoot.position = foot; // 발 밑으로 링 UI 이동
        
        if (statusEffect != null) statusEffect.UpdatePosition(foot - statusUIOffset); // 발 밑으로 복구
    }

    public void SelectSkill()
    {
        currentSpeed = Random.Range(minSpeed, maxSpeed + 1);

        if (skills != null && skills.Length > 0)
        {
            SkillData picked = skills[Random.Range(0, skills.Length)];
            if (picked != null && picked.coinCount > 0)
            {
                eBasePower = picked.basePower <= 0 ? 4 : picked.basePower;
                eCoinPower = picked.coinPower <= 0 ? 3 : picked.coinPower;
                eCoinCount = picked.coinCount <= 0 ? 1 : picked.coinCount;
                eActiveSkillEffects = picked.effects;
                eActiveCoinEffects = picked.coinEffects;
                eActiveSkillName = string.IsNullOrEmpty(picked.skillName) ? "할퀴기" : picked.skillName;
                eActiveSkillIsDefense = picked.isDefenseSkill;
                eActiveDefenseLevel = picked.defenseLevel;
            }
            else
            {
                eBasePower = 4; eCoinPower = 2; eCoinCount = 2; 
                eActiveSkillEffects = null;
                eActiveCoinEffects = null;
                eActiveSkillName = "할퀴기";
                eActiveSkillIsDefense = false;
                eActiveDefenseLevel = 0;
            }
        }
        else
        {
            // 기본값
            eBasePower = 4; eCoinPower = 2; eCoinCount = 2; eActiveSkillEffects = null;
            eActiveSkillIsDefense = false;
            eActiveDefenseLevel = 0;
        }

        if (powerText != null) powerText.text = "?";
    }

    public void TakeDamage(int damage, bool isStatusDamage = false)
    {
        hp -= damage;
        if (hp < 0) hp = 0;
        UpdateHP();
        ShowDamage(damage);

        // 상태이상 데미지가 아닐 때만 피격 시 발동 상태이상(파열, 침잠) 작동
        if (!isStatusDamage && statusEffect != null)
        {
            statusEffect.OnHit();
        }

        if (hp <= 0) Destroy(gameObject);
    }

    public void AddSP(int amount)
    {
        sp = Mathf.Clamp(sp + amount, -45, 45);
        UpdateHP(); // SP 변경 시 텍스트도 갱신
        Debug.Log($"{gameObject.name}의 정신력이 {amount}만큼 변동되어 현재 {sp}입니다.");
    }

    public void Heal(int healAmount)
    {
        hp += healAmount;
        if (hp > maxHp) hp = maxHp;
        UpdateHP();
    }

    public void AttackPlayer()
    {
        if (player != null && !player.isDead) player.TakeDamage(eBasePower + eCoinPower * eCoinCount);
        else if (rangedPlayer != null && !rangedPlayer.isDead) rangedPlayer.TakeDamage(eBasePower + eCoinPower * eCoinCount);
    }

    void UpdateHP()
    {
        if (statusUIBar != null)
        {
            statusUIBar.UpdateHP(hp, maxHp);
            statusUIBar.UpdateSP(sp, 45, true);
        }

        if (shieldText != null)
        {
            if (shield > 0)
            {
                shieldText.gameObject.SetActive(true);
                shieldText.text = $"[+{shield}]";
            }
            else
            {
                shieldText.gameObject.SetActive(false);
            }
        }

        if (hpFillImage != null) hpFillImage.fillAmount = (float)hp / maxHp;
        if (spFillImage != null) spFillImage.fillAmount = (sp + 45f) / 90f; // -45~45 범위를 0.0~1.0으로 변환
    }

    public void AddShield(int amount)
    {
        shield += amount;
        UpdateHP();
    }

    public void ClearShield()
    {
        shield = 0;
        UpdateHP();
    }

    void OnMouseDown()
    {
        // 몬스터 클릭을 감지하면 무조건 스탯창 토글!
        if (StatsUIManager.instance != null)
        {
            StatsUIManager.instance.ToggleEnemyStats(this);
            Debug.Log("몬스터 터치 감지 성공!");
        }
    }

    void ShowDamage(int damage)
    {
        if (damageTextPrefab == null) return;
        GameObject dmg= Instantiate(damageTextPrefab, transform.position, Quaternion.identity);
        DamageText dt= dmg.GetComponent<DamageText>();
        if(dt!=null) dt.SetDamage(damage);
    }

    void OnDestroy()
    {
        // 몬스터가 죽으면 자신만의 UI 텍스트들도 함께 파괴
        if (statusUIBar != null) Destroy(statusUIBar.gameObject);
        if (shieldText != null) Destroy(shieldText.gameObject);
        if (powerText != null) Destroy(powerText.gameObject);
    }
}











