using UnityEngine;
using TMPro;

public class ResourceUI : MonoBehaviour
{
    public TextMeshProUGUI foodText;
    public TextMeshProUGUI materialText;
    public TextMeshProUGUI recordText;
    public TextMeshProUGUI techText;
    public TextMeshProUGUI specialText;

    void Awake()
    {
        // 씬 내 ExploreManager를 참조하여 폰트 등을 동기화하거나 자원 텍스트를 이름으로 찾음
        if (foodText == null) foodText = transform.Find("FoodText")?.GetComponent<TextMeshProUGUI>();
        if (materialText == null) materialText = transform.Find("MaterialText")?.GetComponent<TextMeshProUGUI>();
        if (recordText == null) recordText = transform.Find("RecordText")?.GetComponent<TextMeshProUGUI>();
        if (techText == null) techText = transform.Find("TechText")?.GetComponent<TextMeshProUGUI>();
        if (specialText == null) specialText = transform.Find("SpecialText")?.GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        RefreshUI();
    }

    void OnEnable()
    {
        GameManager.OnResourcesChanged += RefreshUI;
        RefreshUI();
    }

    void OnDisable()
    {
        GameManager.OnResourcesChanged -= RefreshUI;
    }

    public void RefreshUI()
    {
        if (GameManager.instance == null) return;

        int food = GameManager.instance.food;
        int mat = GameManager.instance.material;
        int rec = GameManager.instance.record;
        int tech = GameManager.instance.techFragment;
        int spec = GameManager.instance.specialMaterial;

        if (foodText != null) foodText.text = $"<color=#88ff88>[FOOD]</color> {food}";
        if (materialText != null) materialText.text = $"<color=#ffff88>[MATERIAL]</color> {mat}";
        if (recordText != null) recordText.text = $"<color=#88ffff>[RECORD]</color> {rec}";
        if (techText != null) techText.text = $"<color=#ff88ff>[TECH]</color> {tech}";
        if (specialText != null) specialText.text = $"<color=#ffaa88>[SPECIAL]</color> {spec}";

        if (foodText == null && materialText == null && recordText == null)
        {
            TextMeshProUGUI unifiedText = GetComponent<TextMeshProUGUI>();
            if (unifiedText != null)
            {
                unifiedText.text = $"<color=#88ff88>[FOOD]</color> {food}  |  " +
                                 $"<color=#ffff88>[MATERIAL]</color> {mat}  |  " +
                                 $"<color=#88ffff>[RECORD]</color> {rec}";
            }
        }
    }
}