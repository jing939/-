using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class StatusEffect : MonoBehaviour
{
    [Header("Status Effect Stats")]
    public int bleedPotency = 0;
    public int bleedCount = 0;
    
    public int poisonPotency = 0;
    public int poisonCount = 0;
    
    public int rupturePotency = 0;
    public int ruptureCount = 0;
    
    public int sinkingPotency = 0;
    public int sinkingCount = 0;
    
    // UI Containers
    GameObject bleedContainer;
    TextMeshProUGUI bleedTextObj;
    
    GameObject poisonContainer;
    TextMeshProUGUI poisonTextObj;
    
    GameObject ruptureContainer;
    TextMeshProUGUI ruptureTextObj;
    
    GameObject sinkingContainer;
    TextMeshProUGUI sinkingTextObj;
    
    [HideInInspector] public Sprite bleedSprite;
    [HideInInspector] public Sprite poisonSprite;
    [HideInInspector] public Sprite ruptureSprite;
    [HideInInspector] public Sprite sinkingSprite;
    
    List<GameObject> activeStatusUI = new List<GameObject>();

    public void SetupUI(Transform parentCanvas, TextMeshProUGUI fontTemplate, Texture2D bleedTex = null, Texture2D poisonTex = null, Texture2D ruptureTex = null, Texture2D sinkingTex = null)
    {
        // 출혈 이미지 생성
        bleedContainer = new GameObject("BleedImage");
        bleedContainer.transform.SetParent(parentCanvas, false);
        bleedContainer.transform.SetAsFirstSibling();

        UnityEngine.UI.Image bleedImg = bleedContainer.AddComponent<UnityEngine.UI.Image>();
        bleedImg.raycastTarget = true;
        bleedImg.color = bleedTex != null ? Color.white : Color.red; 
        
        UnityEngine.UI.Button bleedBtn = bleedContainer.AddComponent<UnityEngine.UI.Button>();
        bleedBtn.onClick.AddListener(() => { if (StatsUIManager.instance != null) StatsUIManager.instance.ToggleStatusDetails(this); });
        if (bleedTex != null) 
        {
            bleedSprite = Sprite.Create(bleedTex, new Rect(0, 0, bleedTex.width, bleedTex.height), new Vector2(0.5f, 0.5f));
            bleedImg.sprite = bleedSprite;
        }
        bleedImg.rectTransform.sizeDelta = new Vector2(20, 20);
        
        GameObject btObj = new GameObject("Text");
        btObj.transform.SetParent(bleedContainer.transform, false);
        bleedTextObj = btObj.AddComponent<TextMeshProUGUI>();
        if (fontTemplate != null) bleedTextObj.font = fontTemplate.font;
        bleedTextObj.fontSize = 14;
        bleedTextObj.alignment = TextAlignmentOptions.Center;
        bleedTextObj.overflowMode = TextOverflowModes.Overflow;
        bleedTextObj.enableWordWrapping = false;
        bleedTextObj.raycastTarget = false;
        bleedTextObj.rectTransform.anchoredPosition = new Vector2(0, -15);
        
        // 독 이미지 생성
        poisonContainer = new GameObject("PoisonImage");
        poisonContainer.transform.SetParent(parentCanvas, false);
        poisonContainer.transform.SetAsFirstSibling();

        UnityEngine.UI.Image poisonImg = poisonContainer.AddComponent<UnityEngine.UI.Image>();
        poisonImg.raycastTarget = true;
        poisonImg.color = poisonTex != null ? Color.white : new Color(0.6f, 0f, 0.8f);
        
        UnityEngine.UI.Button poisonBtn = poisonContainer.AddComponent<UnityEngine.UI.Button>();
        poisonBtn.onClick.AddListener(() => { if (StatsUIManager.instance != null) StatsUIManager.instance.ToggleStatusDetails(this); });
        if (poisonTex != null) 
        {
            poisonSprite = Sprite.Create(poisonTex, new Rect(0, 0, poisonTex.width, poisonTex.height), new Vector2(0.5f, 0.5f));
            poisonImg.sprite = poisonSprite;
        }
        poisonImg.rectTransform.sizeDelta = new Vector2(20, 20);
        
        GameObject putObj = new GameObject("Text");
        putObj.transform.SetParent(poisonContainer.transform, false);
        poisonTextObj = putObj.AddComponent<TextMeshProUGUI>();
        if (fontTemplate != null) poisonTextObj.font = fontTemplate.font;
        poisonTextObj.fontSize = 14;
        poisonTextObj.alignment = TextAlignmentOptions.Center;
        poisonTextObj.overflowMode = TextOverflowModes.Overflow;
        poisonTextObj.enableWordWrapping = false;
        poisonTextObj.raycastTarget = false;
        poisonTextObj.rectTransform.anchoredPosition = new Vector2(0, -15);

        // 파열 이미지 생성
        ruptureContainer = new GameObject("RuptureImage");
        ruptureContainer.transform.SetParent(parentCanvas, false);
        ruptureContainer.transform.SetAsFirstSibling();

        UnityEngine.UI.Image ruptureImg = ruptureContainer.AddComponent<UnityEngine.UI.Image>();
        ruptureImg.raycastTarget = true;
        ruptureImg.color = ruptureTex != null ? Color.white : Color.yellow;
        
        UnityEngine.UI.Button ruptureBtn = ruptureContainer.AddComponent<UnityEngine.UI.Button>();
        ruptureBtn.onClick.AddListener(() => { if (StatsUIManager.instance != null) StatsUIManager.instance.ToggleStatusDetails(this); });
        if (ruptureTex != null) 
        {
            ruptureSprite = Sprite.Create(ruptureTex, new Rect(0, 0, ruptureTex.width, ruptureTex.height), new Vector2(0.5f, 0.5f));
            ruptureImg.sprite = ruptureSprite;
        }
        ruptureImg.rectTransform.sizeDelta = new Vector2(20, 20);
        
        GameObject rtObj = new GameObject("Text");
        rtObj.transform.SetParent(ruptureContainer.transform, false);
        ruptureTextObj = rtObj.AddComponent<TextMeshProUGUI>();
        if (fontTemplate != null) ruptureTextObj.font = fontTemplate.font;
        ruptureTextObj.fontSize = 14;
        ruptureTextObj.alignment = TextAlignmentOptions.Center;
        ruptureTextObj.overflowMode = TextOverflowModes.Overflow;
        ruptureTextObj.enableWordWrapping = false;
        ruptureTextObj.raycastTarget = false;
        ruptureTextObj.rectTransform.anchoredPosition = new Vector2(0, -15);

        // 침잠 이미지 생성
        sinkingContainer = new GameObject("SinkingImage");
        sinkingContainer.transform.SetParent(parentCanvas, false);
        sinkingContainer.transform.SetAsFirstSibling();

        UnityEngine.UI.Image sinkingImg = sinkingContainer.AddComponent<UnityEngine.UI.Image>();
        sinkingImg.raycastTarget = true;
        sinkingImg.color = sinkingTex != null ? Color.white : new Color(0.2f, 0.4f, 0.8f);
        
        UnityEngine.UI.Button sinkingBtn = sinkingContainer.AddComponent<UnityEngine.UI.Button>();
        sinkingBtn.onClick.AddListener(() => { if (StatsUIManager.instance != null) StatsUIManager.instance.ToggleStatusDetails(this); });
        if (sinkingTex != null) 
        {
            sinkingSprite = Sprite.Create(sinkingTex, new Rect(0, 0, sinkingTex.width, sinkingTex.height), new Vector2(0.5f, 0.5f));
            sinkingImg.sprite = sinkingSprite;
        }
        sinkingImg.rectTransform.sizeDelta = new Vector2(20, 20);
        
        GameObject stObj = new GameObject("Text");
        stObj.transform.SetParent(sinkingContainer.transform, false);
        sinkingTextObj = stObj.AddComponent<TextMeshProUGUI>();
        if (fontTemplate != null) sinkingTextObj.font = fontTemplate.font;
        sinkingTextObj.fontSize = 14;
        sinkingTextObj.alignment = TextAlignmentOptions.Center;
        sinkingTextObj.overflowMode = TextOverflowModes.Overflow;
        sinkingTextObj.enableWordWrapping = false;
        sinkingTextObj.raycastTarget = false;
        sinkingTextObj.rectTransform.anchoredPosition = new Vector2(0, -15);

        UpdateUI();
    }

    public void UpdatePosition(Vector3 basePosition)
    {
        if (activeStatusUI.Count == 0) return;

        float gap = 25f; // 아이콘 간의 간격
        // 아이콘 개수에 따라 시작 x 위치 계산 (전체가 중앙에 오도록)
        float totalWidth = (activeStatusUI.Count - 1) * gap;
        float startX = basePosition.x - (totalWidth / 2f);

        for (int i = 0; i < activeStatusUI.Count; i++)
        {
            if (activeStatusUI[i] != null)
                activeStatusUI[i].transform.position = new Vector3(startX + (i * gap), basePosition.y, basePosition.z);
        }
    }

    public void BringToFront(bool forward)
    {
        if (bleedContainer != null)
        {
            if (forward) bleedContainer.transform.SetAsLastSibling();
            else bleedContainer.transform.SetAsFirstSibling();
        }
        if (poisonContainer != null)
        {
            if (forward) poisonContainer.transform.SetAsLastSibling();
            else poisonContainer.transform.SetAsFirstSibling();
        }
        if (ruptureContainer != null)
        {
            if (forward) ruptureContainer.transform.SetAsLastSibling();
            else ruptureContainer.transform.SetAsFirstSibling();
        }
        if (sinkingContainer != null)
        {
            if (forward) sinkingContainer.transform.SetAsLastSibling();
            else sinkingContainer.transform.SetAsFirstSibling();
        }
    }

    private void OnDestroy()
    {
        if (bleedContainer != null) Destroy(bleedContainer);
        if (poisonContainer != null) Destroy(poisonContainer);
        if (ruptureContainer != null) Destroy(ruptureContainer);
        if (sinkingContainer != null) Destroy(sinkingContainer);
    }

    public void AddBleed(int potency, int count)
    {
        bleedPotency += potency;
        bleedCount += count; 
        if (bleedPotency > 0 && bleedCount <= 0) bleedCount = 1;
        UpdateUI();
    }

    public void AddPoison(int potency, int count)
    {
        poisonPotency += potency;
        poisonCount += count;
        if (poisonPotency > 0 && poisonCount <= 0) poisonCount = 1;
        UpdateUI();
    }

    public void AddRupture(int potency, int count)
    {
        rupturePotency += potency;
        ruptureCount += count;
        if (rupturePotency > 0 && ruptureCount <= 0) ruptureCount = 1;
        UpdateUI();
    }

    public void AddSinking(int potency, int count)
    {
        sinkingPotency += potency;
        sinkingCount += count;
        if (sinkingPotency > 0 && sinkingCount <= 0) sinkingCount = 1;
        UpdateUI();
    }

    public void OnCoinTossed()
    {
        if (bleedCount > 0 && bleedPotency > 0)
        {
            ApplyDamage(bleedPotency, "출혈");
            bleedCount--;
            if (bleedCount <= 0) bleedPotency = 0;
            UpdateUI();
        }
    }

    public void OnTurnEnd()
    {
        if (poisonCount > 0 && poisonPotency > 0)
        {
            ApplyDamage(poisonPotency, "독");
            poisonCount--;
            if (poisonCount <= 0) poisonPotency = 0;
            UpdateUI();
        }
    }

    // 피격 시 호출됨 (무한 루프 방지를 위해 isStatusDamage 플래그 사용)
    public void OnHit()
    {
        if (ruptureCount > 0 && rupturePotency > 0)
        {
            ApplyDamage(rupturePotency, "파열", true);
            ruptureCount--;
            if (ruptureCount <= 0) rupturePotency = 0;
        }

        if (sinkingCount > 0 && sinkingPotency > 0)
        {
            ApplySPDamage(sinkingPotency);
            sinkingCount--;
            if (sinkingCount <= 0) sinkingPotency = 0;
        }
        UpdateUI();
    }

    private void ApplyDamage(int damage, string effectName, bool isStatusDamage = false)
    {
        Debug.Log($"{gameObject.name}이(가) {effectName}로 인해 {damage}의 피해를 입었습니다!");
        
        PlayerMove pm = GetComponent<PlayerMove>();
        if (pm != null) pm.TakeDamage(damage, isStatusDamage);
        
        RangedPlayerMove rpm = GetComponent<RangedPlayerMove>();
        if (rpm != null) rpm.TakeDamage(damage, isStatusDamage);
        
        Enemy enemy = GetComponent<Enemy>();
        if (enemy != null) enemy.TakeDamage(damage, isStatusDamage);
        
        if (EffectManager.instance != null) EffectManager.instance.SpawnHitEffect(transform.position);
    }

    private void ApplySPDamage(int amount)
    {
        Debug.Log($"{gameObject.name}이(가) 침잠으로 인해 {amount}의 정신력 피해를 입었습니다!");
        
        PlayerMove pm = GetComponent<PlayerMove>();
        if (pm != null) pm.AddSP(-amount);

        RangedPlayerMove rpm = GetComponent<RangedPlayerMove>();
        if (rpm != null) rpm.AddSP(-amount);

        Enemy enemy = GetComponent<Enemy>();
        if (enemy != null) enemy.AddSP(-amount);
    }

    public void UpdateUI()
    {
        if (bleedContainer != null)
        {
            bool hasBleed = bleedCount > 0;
            bleedContainer.SetActive(hasBleed);
            if (hasBleed && bleedTextObj != null) bleedTextObj.text = $"{bleedPotency} {bleedCount}";
            
            if (hasBleed && !activeStatusUI.Contains(bleedContainer)) activeStatusUI.Add(bleedContainer);
            else if (!hasBleed && activeStatusUI.Contains(bleedContainer)) activeStatusUI.Remove(bleedContainer);
        }
        
        if (poisonContainer != null)
        {
            bool hasPoison = poisonCount > 0;
            poisonContainer.SetActive(hasPoison);
            if (hasPoison && poisonTextObj != null) poisonTextObj.text = $"{poisonPotency} {poisonCount}";
            
            if (hasPoison && !activeStatusUI.Contains(poisonContainer)) activeStatusUI.Add(poisonContainer);
            else if (!hasPoison && activeStatusUI.Contains(poisonContainer)) activeStatusUI.Remove(poisonContainer);
        }

        if (ruptureContainer != null)
        {
            bool hasRupture = ruptureCount > 0;
            ruptureContainer.SetActive(hasRupture);
            if (hasRupture && ruptureTextObj != null) ruptureTextObj.text = $"{rupturePotency} {ruptureCount}";
            
            if (hasRupture && !activeStatusUI.Contains(ruptureContainer)) activeStatusUI.Add(ruptureContainer);
            else if (!hasRupture && activeStatusUI.Contains(ruptureContainer)) activeStatusUI.Remove(ruptureContainer);
        }

        if (sinkingContainer != null)
        {
            bool hasSinking = sinkingCount > 0;
            sinkingContainer.SetActive(hasSinking);
            if (hasSinking && sinkingTextObj != null) sinkingTextObj.text = $"{sinkingPotency} {sinkingCount}";
            
            if (hasSinking && !activeStatusUI.Contains(sinkingContainer)) activeStatusUI.Add(sinkingContainer);
            else if (!hasSinking && activeStatusUI.Contains(sinkingContainer)) activeStatusUI.Remove(sinkingContainer);
        }
    }
}
