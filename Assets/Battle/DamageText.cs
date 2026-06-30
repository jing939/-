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
        text.color = Color.white;
        Destroy(gameObject, 1f);
    }

    public void SetHeal(int amount)
    {
        if(text == null) return;
        text.text = $"+{amount}";
        text.color = new Color(0.2f, 1f, 0.3f); // 초록색
        Destroy(gameObject, 1f);
    }

    public void SetSP(int amount)
    {
        if(text == null) return;
        text.text = $"SP +{amount}";
        text.color = new Color(0.4f, 0.8f, 1f); // 하늘색
        Destroy(gameObject, 1f);
    }
}