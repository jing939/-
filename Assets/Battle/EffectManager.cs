using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public static EffectManager instance;

    [Header("유니티에서 파티클 프리팹을 연결해주세요")]
    public GameObject clashSparkPrefab; // 무기가 부딪힐 때 불꽃
    public GameObject hitBloodPrefab;   // 맞았을 때 피격 이펙트

    void Awake()
    {
        instance = this;
    }

    // 합(Clash) 이펙트 발생
    public void SpawnClashEffect(Vector3 position)
    {
        if (clashSparkPrefab != null)
        {
            GameObject effect = Instantiate(clashSparkPrefab, position, Quaternion.identity);
            Destroy(effect, 1f); // 1초 뒤에 파티클 자동 삭제
        }
        else
        {
            // 프리팹이 없다면 긴급 임시 파티클 생성!
            CreateTemporaryEffect(position, Color.yellow);
        }
    }

    // 타격(Hit) 이펙트 발생
    public void SpawnHitEffect(Vector3 position)
    {
        if (hitBloodPrefab != null)
        {
            GameObject effect = Instantiate(hitBloodPrefab, position, Quaternion.identity);
            Destroy(effect, 1f); 
        }
        else
        {
            // 프리팹이 없다면 긴급 임시 파티클 생성!
            CreateTemporaryEffect(position, Color.red);
        }
    }

    // 커스텀(지정) 공격 이펙트 발생
    public void SpawnCustomEffect(GameObject customPrefab, Vector3 position)
    {
        if (customPrefab != null)
        {
            GameObject effect = Instantiate(customPrefab, position, Quaternion.identity);
            Destroy(effect, 1.5f); // 공격 이펙트는 여유있게 삭제
        }
    }

    // 유저님이 파티클을 아직 안 만들었을 때 임시로 시각적 효과를 보여주는 함수
    private void CreateTemporaryEffect(Vector3 pos, Color color)
    {
        GameObject tempObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        tempObj.transform.position = pos;
        tempObj.transform.localScale = Vector3.one * 0.8f;
        Destroy(tempObj.GetComponent<Collider>()); // 콜라이더 제거
        
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = color;
        tempObj.GetComponent<MeshRenderer>().material = mat;

        // 투명하게 사라지게 만들기 (임시 이펙트)
        StartCoroutine(FadeOutAndDestroy(tempObj));
    }

    System.Collections.IEnumerator FadeOutAndDestroy(GameObject obj)
    {
        Material mat = obj.GetComponent<MeshRenderer>().material;
        Color c = mat.color;
        float elapsed = 0;
        
        // 회전도 살짝
        obj.transform.Rotate(0, 0, Random.Range(0f, 360f));

        while (elapsed < 0.2f)
        {
            if (obj == null) yield break;
            elapsed += Time.deltaTime;
            obj.transform.localScale += Vector3.one * Time.deltaTime * 5f;
            c.a = 1f - (elapsed / 0.2f);
            mat.color = c;
            yield return null;
        }
        Destroy(obj);
    }
}
