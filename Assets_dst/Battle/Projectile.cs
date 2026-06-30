using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed=10f;

    Transform target;

    int damage;

    public void Init(
    Transform enemy,
    int dmg)
    {
        target=enemy;

        damage=dmg;
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

        if(Vector3.Distance(
        transform.position,
        target.position)<0.1f)
        {
            target.GetComponent<Enemy>()
            .TakeDamage(damage);

            Destroy(gameObject);
        }
    }
}