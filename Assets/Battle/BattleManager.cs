using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public enum StatusType { None, Bleed, Poison, Rupture, Sinking, Heal, HealSP, AoE }

[System.Serializable]
public class SkillEffect
{
    public StatusType statusType;
    public int potency;   // ?곹깭?댁긽: ?꾨젰 / Heal: ?뚮났??/ AoE: 異붽? ?곕?吏€ 鍮꾩쑉(0~100%)
    public int count;     // ?곹깭?댁긽: ?ㅽ깮 ?딆닔 (誘몄궗????0)
}

[System.Serializable]
public class CoinEffect
{
    public string animationTrigger = "Attack"; // 肄붿씤蹂??숈옉 ?몃━嫄??대쫫
    public float attackSpeed = 1f; // 肄붿씤蹂?怨듦꺽 ?띾룄 (諛곗냽)
    public GameObject customEffectPrefab; // [신규] 해당 모션 전용 이펙트 프리팹
    public SkillEffect[] effects;
}

[System.Serializable]
public class SkillData
{
    public string skillName;
    public bool isDefenseSkill = false;
    public int defenseLevel = 30; // 諛⑹뼱 ?ㅽ궗??諛⑹뼱 ?덈꺼
    public int basePower;
    public int coinPower;
    public int coinCount;
    public SkillEffect[] effects;
    public CoinEffect[] coinEffects;

    [Header("Skill UI Images")]
    public Sprite assignedSlotSprite; // ???ㅽ궗???좏깮?덉쓣 ??蹂댁뿬以??대?吏
    public Sprite readySlotSprite; // ???ㅽ궗濡??寃?吏?뺤쓣 ?꾨즺?덉쓣 ??蹂댁뿬以??대?吏
}

public class BattleManager : MonoBehaviour
{
    public static BattleManager instance;

    [Header("Deprecated References (?댁젣 ?숈쟻 ?좊떦??")]
    public PlayerMove player;
    public RangedPlayerMove rangedPlayer;

    public Enemy selectedEnemy;

    [Header("Limbus Skill Slots")]
    public List<FloatingSkillSlot> skillSlots = new List<FloatingSkillSlot>();

    [Header("Limbus Coin Config")]
    public string selectedSkillName;
    public bool activeSkillIsDefense;
    public int activeDefenseLevel;
    public int activeBasePower;
    public int activeCoinPower;
    public int activeCoinCount;
    public SkillEffect[] activeSkillEffects; // ?뚮젅?댁뼱 ?꾩옱 ?ㅽ궗 ?④낵
    public CoinEffect[] activeCoinEffects; // ?뚮젅?댁뼱 ?꾩옱 肄붿씤蹂??④낵
    
    public int minPower => activeBasePower;
    public int maxPower => activeBasePower + (activeCoinPower * activeCoinCount);
    
    [Header("Background Settings")]
    public Texture2D backgroundTexture;
    public SpriteRenderer backgroundRenderer;

    public enum SkillId { None, Skill1, Skill2, Skill3, Skill4 }
    public SkillId currentActiveSkill; // ?대뼡 ?ㅽ궗??諛쒕룞 以묒씤吏€ 異붿쟻
    
