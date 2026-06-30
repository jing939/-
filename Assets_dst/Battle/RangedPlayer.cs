using UnityEngine;
using TMPro;

public class RangedPlayerMove : MonoBehaviour
{
    public string characterName = "궁수";

    public Transform target;

    public float speed=5f;

    public float attackRange=3f;

    [Header("Base Stats")]
    public int hp=80;
    public int attackLevel = 25;
    public int defenseLevel = 20;
    public int minSpeed = 5;
    public int maxSpeed = 8;
    public int currentSpeed = 0;

    public TextMeshProUGUI hpText;
    public TextMeshProUGUI powerText;

    public GameObject projectile;
    public GameObject damageTextPrefab;

    Vector3 startPosition;

    bool moving;
    bool returning;

    int attackDamage;

    SpriteRenderer sr;

    Camera cam;

    void Start()
    {
        sr=
        GetComponentInChildren<SpriteRenderer>();

        cam=Camera.main;

        startPosition=
        transform.position;

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

        if(moving && target!=null)
        {
            float dist=
            Vector3.Distance(
            transform.position,
            target.position);

            if(dist>attackRange)
            {
                transform.position=
                Vector3.MoveTowards(
                transform.position,
                target.position,
                speed*Time.deltaTime
                );
            }
            else
            {
                moving=false;

                Shoot();

                returning=true;
            }
        }

        if(returning)
        {
            transform.position=
            Vector3.MoveTowards(
            transform.position,
            startPosition,
            speed*Time.deltaTime
            );

            if(Vector3.Distance(
            transform.position,
            startPosition)<0.1f)
            returning=false;
        }
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

    public void SetTarget(Transform enemy)
    {
        target=enemy;
    }

    public void StartAttack(int damage)
    {
        attackDamage=damage;

        moving=true;

        if (powerText != null)
            powerText.text=
            damage.ToString();
    }

    // 선택된 스킬 이름을 머리 위 텍스트로 표시
    public void ShowSkillName(string skillName)
    {
        if (powerText == null) return;
        powerText.text = skillName;
    }

    public void ShootTarget(Transform customTarget, int damage)
    {
        if (projectile == null)
        {
            Debug.LogWarning("[RangedPlayerMove] 무기를 발사해야 하지만 투사체가 없습니다. (발사는 생략되나 데미지는 확실히 들어갑니다)");
            return;
        }

        GameObject bullet=
        Instantiate(
        projectile,
        transform.position,
        Quaternion.identity);

        var projComp = bullet.GetComponent<Projectile>();
        if (projComp != null)
        {
            // 합(Clash) 모드에선 BattleManager가 정확한 타이밍에 딜을 박으므로, 겹치는 데미지 에러 방지를 위해 0을 넘김!
            projComp.Init(customTarget, 0);
        }
    }

    void Shoot()
    {
        ShootTarget(target, attackDamage);
    }

    void UpdateHP()
    {
        hpText.text=
        "HP : "+hp;
    }

    public bool isDead = false;

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        hp -= damage;
        UpdateHP();
        ShowDamage(damage);

        if (hp <= 0)
        {
            isDead = true;
            StartCoroutine(DieAndSlideOff());
        }
    }

    System.Collections.IEnumerator DieAndSlideOff()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (hpText != null) hpText.text = "Dead";

        FloatingSkillSlot slot = GetComponentInChildren<FloatingSkillSlot>();
        if (slot != null) slot.gameObject.SetActive(false);

        Vector3 targetPos = transform.position + new Vector3(-35f, 0, 0); 
        while (Vector3.Distance(transform.position, targetPos) > 0.5f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, 40f * Time.deltaTime);
            yield return null;
        }
    }

    void ShowDamage(int damage)
    {
        if (damageTextPrefab == null) return;

        GameObject dmg = Instantiate(damageTextPrefab, transform.position, Quaternion.identity);
        DamageText dt = dmg.GetComponent<DamageText>();
        if (dt != null) dt.SetDamage(damage);
    }

    void OnMouseDown()
    {
        Debug.Log("[RangedPlayerMove] OnMouseDown -> ClickManager로 직접 이벤트 전달!");
        ClickManager clickManager = FindFirstObjectByType<ClickManager>();
        if (clickManager != null)
        {
            clickManager.HandleClick(this.gameObject);
        }
    }
}
