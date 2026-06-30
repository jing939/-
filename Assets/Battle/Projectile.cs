using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed=10f;

    Transform target;
    int damage;
    SkillEffect[] skillEffects;
    GameObject attacker;

    public void Init(Transform enemy, int dmg, SkillEffect[] effects = null, GameObject atk = null)
    {
        target=enemy;
        damage=dmg;
        skillEffects = effects;
        attacker = atk;
    }

    void Update()
    {
        if(target==null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position=
        Vector3.MoveTowards(
        transform.position,
        target.position,
        speed*Time.deltaTime
        );

        if(Vector3.Distance(transform.position, target.position)<0.1f)
        {
            Enemy e = target.GetComponent<Enemy>();
            if (e != null)
            {
                e.TakeDamage(damage);

                // [신규] 인스펙터에서 설정한 상태 이상 부여
                StatusEffect enemyStatus = target.GetComponent<StatusEffect>();
                if (enemyStatus != null && skillEffects != null)
                {
                    foreach(var eff in skillEffects)
                    {
                        if (eff.statusType == StatusType.Bleed) enemyStatus.AddBleed(eff.potency, eff.count);
                        else if (eff.statusType == StatusType.Poison) enemyStatus.AddPoison(eff.potency, eff.count);
                        else if (eff.statusType == StatusType.Rupture) enemyStatus.AddRupture(eff.potency, eff.count);
                        else if (eff.statusType == StatusType.Sinking) enemyStatus.AddSinking(eff.potency, eff.count);
                    }
                }

                // 적 처치 시 SP 회복
                if (e.hp <= 0)
                {
                    if (attacker != null)
                    {
                        RangedPlayerMove rpm = attacker.GetComponent<RangedPlayerMove>();
                        if (rpm != null) rpm.AddSP(10);
                    }
                }
            }

            // 총알이 명중했을 때 이펙트 및 흔들림 발생!
            if (EffectManager.instance != null) EffectManager.instance.SpawnHitEffect(target.position);
            if (CameraShake.instance != null)
            {
                if (damage >= 100) CameraShake.instance.HeavyShake(); // 데미지가 크면 강하게 (Skill 4 필살기 처리)
                else CameraShake.instance.MediumShake();
            }

            Destroy(gameObject);
        }
    }
}