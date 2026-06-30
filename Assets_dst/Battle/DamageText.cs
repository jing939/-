using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{
    TextMeshProUGUI text;

    void Awake()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void SetDamage(int damage)
    {
        if(text == null) return;

        text.text = damage.ToString();

        Destroy(gameObject,1f);
    }
}