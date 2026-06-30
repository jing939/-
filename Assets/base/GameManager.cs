using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public static event System.Action OnResourcesChanged;

    public void TriggerResourcesChanged()
    {
        OnResourcesChanged?.Invoke();
    }

    const string SaveKey = "GameProgress";

    public int food=50;

    public int material=30;

    public int record=10;
    public int techFragment = 5;
    public int specialMaterial = 0;

    [Header("Extended Systems")]
    public int hp = 100;
    public int maxHp = 100;
    public int potionCount = 0;
    public int baseCombatPower = 10;

    public List<AllyData> allies = new List<AllyData>();
    public List<string> acquiredSkills = new List<string>();
    public List<LoreData> collectedLores = new List<LoreData>();

    public int GetTotalCombatPower()
    {
        int power = baseCombatPower;
        foreach (var ally in allies) power += ally.power;
        // 추후 스킬 보너스 합산 가능
        return power;
    }

    public void CraftPotion()
    {
        if (specialMaterial >= 3)
        {
            specialMaterial -= 3;
            potionCount++;
            Debug.Log("[CRAFT] Potion created.");
            SaveProgress();
        }
        else
        {
            Debug.Log("[CRAFT] Not enough materials.");
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            LoadProgress();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [System.Serializable]
    class GameSaveData
    {
        public int food;
        public int material;
        public int record;
        public int techFragment;
        public int specialMaterial;
        public int hp;
        public int maxHp;
        public int potionCount;
        public int baseCombatPower;
    }

    public void SaveProgress()
    {
        var data = new GameSaveData
        {
            food             = food,
            material         = material,
            record           = record,
            techFragment     = techFragment,
            specialMaterial  = specialMaterial,
            hp               = hp,
            maxHp            = maxHp,
            potionCount      = potionCount,
            baseCombatPower  = baseCombatPower
        };

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        TriggerResourcesChanged();
    }

    public void LoadProgress()
    {
        if (!PlayerPrefs.HasKey(SaveKey)) return;

        string json = PlayerPrefs.GetString(SaveKey);
        if (string.IsNullOrEmpty(json)) return;

        var data = JsonUtility.FromJson<GameSaveData>(json);
        if (data == null) return;

        food            = data.food;
        material        = data.material;
        record          = data.record;
        techFragment    = data.techFragment;
        specialMaterial = data.specialMaterial;
        hp              = data.hp;
        maxHp           = data.maxHp;
        potionCount     = data.potionCount;
        baseCombatPower = data.baseCombatPower;
        TriggerResourcesChanged();
    }

    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }
}