using UnityEngine;
using TMPro;

public class ResourceUI : MonoBehaviour
{
    public TextMeshProUGUI foodText;
    public TextMeshProUGUI materialText;
    public TextMeshProUGUI medicineText;

    void Update()
    {
        if(GameManager.instance==null)
        return;

        foodText.text =
        "식량 : " +
        GameManager.instance.food;

        materialText.text =
        "자재 : " +
        GameManager.instance.material;

        medicineText.text =
        "의약품 : " +
        GameManager.instance.medicine;
    }
}