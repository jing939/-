using UnityEngine;
using TMPro;

public class PlayerMove : MonoBehaviour
{
    public string characterName = "기사";

    public Transform target;

    public float speed = 5f;

    [Header("Base Stats")]
    public int hp = 100;
    public int attackLevel = 30;
    public int defenseLevel = 30;
    public int minSpeed = 3;
    public int maxSpeed = 6;
    public int currentSpeed = 0;

    public TextMeshProUGUI hpText;
    public TextMeshProUGUI powerText;

    public GameObject damageTextPrefab;

    Vector3 startPosition;

    bool moving;
    bool returning;

    int attackDamage;

    SpriteRenderer sr;

    Camera cam;

    void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();

        cam = Camera.main;

        startPosition = transform.position;

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

        if(moving)
        {
            transform.position =
            Vector3.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
            );

            if(Vector3.Distance(
            transform.position,
            target.position)<0.1f)
            {
                moving=false;

                Attack();

                returning=true;
            }
        }

        if(returning)
        {
            transform.position=
            Vector3.MoveTowards(
            transform.position,
            startPosition,
            speed * Time.deltaTime
            );

            if(Vector3.Distance(
            transform.position,
            startPosition)<0.1f)
            {
                returning=false;
            }
        }
    }

    void UpdateUI()
    {
        if(sr==null) return;

        Vector3 head =
        cam.WorldToScreenPoint(
        sr.bounds.max);

        Vector3 foot =
        cam.WorldToScreenPoint(
        sr.bounds.min);

        powerText.transform.position =
        head + new Vector3(0,20,0);

        hpText.transform.position =
        foot - new Vector3(0,20,0);
    }

    public void SetTarget(Transform enemy)
    {
        target=enemy;
    }

    public void StartAttack(int damage)
    {
        attackDamage=damage;

        startPosition=
        transform.position;

        moving=true;

        if(powerText!=null)
        powerText.text=
        damage.ToString();
    }

    void Attack()
    {
        if(target==null) return;

        Enemy enemy=
        target.GetComponent<Enemy>();

        enemy.TakeDamage(attackDamage);
    }

    // 선택된 스킬 이름을 머리 위 텍스트로 표시
    public void ShowSkillName(string skillName)
    {
        if (powerText == null) return;
        powerText.text = skillName;
    }

    public bool isDead = false;

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        hp-=damage;
        UpdateHP();
        ShowDamage(damage);

        if(hp<=0)
        {
            isDead = true;
            // 죽으면 파괴하지 않고 화면 밖으로 퇴장 (코루틴)
            StartCoroutine(DieAndSlideOff());
        }
    }

    System.Collections.IEnumerator DieAndSlideOff()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 체력바 등의 슬롯 텍스트 비우기
        if (hpText != null) hpText.text = "Dead";

        // 사망 시 머리 위 스킬칸(UI)을 아예 꺼버림 (클릭 원천 차단)
        FloatingSkillSlot slot = GetComponentInChildren<FloatingSkillSlot>();
        if (slot != null) slot.gameObject.SetActive(false);

        // 아주 빠르고 멀리 화면 왼쪽 바깥으로 퇴장
        Vector3 targetPos = transform.position + new Vector3(-35f, 0, 0); 
        while (Vector3.Distance(transform.position, targetPos) > 0.5f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, 40f * Time.deltaTime);
            yield return null;
        }
    }

    void UpdateHP()
    {
        hpText.text=
        "HP : "+hp;
    }

    void ShowDamage(int damage)
    {
        if(damageTextPrefab==null) return;

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

    void OnMouseDown()
    {
        Debug.Log("[PlayerMove] OnMouseDown -> ClickManager로 직접 이벤트 전달!");
        ClickManager clickManager = FindFirstObjectByType<ClickManager>();
        if (clickManager != null)
        {
            clickManager.HandleClick(this.gameObject);
        }
    }
}