    public bool isClashing = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        InitializeBackground();
    }

    void InitializeBackground()
    {
        if (backgroundRenderer == null && backgroundTexture != null)
        {
            GameObject bgObj = new GameObject("BattleBackground");
            bgObj.transform.SetParent(this.transform);
            backgroundRenderer = bgObj.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = Sprite.Create(backgroundTexture, 
                new Rect(0, 0, backgroundTexture.width, backgroundTexture.height), 
                new Vector2(0.5f, 0.5f));
        }

        if (backgroundRenderer != null)
        {
            // [?섏젙] 諛곌꼍??移대찓?쇱쓽 ?꾩옱 以묒떖 ?꾩튂??留욎떠 諛곗튂?⑸땲??(Z=10)
            Camera cam = Camera.main;
            Vector3 camPos = cam != null ? cam.transform.position : Vector3.zero;
            backgroundRenderer.transform.position = new Vector3(camPos.x, camPos.y, 10f);
            
            // [異붽?] ?붾㈃??苑?李⑤룄濡??ㅼ???議곗젅
            if (cam != null && cam.orthographic)
            {
                float height = cam.orthographicSize * 2;
                float width = height * cam.aspect;
                
                Sprite s = backgroundRenderer.sprite;
                if (s != null)
                {
                    float unitWidth = s.texture.width / s.pixelsPerUnit;
                    float unitHeight = s.texture.height / s.pixelsPerUnit;
                    
                    backgroundRenderer.transform.localScale = new Vector3(width / unitWidth, height / unitHeight, 1);
                }
            }
        }
    }

    public void SetBackground(Sprite newSprite)
    {
        if (backgroundRenderer != null)
        {
            backgroundRenderer.sprite = newSprite;
        }
    }

    public void ConfigureSkill(GameObject attacker, SkillId skill)
    {
        currentActiveSkill = skill;
        
        int index = (int)skill - 1; // Skill1 = 0, Skill2 = 1, ...
        if (index < 0) return;

        SkillData data = null;
        PlayerMove pm = attacker.GetComponent<PlayerMove>();
        RangedPlayerMove rpm = attacker.GetComponent<RangedPlayerMove>();

        if (pm != null && index < pm.skills.Length) data = pm.skills[index];
        else if (rpm != null && index < rpm.skills.Length) data = rpm.skills[index];

        if (data != null && data.coinCount > 0)
        {
            activeBasePower = data.basePower <= 0 ? 4 : data.basePower;
            activeCoinPower = data.coinPower <= 0 ? 3 : data.coinPower;
            activeCoinCount = data.coinCount <= 0 ? 1 : data.coinCount;
            selectedSkillName = string.IsNullOrEmpty(data.skillName) ? "이름 없는 스킬" : data.skillName;
            activeSkillIsDefense = data.isDefenseSkill;
            activeDefenseLevel = data.defenseLevel;
            activeSkillEffects = data.effects;
            activeCoinEffects = data.coinEffects;
        }
        else
        {
            // ?대갚 (?곗씠?곌? ?녾굅??肄붿씤 媛쒖닔媛 0?쇰줈 ?명똿?섏? ?딆븯????
            activeBasePower = 4; activeCoinPower = 2; activeCoinCount = 2; selectedSkillName = "湲곕낯 怨듦꺽 (?ㅼ젙 ?꾩슂)";
            activeSkillIsDefense = false;
            activeDefenseLevel = 0;
            activeSkillEffects = null;
            activeCoinEffects = null;
        }
    }

    // 蹂댁“ ?⑥닔: ?뚮젅?댁뼱媛 二쎌뿀?붿? ?먮퀎
    bool IsPlayerDead(GameObject obj)
    {
        if (obj == null) return true;
        PlayerMove pm = obj.GetComponent<PlayerMove>();
        if (pm != null && pm.isDead) return true;
        RangedPlayerMove rpm = obj.GetComponent<RangedPlayerMove>();
        if (rpm != null && rpm.isDead) return true;
        return false;
    }

    // ?꾪닾 ?쒖옉 踰꾪듉??
    public void StartBattle()
    {
        if (isClashing) return;

        // ?ъ뿉 ?덈뒗 紐⑤뱺 ?щ’??媛뺤젣濡?李얠뒿?덈떎
        FloatingSkillSlot[] allSlots = FindObjectsByType<FloatingSkillSlot>(FindObjectsSortMode.None);
        List<FloatingSkillSlot> validSlots = new List<FloatingSkillSlot>();

        // 寃利? ?섎굹?쇰룄 ??梨꾩썙???덉쑝硫??꾪닾瑜?嫄곕??⑸땲??
        foreach (var slot in allSlots)
        {
            if (slot.ownerPlayer == null) continue;
            
            // 二쎌? ?뚮젅?댁뼱???щ’? ?꾩쟾??臾댁떆?⑸땲??(?ㅽ궗 ??梨꾩썙???섏뼱媛寃???
            if (IsPlayerDead(slot.ownerPlayer)) continue;

            if (slot.assignedSkill == SkillId.None || slot.assignedTarget == null)
            {
                // ?붾㈃ ?띿뒪??異쒕젰 ?놁씠, 肄섏넄 濡쒓렇留??④린怨??꾪닾 ?쒖옉??議곗슜??痍⑥냼?⑸땲??
                Debug.LogWarning("?ㅽ궗??鍮꾩뼱?덇굅??怨듦꺽 ??곸씠 吏?뺣릺吏 ?딆? ?щ’???덉뒿?덈떎! ?꾪닾 媛쒖떆 遺덇?.");
                return; 
            }
            validSlots.Add(slot);
        }

        if (validSlots.Count > 0)
        {
            StartCoroutine(BattleSequenceRoutine(validSlots));
        }
    }

    IEnumerator BattleSequenceRoutine(List<FloatingSkillSlot> slots)
    {
        isClashing = true; // ?꾩껜 由대젅?닿? ?앸궇 ?뚭퉴吏 ?쎌쓣 寃곷땲??
        
        // ?꾪닾 ?쒖옉 ???ㅽ궗 怨좊Ⅴ??踰꾪듉 ?④린湲?
        FloatingSkillSlot[] allSlots = FindObjectsByType<FloatingSkillSlot>(FindObjectsSortMode.None);
        foreach (var s in allSlots) if (s != null) s.gameObject.SetActive(false);

        // ?대쾲 ???꾪닾??李몄뿬??紐⑤뱺 ?좊떅?ㅼ쓣 湲곗뼲?⑸땲??(?섏쨷???쒓볼踰덉뿉 蹂듦??쒗궎湲??꾪빐)
        HashSet<GameObject> participants = new HashSet<GameObject>();
        HashSet<Enemy> alreadyClashedEnemies = new HashSet<Enemy>(); // ?대쾲 ?댁뿉 ?대? ?⑹쓣 ????異붿쟻
        
        foreach (var slot in slots)
        {
            if (slot.ownerPlayer == null) continue;
            
            participants.Add(slot.ownerPlayer);
            if (slot.assignedTarget != null) participants.Add(slot.assignedTarget.gameObject);

            // 異붽?: ?먯떊??李⑤?媛 ?붾뒗???대? ?댁쟾 ?꾪닾???ы뙆濡?二쎌뼱踰꾨졇?ㅻ㈃ 臾댁떆!
            if (IsPlayerDead(slot.ownerPlayer))
            {
                Debug.Log($"{slot.ownerPlayer.name}?(?? ?대? ?щ쭩?섏뿬 怨듦꺽???섑뻾?????놁뒿?덈떎.");
                slot.ClearSlot();
                continue;
            }

            // 異붽?: 留뚯빟 ?욎뿉???뚮┛ 怨듦꺽 ?뚮Ц??紐ъ뒪?곌? ?대? 二쎌뿀?ㅻ㈃ 怨듦꺽 痍⑥냼!
            if (slot.assignedTarget == null || slot.assignedTarget.hp <= 0)
            {
                Debug.Log($"{slot.ownerPlayer.name}???寃잛씠 ?대? ?щ쭩?섏뿬 怨듦꺽???ㅽ궢?⑸땲??");
                slot.ClearSlot();
                continue;
            }

            selectedEnemy = slot.assignedTarget;
            ConfigureSkill(slot.ownerPlayer, slot.assignedSkill);
            
            // ?쇰갑 怨듦꺽 ?щ? 寃곗젙: ?寃잛씠 ?대쾲 ?댁뿉 ?대? ?⑹쓣 ?덈떎硫??쇰갑 怨듦꺽????
            bool isOneSided = alreadyClashedEnemies.Contains(selectedEnemy);
            if (!isOneSided) alreadyClashedEnemies.Add(selectedEnemy);

            // 湲곗〈 ??濡쒖쭅???앸궇 ?뚭퉴吏 ?ш린???듭㎏濡?湲곕떎由쎈땲??(?쒖감 ?ㅽ뻾 ?듭떖)
            yield return StartCoroutine(ClashRoutine(slot.ownerPlayer, isOneSided));
            
            slot.ClearSlot(); // ?ъ슜???щ’? 利됱떆 鍮꾩?
            
            // ?ㅼ쓬 ???怨듦꺽 ???꾩＜ 吏㏃? ?ъ슫
            yield return new WaitForSeconds(0.5f);
        }

        // [?좉퇋] 紐⑤뱺 由대젅??怨듦꺽???앸궃 ?? 李몄뿬?덈뜕 紐⑤뱺 罹먮┃?곕뱾??媛곸옄???ㅻ━吏???꾩튂濡?蹂듦??쒗궎嫄곕굹 ?댁옣?쒗궢?덈떎.
        foreach (var unit in participants)
        {
            if (unit == null) continue;

            PlayerMove pm = unit.GetComponent<PlayerMove>();
            RangedPlayerMove rpm = unit.GetComponent<RangedPlayerMove>();
            bool dead = (pm != null && pm.isDead) || (rpm != null && rpm.isDead);

            if (dead)
            {
                // 二쎌? ?뚮젅?댁뼱??蹂듦??섏? ?딄퀬 ?붾㈃ 諛뽰쑝濡??댁옣!
                if (pm != null) pm.StartRunAway();
                if (rpm != null) rpm.StartRunAway();
            }
            else
            {
                // ?댁븘?덈뒗 ?좊떅留??먮옒 ?먮━濡?蹂듦?
                Vector3 startPos = GetInitialPosition(unit);
                StartCoroutine(MoveSingleTransform(unit.transform, startPos, 0.5f));
            }
        }

        yield return new WaitForSeconds(0.6f);

        // [?곹깭 ?댁긽] ??醫낅즺 ???붿긽 ?곕?吏 諛쒕룞
        StatusEffect[] allStatus = FindObjectsByType<StatusEffect>(FindObjectsSortMode.None);
        foreach(var status in allStatus)
        {
            if(status != null) status.OnTurnEnd();
        }

        // [蹂댄샇留?珥덇린?? ?댁씠 ?앸굹硫??⑥? 蹂댄샇留됱? 紐⑤몢 利앸컻?⑸땲??
        PlayerMove[] allP = FindObjectsByType<PlayerMove>(FindObjectsSortMode.None);
        foreach(var p in allP) if (p != null) p.ClearShield();

        RangedPlayerMove[] allRP = FindObjectsByType<RangedPlayerMove>(FindObjectsSortMode.None);
        foreach(var rp in allRP) if (rp != null) rp.ClearShield();

        Enemy[] allE = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach(var e in allE) if (e != null) e.ClearShield();

        isClashing = false; // 紐⑤뱺 由대젅???꾪닾 醫낅즺
        
        // 由대젅???꾪닾媛€ ?????앸궃 ???뱁뙣 ?먯젙
        bool isGameOver = CheckBattleEnd();

        if (isGameOver)
        {
            bool win = IsPlayerVictory();
            UpdatePlayerHpToGameManager();
            StartCoroutine(ReturnToExploreScene(win));
        }
        else
        {
            // 寃뚯엫???앸궃吏€ ?딆븯?ㅻ㈃ ?ㅼ쓬 ?댁쓣 ?꾪빐 ?ㅽ궗 踰꾪듉 ?ㅼ떆 ?쒖떆
            foreach (var s in allSlots) if (s != null) s.gameObject.SetActive(true);
        }
    }

    bool IsPlayerVictory()
    {
        PlayerMove[] players = FindObjectsByType<PlayerMove>(FindObjectsSortMode.None);
        RangedPlayerMove[] rPlayers = FindObjectsByType<RangedPlayerMove>(FindObjectsSortMode.None);
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        bool anyPlayerAlive = false;
        foreach (var p in players) if (!p.isDead) anyPlayerAlive = true;
        foreach (var rp in rPlayers) if (!rp.isDead) anyPlayerAlive = true;

        bool anyEnemyAlive = false;
        foreach (var e in enemies) if (e != null && e.hp > 0) anyEnemyAlive = true;

        return anyPlayerAlive && !anyEnemyAlive;
    }

    IEnumerator ClashRoutine(GameObject attackerObj, bool isOneSided = false)
    {
        // ?곹깭?댁긽 UI ?욎쑝濡?媛?몄삤湲?
        StatusEffect pStatus = attackerObj.GetComponent<StatusEffect>();
        StatusEffect eStatus = selectedEnemy != null ? selectedEnemy.GetComponent<StatusEffect>() : null;

        if (pStatus != null) pStatus.BringToFront(true);
        if (eStatus != null) eStatus.BringToFront(true);

        // 怨듦꺽?먯? 諛⑹뼱?먯쓽 ?먮옒 ?꾩튂 (???쒖젏???꾨땲??寃뚯엫 ?쒖옉 ?쒖젏???꾩튂媛 ?꾩슂??
        Vector3 attackerHome = GetInitialPosition(attackerObj);
        Vector3 enemyHome = selectedEnemy != null ? GetInitialPosition(selectedEnemy.gameObject) : Vector3.zero;

        // 怨듦꺽??而댄룷?뚰듃 ?앸퀎 (洹쇨굅由ъ씤吏 ?먭굅由ъ씤吏 ?먮퀎)
        PlayerMove meleeAttacker = attackerObj.GetComponent<PlayerMove>();
        RangedPlayerMove rangedAttacker = attackerObj.GetComponent<RangedPlayerMove>();

        // 肄붿씤 ?욌㈃???섏삱 ?뺣쪧 媛?몄삤湲?
        float pProb = meleeAttacker != null ? meleeAttacker.headProbability : (rangedAttacker != null ? rangedAttacker.headProbability : 0.5f);
        float eProb = selectedEnemy != null ? selectedEnemy.headProbability : 0.5f;

        if (selectedEnemy != null) selectedEnemy.SelectSkill();

        // (湲곗〈 Skill3 泥대젰 媛먯냼 ?꾩떆 濡쒖쭅 ?쒓굅)

        int pCoinsLeft = activeSkillIsDefense ? 0 : activeCoinCount;
        int eCoinsLeft = (selectedEnemy != null && !isOneSided) ? (selectedEnemy.eActiveSkillIsDefense ? 0 : selectedEnemy.eCoinCount) : 0;

        // [?좉퇋] 諛⑹뼱 ?ㅽ궗? ?먭린媛 怨듦꺽??諛쏆쓣 ?뚮쭔 ?ъ슜?쒕떎. (????怨듦꺽?????섎뒗 寃쎌슦 ?ㅽ궢)
        if (pCoinsLeft == 0 && eCoinsLeft == 0)
        {
            if (pStatus != null) pStatus.BringToFront(false);
            if (eStatus != null) eStatus.BringToFront(false);
            yield break; // 吏?곗씠??紐⑥뀡 ?놁씠 利됱떆 痍⑥냼 泥섎━
        }

        if (BattleUIManager.instance != null)
        {
            string pSkill = selectedSkillName;
            string eSkill = (selectedEnemy != null) ? selectedEnemy.eActiveSkillName : "";
            BattleUIManager.instance.ShowClashUI(true, attackerObj, selectedEnemy != null ? selectedEnemy.gameObject : null, pSkill, eSkill, isOneSided);
        }
        
        int pRoll = 0;
        int eRoll = 0;
        int clashCount = 0;

        float atkDefModifier = 1f;
        int attackerAttackLevel = meleeAttacker != null ? meleeAttacker.attackLevel : (rangedAttacker != null ? rangedAttacker.attackLevel : 0);

        if (selectedEnemy != null)
        {
            int enemyDefenseLevel = selectedEnemy.defenseLevel;
            if (selectedEnemy.eActiveSkillIsDefense && selectedEnemy.eActiveDefenseLevel > enemyDefenseLevel)
            {
                enemyDefenseLevel = selectedEnemy.eActiveDefenseLevel;
            }
            atkDefModifier = 1f - ((enemyDefenseLevel - attackerAttackLevel) * 0.03f);
        }

        // 紐ъ뒪??怨듦꺽 ???곗씪 蹂댁젙移?
        float enemyAtkDefModifier = 1f;
        int enemyAttackLevel = selectedEnemy != null ? selectedEnemy.attackLevel : 0;
        int playerDefenseLevel = meleeAttacker != null ? meleeAttacker.defenseLevel : (rangedAttacker != null ? rangedAttacker.defenseLevel : 0);
        
        if (activeSkillIsDefense && activeDefenseLevel > playerDefenseLevel)
        {
            playerDefenseLevel = activeDefenseLevel;
        }
        
        enemyAtkDefModifier = 1f - ((playerDefenseLevel - enemyAttackLevel) * 0.03f);

        Vector3 clashCenter = Vector3.zero;
        if (selectedEnemy != null)
            clashCenter = (attackerHome + enemyHome) / 2f;

        // [異붽?] ?⑹쓣 移섎뒗 ?꾩튂瑜?以묒떖?쇰줈 移대찓??以뚯씤
        if (CameraShake.instance != null) 
        {
            if (!isOneSided && pCoinsLeft > 0 && eCoinsLeft > 0)
                CameraShake.instance.FocusOnClash(clashCenter, 0.65f, 0.4f);
            else
                CameraShake.instance.ResetFocus(0.4f); // 일방 공격일 때는 줌인하지 않음
        }

        Animator pAnim = attackerObj.GetComponentInChildren<Animator>();
        Animator eAnim = selectedEnemy != null ? selectedEnemy.GetComponentInChildren<Animator>() : null;

        if (pCoinsLeft > 0 && eCoinsLeft > 0)
        {
            if (pAnim != null) pAnim.Play("Run", -1, 0f);
            if (eAnim != null) eAnim.Play("Run", -1, 0f);

            // [蹂寃? ?⑹쓣 移??뚮뒗 ????以묒븰?쇰줈 紐⑥엯?덈떎.
            yield return MoveTwoTransforms(attackerObj.transform, selectedEnemy != null ? selectedEnemy.transform : null, clashCenter + Vector3.left * 1.5f, clashCenter + Vector3.right * 1.5f, 0.4f);
        }
        else
        {
            // ?쒖そ留?怨듦꺽(?쇰갑 怨듦꺽)?섍굅???쒖そ??諛⑹뼱??寃쎌슦, ?ㅺ?媛??履쎈쭔 Run ?ъ깮 (?쒖옄由?諛⑹뼱??Guard)
            if (pCoinsLeft > 0 && pAnim != null) pAnim.Play("Run", -1, 0f);
            else if (activeSkillIsDefense && pAnim != null) pAnim.Play("Guard", -1, 0f);

            if (eCoinsLeft > 0 && eAnim != null) eAnim.Play("Run", -1, 0f);
            else if (selectedEnemy != null && selectedEnemy.eActiveSkillIsDefense && eAnim != null) eAnim.Play("Guard", -1, 0f);
        }

        while (pCoinsLeft > 0 && eCoinsLeft > 0)
        {
            // [蹂寃? ?꾩옱 ?꾩튂?ㅼ쓽 以묎컙 吏?먯뿉???ㅼ떆 留뚮굹 ?⑹쓣 移⑸땲??
            Vector3 currentMid = (attackerObj.transform.position + (selectedEnemy != null ? selectedEnemy.transform.position : attackerObj.transform.position)) / 2f;
            yield return MoveTwoTransforms(attackerObj.transform, selectedEnemy != null ? selectedEnemy.transform : null, currentMid + Vector3.left * 1.5f, currentMid + Vector3.right * 1.5f, 0.2f);
            
            if (pAnim != null)
            {
                if (activeSkillIsDefense) pAnim.Play("Guard", -1, 0f);
                else pAnim.Play("Clash", -1, 0f);
            }
            if (eAnim != null)
            {
                if (selectedEnemy.eActiveSkillIsDefense) eAnim.Play("Guard", -1, 0f);
                else eAnim.Play("Clash", -1, 0f);
            }

            clashCount++;

            if (BattleUIManager.instance != null)
                BattleUIManager.instance.SetupClashCoins(pCoinsLeft, eCoinsLeft);

            pRoll = activeBasePower;
            eRoll = selectedEnemy != null ? selectedEnemy.eBasePower : 0;
            
            if (BattleUIManager.instance != null) BattleUIManager.instance.UpdateClashNumbers(pRoll, eRoll);

            int maxI = Mathf.Max(pCoinsLeft, eCoinsLeft);
            // 肄붿씤??留롮쓣?섎줉 ?먯젙 ?띾룄媛€ 鍮⑤씪吏?(1~2媛? 0.4珥? 10媛?: 0.1珥?
            float clashCoinInterval = Mathf.Lerp(0.4f, 0.1f, Mathf.Clamp01((maxI - 1) / 9f));
            for(int i = 0; i < maxI; i++)
            {
                yield return new WaitForSeconds(clashCoinInterval);

                if (i < pCoinsLeft)
                {
                    bool head = Random.value <= pProb;
                    if (BattleUIManager.instance != null) BattleUIManager.instance.UpdatePlayerCoin(i, head ? BattleUIManager.CoinState.Head : BattleUIManager.CoinState.Tail);
                    if (head) pRoll += activeCoinPower;
                }

                if (i < eCoinsLeft)
                {
                    bool head = Random.value <= eProb;
                    if (BattleUIManager.instance != null) BattleUIManager.instance.UpdateEnemyCoin(i, head ? BattleUIManager.CoinState.Head : BattleUIManager.CoinState.Tail);
                    if (head && selectedEnemy != null) eRoll += selectedEnemy.eCoinPower;
                }

                if (BattleUIManager.instance != null) BattleUIManager.instance.UpdateClashNumbers(pRoll, eRoll);
            }

            yield return new WaitForSeconds(0.6f);

            // [蹂€寃? ???뱁뙣 ?먯젙 ?쒖젏???묒そ 紐⑤몢 異쒗삁 諛쒕룞
            StatusEffect clashPStatus = attackerObj.GetComponent<StatusEffect>();
            if(clashPStatus != null) clashPStatus.OnCoinTossed();
            StatusEffect clashEStatus = selectedEnemy != null ? selectedEnemy.GetComponent<StatusEffect>() : null;
            if(clashEStatus != null) clashEStatus.OnCoinTossed();

            bool isPDead = (meleeAttacker != null && meleeAttacker.isDead) || (rangedAttacker != null && rangedAttacker.isDead);
            bool isEDead = (selectedEnemy == null || selectedEnemy.hp <= 0);

            if (isPDead) pCoinsLeft = 0;
            if (isEDead) eCoinsLeft = 0;

            if (isPDead || isEDead) break;

            if (EffectManager.instance != null) EffectManager.instance.SpawnClashEffect(clashCenter);
            if (CameraShake.instance != null) CameraShake.instance.LightShake();

            if (pRoll > eRoll)
            {
                eCoinsLeft--;
                // 합을 겨룰 때 서로 다른 방향으로 밀려남 (튕겨나감)
                attackerObj.transform.position += Vector3.left * 0.4f;
                if (selectedEnemy != null) selectedEnemy.transform.position += Vector3.right * 0.4f;
                
                // 승자는 뒤로 밀려날 때 자연스럽게 대기 자세로 돌아옴
                                if (eAnim != null) eAnim.Play("Hit", -1, 0f);
            }
            else if (pRoll < eRoll)
            {
                pCoinsLeft--;
                // 합을 겨룰 때 서로 다른 방향으로 밀려남 (튕겨나감)
                attackerObj.transform.position += Vector3.left * 0.4f;
                if (selectedEnemy != null) selectedEnemy.transform.position += Vector3.right * 0.4f;

                if (pAnim != null) pAnim.Play("Hit", -1, 0f);
                // 승자는 뒤로 밀려날 때 자연스럽게 대기 자세로 돌아옴
                            }
            else
            {
                // 臾댁듅遺€???뚮룄 ?쒕줈 諛€?ㅻ굹硫??쇨꺽(異⑷꺽) 紐⑥뀡
                attackerObj.transform.position += Vector3.left * 0.4f;
                if (selectedEnemy != null) selectedEnemy.transform.position += Vector3.right * 0.4f;

                if (pAnim != null) pAnim.Play("Hit", -1, 0f);
                if (eAnim != null) eAnim.Play("Hit", -1, 0f);
            }

            yield return new WaitForSeconds(0.3f);
        }

        bool playerWonTotal = pCoinsLeft > 0;
        int winnerCoinsLeft = playerWonTotal ? pCoinsLeft : eCoinsLeft;

        float finalClashModifier = 1f + (clashCount * 0.03f); 

        // (寃곌낵 ?띿뒪??異쒕젰 ?쒓굅)

        // [?좉퇋] ?쇰갑 怨듦꺽???꾨땶 '????吏꾪뻾?섏뿀???뚮쭔, 理쒖쥌 ?뱁뙣???곕씪 SP 諛섏쁺 (臾댁듅遺€ ?쒖쇅)
        bool anyDefense = activeSkillIsDefense || (selectedEnemy != null && selectedEnemy.eActiveSkillIsDefense);
        if (!isOneSided && !anyDefense && (pCoinsLeft > 0 || eCoinsLeft > 0))
        {
            if (playerWonTotal)
            {
                if (meleeAttacker != null) meleeAttacker.AddSP(10);
                if (rangedAttacker != null) rangedAttacker.AddSP(10);
                if (selectedEnemy != null) selectedEnemy.AddSP(-5);
            }
            else
            {
                if (meleeAttacker != null) meleeAttacker.AddSP(-5);
                if (rangedAttacker != null) rangedAttacker.AddSP(-5);
                if (selectedEnemy != null) selectedEnemy.AddSP(10);
            }
        }

        if (playerWonTotal)
        {
            // [蹂€寃? ?뚮젅?댁뼱媛€ ?닿꼈???뚮쭔 怨듦꺽?먭? ?곸쓽 ?꾩튂??留욎떠 ?대룞?⑸땲??
            Vector3 targetPos = selectedEnemy != null ? selectedEnemy.transform.position : Vector3.zero;
            float attackDist = (meleeAttacker != null) ? 1.2f : 5.0f; // ?먭굅由щ㈃ 5.0 嫄곕━ ?좎?
            Vector3 finalAtkPos = targetPos + Vector3.left * attackDist;

            yield return MoveTwoTransforms(attackerObj.transform, null, finalAtkPos, Vector3.zero, 0.3f);

            // (?먭굅由?罹먮┃?곕룄 ?댁젣 吏€?뺣맂 嫄곕━?먯꽌 ?ш꺽?섎?濡?湲곗〈??'?먮옒 ?먮━ 蹂듦?' 肄붾뱶???쒓굅?⑸땲??

            int enemyDefenseCoins = (selectedEnemy != null && selectedEnemy.eActiveSkillIsDefense) ? selectedEnemy.eCoinCount : 0;
            if (BattleUIManager.instance != null) BattleUIManager.instance.SetupClashCoins(winnerCoinsLeft, enemyDefenseCoins);
            int currentPower = activeBasePower;
            if (BattleUIManager.instance != null) BattleUIManager.instance.UpdateClashNumbers(currentPower, 0);

            int eDefensePower = 0;

            if (selectedEnemy != null && selectedEnemy.eActiveSkillIsDefense && winnerCoinsLeft > 0)
            {
                eDefensePower = selectedEnemy.eBasePower;
                if (BattleUIManager.instance != null) BattleUIManager.instance.UpdateClashNumbers(currentPower, eDefensePower);

                for (int d = 0; d < selectedEnemy.eCoinCount; d++)
                {
                    yield return new WaitForSeconds(0.2f);
                    bool head = Random.value <= eProb;
                    if (BattleUIManager.instance != null) BattleUIManager.instance.UpdateEnemyCoin(d, head ? BattleUIManager.CoinState.Head : BattleUIManager.CoinState.Tail);
                    if (head) eDefensePower += selectedEnemy.eCoinPower;
                    if (BattleUIManager.instance != null) BattleUIManager.instance.UpdateClashNumbers(currentPower, eDefensePower);
                }
                selectedEnemy.AddShield(eDefensePower);
            }

            float hitCoinInterval = Mathf.Lerp(0.2f, 0.05f, Mathf.Clamp01((winnerCoinsLeft - 1) / 9f));
            float hitAttackInterval = Mathf.Lerp(0.3f, 0.08f, Mathf.Clamp01((winnerCoinsLeft - 1) / 9f));
            for (int hit = 0; hit < winnerCoinsLeft; hit++)
            {
                if (selectedEnemy == null || selectedEnemy.hp <= 0) break;

                yield return new WaitForSeconds(hitCoinInterval);
                
                StatusEffect freeHitPStatus = attackerObj.GetComponent<StatusEffect>();
                if(freeHitPStatus != null) freeHitPStatus.OnCoinTossed();

                bool isAttackerDead = (meleeAttacker != null && meleeAttacker.isDead) || (rangedAttacker != null && rangedAttacker.isDead);
                if (isAttackerDead) break;
                if (selectedEnemy == null || selectedEnemy.hp <= 0) break;
                
                bool head = Random.value <= pProb;
                
                if (BattleUIManager.instance != null) 
                    BattleUIManager.instance.UpdatePlayerCoin(hit, head ? BattleUIManager.CoinState.Head : BattleUIManager.CoinState.Tail);
                
                if (head) currentPower += activeCoinPower;
                if (BattleUIManager.instance != null) BattleUIManager.instance.UpdateClashNumbers(currentPower, eDefensePower);

                string pTriggerName = "Attack";
                float pAnimSpeed = 1f;
                if (activeCoinEffects != null && hit < activeCoinEffects.Length && activeCoinEffects[hit] != null)
                {
                    if (!string.IsNullOrEmpty(activeCoinEffects[hit].animationTrigger)) pTriggerName = activeCoinEffects[hit].animationTrigger;
                    if (activeCoinEffects[hit].attackSpeed > 0f) pAnimSpeed = activeCoinEffects[hit].attackSpeed;
                }

                if (activeSkillIsDefense)
                {
                    if (pAnim != null) pAnim.Play("Guard", -1, 0f);
                }
                else
                {
                    if (pAnim != null)
                    {
                        pAnim.speed = pAnimSpeed;
                        pAnim.SetTrigger(pTriggerName);
                    }
                }

                float animLength = 0.5f;
                if (pAnim != null && !activeSkillIsDefense)
                {
                    yield return null;
                    AnimatorStateInfo stateInfo = pAnim.GetCurrentAnimatorStateInfo(0);
                    if (pAnim.IsInTransition(0)) stateInfo = pAnim.GetNextAnimatorStateInfo(0);
                    if (stateInfo.length > 0) animLength = stateInfo.length;
                }
                else if (eAnim != null && !activeSkillIsDefense)
                {
                    yield return null;
                    AnimatorStateInfo stateInfo = eAnim.GetCurrentAnimatorStateInfo(0);
                    if (eAnim.IsInTransition(0)) stateInfo = eAnim.GetNextAnimatorStateInfo(0);
                    if (stateInfo.length > 0) animLength = stateInfo.length;
                }

                float halfTime = (animLength * 0.5f) / pAnimSpeed;
                yield return new WaitForSeconds(halfTime);
                
                int finalDamage = Mathf.Max(1, Mathf.RoundToInt(currentPower * atkDefModifier * finalClashModifier));

                List<SkillEffect> effectsToApply = new List<SkillEffect>();
                if (activeSkillEffects != null) effectsToApply.AddRange(activeSkillEffects);
                if (activeCoinEffects != null && hit < activeCoinEffects.Length && activeCoinEffects[hit] != null && activeCoinEffects[hit].effects != null)
                {
                    effectsToApply.AddRange(activeCoinEffects[hit].effects);
                }

                if (!activeSkillIsDefense)
                {
                    if (meleeAttacker != null)
                    {
                        selectedEnemy.TakeDamage(finalDamage);

                        if (eAnim != null)
                        {
                            if (selectedEnemy != null && selectedEnemy.eActiveSkillIsDefense) eAnim.Play("Guard", -1, 0f);
                            else eAnim.Play("Hit", -1, 0f);
                        }
                        
                        if (selectedEnemy == null || selectedEnemy.hp <= 0)
                        {
                            if (meleeAttacker != null) meleeAttacker.AddSP(10);
                        }
                        
                        StatusEffect enemyStatus = selectedEnemy.GetComponent<StatusEffect>();
                        if(enemyStatus != null)
                        {
                            foreach(var eff in effectsToApply)
                            {
                                if (eff.statusType == StatusType.Bleed) enemyStatus.AddBleed(eff.potency, eff.count);
                                else if (eff.statusType == StatusType.Poison) enemyStatus.AddPoison(eff.potency, eff.count);
                                else if (eff.statusType == StatusType.Rupture) enemyStatus.AddRupture(eff.potency, eff.count);
                                else if (eff.statusType == StatusType.Sinking) enemyStatus.AddSinking(eff.potency, eff.count);
                            }
                        }

                        foreach (var eff in effectsToApply)
                        {
                            if (eff.statusType == StatusType.Heal)
                            {
                                if (meleeAttacker != null) meleeAttacker.HealHP(eff.potency);
                            }
                            else if (eff.statusType == StatusType.HealSP)
                            {
                                if (meleeAttacker != null)
                                {
                                    meleeAttacker.AddSP(eff.potency);
                                    if (meleeAttacker.damageTextPrefab != null)
                                    {
                                        GameObject dmg = Instantiate(meleeAttacker.damageTextPrefab, meleeAttacker.transform.position, Quaternion.identity);
                                        DamageText dt = dmg.GetComponent<DamageText>();
                                        if (dt != null) dt.SetSP(eff.potency);
                                    }
                                }
                            }
                            else if (eff.statusType == StatusType.AoE)
                            {
                                Enemy[] allEnemies = FindObjectsOfType<Enemy>();
                                int aoeDmg = Mathf.Max(1, Mathf.RoundToInt(finalDamage * eff.potency / 100f));
                                foreach (Enemy ae in allEnemies)
                                {
                                    if (ae == null || ae.hp <= 0) continue;
                                    if (ae == selectedEnemy) continue;
                                    ae.TakeDamage(aoeDmg);
                                    if (EffectManager.instance != null) EffectManager.instance.SpawnHitEffect(ae.transform.position);
                                }
                            }
                        }
                        
                        if (EffectManager.instance != null)
                        {
                            EffectManager.instance.SpawnHitEffect(selectedEnemy.transform.position);
                            if (activeCoinEffects != null && hit < activeCoinEffects.Length && activeCoinEffects[hit] != null && activeCoinEffects[hit].customEffectPrefab != null)
                            {
                                EffectManager.instance.SpawnCustomEffect(activeCoinEffects[hit].customEffectPrefab, selectedEnemy.transform.position);
                            }
                        }
                        if (CameraShake.instance != null) CameraShake.instance.MediumShake();

                        selectedEnemy.transform.position += Vector3.right * 0.2f;
                    }
                    else if (rangedAttacker != null)
                    {
                        if (rangedAttacker.projectile != null)
                        {
                            GameObject bullet = Instantiate(rangedAttacker.projectile, attackerObj.transform.position, Quaternion.identity);
                            bullet.GetComponent<Projectile>().Init(selectedEnemy.transform, finalDamage, effectsToApply.ToArray(), attackerObj);
                        }
                        else
                        {
                            GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            bullet.transform.position = attackerObj.transform.position;
                            bullet.transform.localScale = Vector3.one * 0.3f;
                            Projectile p = bullet.AddComponent<Projectile>();
                            p.speed = 25f;
                            p.Init(selectedEnemy.transform, finalDamage, effectsToApply.ToArray(), attackerObj);
                        }
                    }
                    
                    yield return new WaitForSeconds(halfTime);
                }
                else
                {
                    yield return new WaitForSeconds(halfTime);
                }
            }

            yield return new WaitForSeconds(0.4f);

            Animator pAnimEnd = attackerObj.GetComponentInChildren<Animator>();
            if (pAnimEnd != null)
            {
                pAnimEnd.speed = 1f;
            }
        }
        else if (eCoinsLeft > 0)
        {
            Vector3 playerPos = attackerObj.transform.position;
            Vector3 monsterAtkPos = playerPos + Vector3.right * 1.2f;
            
            if (selectedEnemy != null)
                yield return StartCoroutine(MoveTwoTransforms(null, selectedEnemy.transform, Vector3.zero, monsterAtkPos, 0.3f));

            int playerDefenseCoins = activeSkillIsDefense ? activeCoinCount : 0;
            if (BattleUIManager.instance != null) BattleUIManager.instance.SetupClashCoins(playerDefenseCoins, winnerCoinsLeft);

            int currentPower = selectedEnemy != null ? selectedEnemy.eBasePower : 0;
            int pDefensePower = 0;
            if (BattleUIManager.instance != null) BattleUIManager.instance.UpdateClashNumbers(pDefensePower, currentPower);
            if (activeSkillIsDefense && winnerCoinsLeft > 0)
            {
                pDefensePower = activeBasePower;
                if (BattleUIManager.instance != null) BattleUIManager.instance.UpdateClashNumbers(pDefensePower, currentPower);

                for (int d = 0; d < activeCoinCount; d++)
                {
                    yield return new WaitForSeconds(0.2f);
                    bool head = Random.value <= pProb;
                    if (BattleUIManager.instance != null) BattleUIManager.instance.UpdatePlayerCoin(d, head ? BattleUIManager.CoinState.Head : BattleUIManager.CoinState.Tail);
                    if (head) pDefensePower += activeCoinPower;
                    if (BattleUIManager.instance != null) BattleUIManager.instance.UpdateClashNumbers(pDefensePower, currentPower);
                }
                if (meleeAttacker != null) meleeAttacker.AddShield(pDefensePower);
                if (rangedAttacker != null) rangedAttacker.AddShield(pDefensePower);
            }

            float hitCoinInterval = Mathf.Lerp(0.2f, 0.05f, Mathf.Clamp01((winnerCoinsLeft - 1) / 9f));
            float hitAttackInterval = Mathf.Lerp(0.3f, 0.08f, Mathf.Clamp01((winnerCoinsLeft - 1) / 9f));

            for (int hit = 0; hit < winnerCoinsLeft; hit++)
            {
                if (selectedEnemy == null || selectedEnemy.hp <= 0) break;
                if (IsPlayerDead(attackerObj)) break;

                yield return new WaitForSeconds(hitCoinInterval);
                
                StatusEffect freeHitEStatus = selectedEnemy.GetComponent<StatusEffect>();
                if(freeHitEStatus != null) freeHitEStatus.OnCoinTossed();
                
                bool head = Random.value <= eProb;
                if (BattleUIManager.instance != null) 
                    BattleUIManager.instance.UpdateEnemyCoin(hit, head ? BattleUIManager.CoinState.Head : BattleUIManager.CoinState.Tail);
                
                if (head && selectedEnemy != null) currentPower += selectedEnemy.eCoinPower;
                if (BattleUIManager.instance != null) BattleUIManager.instance.UpdateClashNumbers(pDefensePower, currentPower);


                string eTriggerName = "Attack";
                if (selectedEnemy != null && selectedEnemy.eActiveCoinEffects != null && hit < selectedEnemy.eActiveCoinEffects.Length)
                {
                    if (!string.IsNullOrEmpty(selectedEnemy.eActiveCoinEffects[hit].animationTrigger))
                        eTriggerName = selectedEnemy.eActiveCoinEffects[hit].animationTrigger;
                }

                if (selectedEnemy != null && selectedEnemy.eActiveSkillIsDefense)
                {
                    if (eAnim != null) eAnim.Play("Guard", -1, 0f);
                }
                else
                {
                    if (eAnim != null) eAnim.SetTrigger(eTriggerName);
                }

                float animLength = 0.5f;
                if (eAnim != null && (selectedEnemy == null || !selectedEnemy.eActiveSkillIsDefense))
                {
                    yield return null;
                    AnimatorStateInfo stateInfo = eAnim.GetCurrentAnimatorStateInfo(0);
                    if (eAnim.IsInTransition(0)) stateInfo = eAnim.GetNextAnimatorStateInfo(0);
                    if (stateInfo.length > 0) animLength = stateInfo.length;
                }

                float halfTime = (animLength * 0.5f);
                yield return new WaitForSeconds(halfTime);

                int finalDamage = Mathf.Max(1, Mathf.RoundToInt(currentPower * enemyAtkDefModifier * finalClashModifier));

                if (selectedEnemy != null && !selectedEnemy.eActiveSkillIsDefense)
                {
                    if (meleeAttacker != null) meleeAttacker.TakeDamage(finalDamage);
                    else if (rangedAttacker != null) rangedAttacker.TakeDamage(finalDamage);

                    if (pAnim != null)
                    {
                        if (activeSkillIsDefense) pAnim.Play("Guard", -1, 0f);
                        else pAnim.Play("Hit", -1, 0f);
                    }

                    if (IsPlayerDead(attackerObj))
                    {
                        if (selectedEnemy != null) selectedEnemy.AddSP(10);
                    }

                    if (EffectManager.instance != null)
                    {
                        EffectManager.instance.SpawnHitEffect(attackerObj.transform.position);
                        if (selectedEnemy != null && selectedEnemy.eActiveCoinEffects != null && hit < selectedEnemy.eActiveCoinEffects.Length && selectedEnemy.eActiveCoinEffects[hit] != null && selectedEnemy.eActiveCoinEffects[hit].customEffectPrefab != null)
                        {
                            EffectManager.instance.SpawnCustomEffect(selectedEnemy.eActiveCoinEffects[hit].customEffectPrefab, attackerObj.transform.position);
                        }
                    }
                    if (CameraShake.instance != null) CameraShake.instance.MediumShake();

                    attackerObj.transform.position += Vector3.left * 0.2f;
                }
                
                float delay = (hit == winnerCoinsLeft - 1) ? 0.6f : hitAttackInterval;
                yield return new WaitForSeconds(delay);
            }

            Animator eAnimEnd = selectedEnemy != null ? selectedEnemy.GetComponentInChildren<Animator>() : null;
            if (eAnimEnd != null) eAnimEnd.speed = 1f;
        }

        if (attackerObj != null && !IsPlayerDead(attackerObj))
        {
            Animator finalPAnim = attackerObj.GetComponentInChildren<Animator>();
            if (finalPAnim != null) finalPAnim.Play("Idle", -1, 0f);
        }

        if (selectedEnemy != null && selectedEnemy.hp > 0)
        {
            Animator finalEAnim = selectedEnemy.GetComponentInChildren<Animator>();
            if (finalEAnim != null) finalEAnim.Play("Idle", -1, 0f);
        }

        if (CameraShake.instance != null) CameraShake.instance.ResetFocus();

        if (BattleUIManager.instance != null) BattleUIManager.instance.ShowClashUI(false);

        if (pStatus != null) pStatus.BringToFront(false);
        if (eStatus != null) eStatus.BringToFront(false);
    }

    Vector3 GetInitialPosition(GameObject obj)
    {
        PlayerMove pm = obj.GetComponent<PlayerMove>();
        if (pm != null) return pm.startPosition;
        RangedPlayerMove rpm = obj.GetComponent<RangedPlayerMove>();
        if (rpm != null) return rpm.startPosition;
        Enemy e = obj.GetComponent<Enemy>();
        if (e != null) return e.startPosition;
        return obj.transform.position;
    }

    IEnumerator MoveTwoTransforms(Transform t1, Transform t2, Vector3 pos1, float duration)
    {
        yield return MoveTwoTransforms(t1, t2, pos1, pos1, duration);
    }

    IEnumerator MoveTwoTransforms(Transform t1, Transform t2, Vector3 pos1, Vector3 pos2, float duration)
    {
        Vector3 start1 = t1 != null ? t1.position : Vector3.zero;
        Vector3 start2 = t2 != null ? t2.position : Vector3.zero;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);
            if (t1 != null) t1.position = Vector3.Lerp(start1, pos1, t);
            if (t2 != null) t2.position = Vector3.Lerp(start2, pos2, t);
            yield return null;
        }
        if (t1 != null) t1.position = pos1;
        if (t2 != null) t2.position = pos2;
    }

    IEnumerator MoveSingleTransform(Transform t, Vector3 dest, float duration)
    {
        if (t == null) yield break;
        Vector3 start = t.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float timeRatio = elapsed / duration;
            t.position = Vector3.Lerp(start, dest, Mathf.SmoothStep(0f, 1f, timeRatio));
            yield return null;
        }
        t.position = dest;
    }

    bool CheckBattleEnd()
    {
        bool pAlive = false;
        PlayerMove[] pms = FindObjectsOfType<PlayerMove>();
        foreach (var p in pms) { if (!p.isDead) pAlive = true; }
        
        RangedPlayerMove[] rpms = FindObjectsOfType<RangedPlayerMove>();
        foreach (var rp in rpms) { if (!rp.isDead) pAlive = true; }

        bool eAlive = false;
        Enemy[] es = FindObjectsOfType<Enemy>();
        foreach (var e in es) { if (e.hp > 0) eAlive = true; }

        return !pAlive || !eAlive;
    }

    IEnumerator ReturnToExploreScene(bool win)
    {
        yield return new WaitForSeconds(2f);
        if (ExploreManager.instance != null)
        {
            ExploreManager.instance.OnBattleFinished(win);
            SceneManager.UnloadSceneAsync("BattleScene");
        }
        else
        {
            SceneManager.LoadScene("ExploreScene");
        }
    }

    void UpdatePlayerHpToGameManager()
    {
        if (GameManager.instance == null) return;

        PlayerMove meleePlayer = Object.FindAnyObjectByType<PlayerMove>();
        RangedPlayerMove rangedPlayerComp = Object.FindAnyObjectByType<RangedPlayerMove>();

        if (meleePlayer != null)
        {
            GameManager.instance.hp = Mathf.Max(0, meleePlayer.hp);
        }
        else if (rangedPlayerComp != null)
        {
            GameManager.instance.hp = Mathf.Max(0, rangedPlayerComp.hp);
        }
        else
        {
            GameManager.instance.hp = 0;
        }
        GameManager.instance.SaveProgress();
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}




























