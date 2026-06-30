using UnityEngine;
using TMPro;

public class PlayerMove : MonoBehaviour
{
    public Transform target;
    public float speed = 5f;

    [Header("Base Stats")]
    public int maxHp = 100;
    public int hp = 100;
    public int shield = 0;
    public int attackLevel = 30;
    public int defenseLevel = 25;
    public int minSpeed = 4;
    public int maxSpeed = 8;
    public int currentSpeed;
    
    [Header("정신력 (Sanity)")]
    [Range(-45, 45)] public int sp = 0; // -45 ~ +45

    [Header("코인 확률 (0.0 ~ 1.0)")]
    // 정신력이 0이면 50%, 45면 95%, -45면 5%
    public float headProbability => 0.5f + (sp / 100f);

    [Header("스킬 데이터 (4개 권장)")]
    public SkillData[] skills = new SkillData[4];

    public bool isDead = false;

    [Header("Skill UI")]
    public TextMeshProUGUI hpText;
    public Sprite hpIconSprite;
    public Sprite spIconSprite;
    private StatusUIBar statusUIBar;
    public TextMeshProUGUI powerText;
    public GameObject damageTextPrefab;
    private TextMeshProUGUI spText; // 실시간 정신력 UI
    private TextMeshProUGUI shieldText; // 보호막 UI

    [Header("Custom UI Images (커스텀 이미지)")]
    public Sprite emptySlotSprite; // 캐릭터 기본 이미지 (비어있을 때)

    [Header("Ring UI (발밑 림버스 스타일)")]
    public RectTransform ringUIRoot; // 링 UI 전체 부모
    public UnityEngine.UI.Image hpFillImage; // 'HpFill' 이름의 자식 이미지
    public UnityEngine.UI.Image spFillImage; // 'SpFill' 이름의 자식 이미지

    public TextMeshProUGUI statusText;
    [Header("Status Effect UI")]
    public Vector3 statusUIOffset = new Vector3(0, 40, 0);
    public Texture2D bleedIcon;
    public Texture2D poisonIcon;
    public Texture2D ruptureIcon;
    public Texture2D sinkingIcon;
    [HideInInspector] public StatusEffect statusEffect;

    [HideInInspector] public Vector3 startPosition;
    bool moving;
    bool returning;
    int attackDamage;

    SpriteRenderer sr;
    Camera cam;

    void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        startPosition = transform.position;
        
        // 각 캐릭터가 자신만의 독립적인 UI를 가지도록 복제
        if (hpText != null)
        {
            // 이제 단순 텍스트 대신 새로 만든 StatusUIBar를 생성합니다!
            statusUIBar = StatusUIBar.Create(hpText.transform.parent, gameObject, hpIconSprite, spIconSprite);
            hpText.gameObject.SetActive(false); // 원본 텍스트 숨기기
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
            orig.gameObject.SetActive(false); // 원본 숨기기
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

        if(moving && target != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

            if(Vector3.Distance(transform.position, target.position)<0.1f)
            {
                moving=false;
                Attack();
                returning=true;
            }
        }

        if(returning)
        {
            transform.position= Vector3.MoveTowards(transform.position, startPosition, speed * Time.deltaTime);

            if(Vector3.Distance(transform.position, startPosition)<0.1f)
            {
                returning=false;
            }
        }
    }

    void UpdateUI()
    {
        if(sr==null) return;
        if(cam==null) { cam = Camera.main; return; }

        Vector3 head = cam.WorldToScreenPoint(new Vector3(sr.bounds.center.x, sr.bounds.max.y, 0));
        Vector3 foot = cam.WorldToScreenPoint(new Vector3(sr.bounds.center.x, sr.bounds.min.y, 0));

        if (powerText != null) powerText.transform.position = head + new Vector3(0,20,0);
        if (statusUIBar != null) { statusUIBar.transform.position = foot + new Vector3(0, -25, 0); }
        
        if (ringUIRoot != null) ringUIRoot.position = foot; // 발 밑으로 링 UI 이동
        
        if (statusEffect != null) statusEffect.UpdatePosition(foot - statusUIOffset); // 발 밑으로 복구
    }

    void OnMouseDown()
    {
        // 플레이어 클릭을 감지하면 무조건 스탯창 토글!
        if (StatsUIManager.instance != null)
        {
            StatsUIManager.instance.TogglePlayerStats(this);
            Debug.Log("플레이어 터치 감지 성공!");
        }
    }

    public void SetTarget(Transform enemy)
    {
        target=enemy;
    }

    public void StartAttack(int damage)
    {
        attackDamage=damage;
        startPosition= transform.position;
        moving=true;

        if (powerText != null) powerText.text= damage.ToString();
    }

    void Attack()
    {
        if(target==null) return;
        Enemy enemy= target.GetComponent<Enemy>();
        if (enemy != null) enemy.TakeDamage(attackDamage);
    }

    public void TakeDamage(int damage, bool isStatusDamage = false)
    {
        if (isDead) return;

        int originalDamage = damage;

        // 쉴드 먼저 차감
        if (shield > 0)
        {
            if (damage >= shield)
            {
                damage -= shield;
                shield = 0;
            }
            else
            {
                shield -= damage;
                damage = 0;
            }
            UpdateHP(); // 쉴드가 깎였으니 UI 바로 갱신
        }

        hp -= damage;
        UpdateHP();
        ShowDamage(originalDamage);

        if(hp<=0)
        {
            isDead = true;
            // 즉시 이동하지 않고 BattleManager가 모든 공격이 끝난 뒤에 이동시킵니다.
        }

        // 상태이상 데미지가 아닐 때만 피격 시 발동 상태이상(파열, 침잠) 작동
        if (!isStatusDamage && statusEffect != null)
        {
            statusEffect.OnHit();
        }
    }

    public void AddSP(int amount)
    {
        if (isDead) return;
        sp = Mathf.Clamp(sp + amount, -45, 45);
        UpdateHP(); // SP 변경 시 텍스트도 갱신
        Debug.Log($"{gameObject.name}의 정신력이 {amount}만큼 변동되어 현재 {sp}입니다.");
    }

    public void HealHP(int amount)
    {
        if (isDead) return;
        hp = Mathf.Min(hp + amount, maxHp);
        UpdateHP();
        // 초록색 회복 수치 표시
        if (damageTextPrefab != null)
        {
            GameObject dmg = Instantiate(damageTextPrefab, transform.position, Quaternion.identity);
            DamageText dt = dmg.GetComponent<DamageText>();
            if (dt != null) dt.SetHeal(amount);
        }
    }

    public void StartRunAway()
    {
        StartCoroutine(RunAway());
    }

    System.Collections.IEnumerator RunAway()
    {
        // 도망갈 땐 UI 글씨가 안 보이게 끔
        if (statusUIBar != null) statusUIBar.gameObject.SetActive(false);
        if (powerText != null) powerText.gameObject.SetActive(false);

        // 왼쪽 저 멀리로 목표점 설정
        Vector3 targetPos = new Vector3(-15f, transform.position.y, transform.position.z);
        
        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, (speed * 3) * Time.deltaTime);
            yield return null;
        }
    }

    void UpdateHP()
    {
        if (statusUIBar != null)
        {
            statusUIBar.UpdateHP(hp, maxHp);
            statusUIBar.UpdateSP(sp, 45, false);
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

    void ShowDamage(int damage)
    {
        if(damageTextPrefab==null) return;
        GameObject dmg= Instantiate(damageTextPrefab, transform.position, Quaternion.identity);
        DamageText dt= dmg.GetComponent<DamageText>();
        if(dt!=null) dt.SetDamage(damage);
    }

    void OnDestroy()
    {
        if (statusUIBar != null) Destroy(statusUIBar.gameObject);
        if (shieldText != null) Destroy(shieldText.gameObject);
        if (powerText != null) Destroy(powerText.gameObject);
    }
}








