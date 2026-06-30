using UnityEngine;
using System.Collections;

public class BattleManager : MonoBehaviour
{
    public static BattleManager instance;

    [Header("FX & Audio (특수효과)")]
    public GameObject clashParticlePrefab; // 합의 불꽃
    public GameObject hitParticlePrefab;   // 베임/타격 출혈
    public AudioSource audioSource;
    public AudioClip clashSound;           // 챙그랑
    public AudioClip drawSound;            // 무승부 튕김
    public AudioClip hitSound;             // 퍽/서걱 타격음

    [Header("Optional")]
    public TurnManager turnManager;

    public GameObject activePlayer;

    public PlayerMove player;

    public RangedPlayerMove rangedPlayer;

    Enemy selectedEnemy;

    int playerRoll;

    [Header("Skill")]
    public string selectedSkillName;
    int activeBasePower;
    int activeCoinPower;
    int activeCoinCount;

    public enum SkillId
    {
        None,
        Skill1,
        Skill2,
        Skill3, // 테스터용 자폭
        Skill4  // 테스터용 즉사기(필살기)
    }

    [System.Serializable]
    public class UnitStats
    {
        public string name = "Unit";
        public int maxHp = 30;
        public int hp = 30;
        public int attack = 10;
        public int defense = 3;
        public bool isDead;

        public void ResetHp()
        {
            hp = maxHp;
            isDead = false;
        }
    }

    [Header("Turn Battle Stats (Inspector)")]
    public UnitStats playerStats = new UnitStats { name = "Player" };
    public UnitStats enemyStats = new UnitStats { name = "Enemy" };

    void Awake()
    {
        instance = this;
        if (turnManager == null) turnManager = TurnManager.instance;

        // Inspector 연결이 안 되어도 동작하게 자동 연결
        if (player == null) player = FindFirstObjectByType<PlayerMove>();
        if (rangedPlayer == null) rangedPlayer = FindFirstObjectByType<RangedPlayerMove>();
    }

    void Start()
    {
        Debug.Log("[BattleManager] Start");

        // 안전장치: 시작 시 HP 범위 정리
        playerStats.maxHp = Mathf.Max(1, playerStats.maxHp);
        enemyStats.maxHp = Mathf.Max(1, enemyStats.maxHp);
        playerStats.hp = Mathf.Clamp(playerStats.hp, 0, playerStats.maxHp);
        enemyStats.hp = Mathf.Clamp(enemyStats.hp, 0, enemyStats.maxHp);
        playerStats.isDead = playerStats.hp <= 0;
        enemyStats.isDead = enemyStats.hp <= 0;
    }

    // UI에서 타겟을 지정할 때만 호출
    public void SetTarget(Enemy enemy)
    {
        selectedEnemy = enemy;
        Debug.Log($"[BattleManager] Target -> {(selectedEnemy != null ? selectedEnemy.name : "null")}");
    }

    // UI에서 스킬을 고르면 호출
    public void ConfigureSkill(SkillId skill)
    {
        switch (skill)
        {
            case SkillId.Skill1:
                activeBasePower = 4;
                activeCoinPower = 2;
                activeCoinCount = 3; // 3연타
                selectedSkillName = "3연속 잽 (4 위력, +2 코인)";
                break;
            case SkillId.Skill2:
                activeBasePower = 5;
                activeCoinPower = 4;
                activeCoinCount = 2; // 2연타
                selectedSkillName = "강타 (5 위력, +4 코인)";
                break;
            case SkillId.Skill3: // 적에게 일부러 맞아죽기 위한 스킬
                activeBasePower = 1;
                activeCoinPower = 0;
                activeCoinCount = 1;
                selectedSkillName = "자폭 스킬 (무조건 1)";
                break;
            case SkillId.Skill4: // 적을 한방에 가루로 만드는 스킬
                activeBasePower = 50;
                activeCoinPower = 50;
                activeCoinCount = 3; // 3연속 난도질
                selectedSkillName = "차원을 베는 참격 (50 위력, +50 코인)";
                break;
            default:
                activeBasePower = 0;
                activeCoinPower = 0;
                activeCoinCount = 0;
                selectedSkillName = string.Empty;
                break;
        }

        Debug.Log($"[BattleManager] Skill -> {selectedSkillName} (B:{activeBasePower}, P:{activeCoinPower}, C:{activeCoinCount})");
    }

    public void GetSkillInfo(SkillId skill, out int bPower, out int cPower, out int cCount, out string sName)
    {
        bPower = 0; cPower = 0; cCount = 0; sName = "";
        switch (skill)
        {
            case SkillId.Skill1: bPower = 4; cPower = 2; cCount = 3; sName = "3연속 잽 (4 위력, +2 코인)"; break;
            case SkillId.Skill2: bPower = 5; cPower = 4; cCount = 2; sName = "강타 (5 위력, +4 코인)"; break;
            case SkillId.Skill3: bPower = 1; cPower = 0; cCount = 1; sName = "자폭 스킬 (무조건 1)"; break;
            case SkillId.Skill4: bPower = 50; cPower = 50; cCount = 3; sName = "차원을 베는 참격"; break;
        }
    }

    // BattleUIManager에서 버튼 2번째 클릭 시 호출
    public void ExecuteAttack(Enemy target, SkillId skill)
    {
        SetTarget(target);
        ConfigureSkill(skill);
        StartCoroutine(ClashRoutine());
    }

    public IEnumerator ExecuteActionQueue(System.Collections.Generic.List<FloatingSkillSlot> queuedSlots)
    {
        BattleUIManager.instance.SetState(BattleUIManager.BattleState.Attacking);

        // 정렬 순서 보장 (슬롯 인덱스 기준)
        queuedSlots.Sort((a, b) => a.slotIndex.CompareTo(b.slotIndex));

        foreach (var slot in queuedSlots)
        {
            if (slot.assignedSkill != SkillId.None && slot.assignedTarget != null)
            {
                // 충돌/죽음 방지: 앞선 클레시에서 캐릭터나 적이 죽었으면 스킵
                if (slot.ownerPlayer == null || slot.assignedTarget == null) continue;

                bool isPlayerDead = false;
                PlayerMove pm = slot.ownerPlayer.GetComponent<PlayerMove>();
                if (pm != null && pm.isDead) isPlayerDead = true;
                
                RangedPlayerMove rpm = slot.ownerPlayer.GetComponent<RangedPlayerMove>();
                if (rpm != null && rpm.isDead) isPlayerDead = true;

                if (isPlayerDead) continue; // 플레이어가 사망하여 퇴장한 상태면 스킵

                activePlayer = slot.ownerPlayer;
                ConfigureSkill(slot.assignedSkill);
                SetTarget(slot.assignedTarget);

                yield return StartCoroutine(ClashRoutine());
            }
        }

        // 전체 행동 종료 시 슬롯 비우기 및 턴 넘기기
        foreach (var s in queuedSlots) s.ClearSlot();

        if (turnManager != null) turnManager.NextTurn();
        EndAttack();
    }

    IEnumerator ClashRoutine()
    {
        Debug.Log("[BattleManager] Clash Started");

        if (turnManager != null && !turnManager.IsPlayerTurn())
        {
            Debug.LogWarning($"[BattleManager] Blocked by TurnManager");
            yield break;
        }

        if (selectedEnemy == null || (activeCoinCount == 0 && activeBasePower == 0) || activePlayer == null)
        {
            Debug.LogError("[BattleManager] 타겟, 스킬, 혹은 참전 캐릭터가 NULL입니다.");
            yield break;
        }

        selectedEnemy.SelectSkill(); // 전투 진입 전 적 스킬 고정

        Transform pT = activePlayer.transform;
        Transform eT = selectedEnemy.transform;
        
        Vector3 playerStartPos = pT.position;
        Vector3 enemyStartPos = eT.position;
        Vector3 clashPoint = (playerStartPos + enemyStartPos) / 2f;
        
        float offset = 0.8f;
        bool isPlayerLeft = playerStartPos.x < enemyStartPos.x;
        Vector3 pOffset = isPlayerLeft ? new Vector3(-offset, 0, 0) : new Vector3(offset, 0, 0);
        Vector3 eOffset = isPlayerLeft ? new Vector3(offset, 0, 0) : new Vector3(-offset, 0, 0);
        Vector3 pClashPos = clashPoint + pOffset;
        Vector3 eClashPos = clashPoint + eOffset;

        BattleUIManager.instance.ShowClashUI(true);
        yield return StartCoroutine(MoveTwoTransforms(pT, pClashPos, eT, eClashPos, 15f));
        
        PlaySound(clashSound);
        SpawnParticle(clashParticlePrefab, clashPoint);
        StartCoroutine(CameraShakeRoutine(0.1f, 0.15f)); 
        yield return new WaitForSeconds(0.2f); 

        int pCoinsLeft = activeCoinCount;
        int eCoinsLeft = selectedEnemy.eCoinCount;

        int pRoll = 0;
        int eRoll = 0;
        int clashCount = 0; 
        string winner = ""; 

        // [림버스 코인 파괴 규칙] 한 쪽의 코인이 0개가 될 때까지 계속 합을 겨룹니다.
        while (pCoinsLeft > 0 && eCoinsLeft > 0)
        {
            // 루프 진입 시 코인 UI 대기 상태 동적 렌더링
            BattleUIManager.instance.SetupClashCoins(pCoinsLeft, eCoinsLeft);
            pRoll = activeBasePower;
            eRoll = selectedEnemy.eBasePower;
            BattleUIManager.instance.UpdateClashNumbers(pRoll, eRoll);

            // 실시간 코인 연출 (최대 갯수만큼 턴을 돌며 단위로 뒤집음)
            int maxI = Mathf.Max(pCoinsLeft, eCoinsLeft);
            for(int i = 0; i < maxI; i++)
            {
                yield return new WaitForSeconds(0.4f); // 긴장감 대기 시간 (틱당)

                if (i < pCoinsLeft)
                {
                    bool head = Random.value >= 0.5f;
                    BattleUIManager.instance.UpdatePlayerCoin(i, head ? BattleUIManager.CoinState.Head : BattleUIManager.CoinState.Tail);
                    if (head) pRoll += activeCoinPower;
                }

                if (i < eCoinsLeft)
                {
                    bool head = Random.value >= 0.5f;
                    BattleUIManager.instance.UpdateEnemyCoin(i, head ? BattleUIManager.CoinState.Head : BattleUIManager.CoinState.Tail);
                    if (head) eRoll += selectedEnemy.eCoinPower;
                }

                BattleUIManager.instance.UpdateClashNumbers(pRoll, eRoll); // 숫자가 틱틱틱 올라감!
            }

            yield return new WaitForSeconds(0.6f); // 모두 굴려진 후 숫자를 확인하는 텐션 시간

            if (pRoll == eRoll)
            {
                clashCount++; 
                Debug.Log($"[Clash] 무승부 ({pRoll} == {eRoll}) 남은코인 P:{pCoinsLeft} E:{eCoinsLeft}");
                BattleUIManager.instance.SetClashResultText("합 무승부! 다시 겨루기!");
            }
            else if (pRoll > eRoll)
            {
                eCoinsLeft--; // 패자 코인 파괴!
                Debug.Log($"[Clash] 승리 ({pRoll} > {eRoll}) 적 코인파괴 -> E 남은코인:{eCoinsLeft}");
                BattleUIManager.instance.SetClashResultText($"합 우세! 적 코인 파괴!");
            }
            else
            {
                pCoinsLeft--; // 패자 코인 파괴!
                Debug.Log($"[Clash] 패배 ({pRoll} < {eRoll}) 내 코인파괴 -> P 남은코인:{pCoinsLeft}");
                BattleUIManager.instance.SetClashResultText($"합 열세.. 아군 코인 파괴!");
            }

            // 시각 연출 (튕겨나감)
            Vector3 pBumpPos = pClashPos + pOffset * 0.5f;
            Vector3 eBumpPos = eClashPos + eOffset * 0.5f;
            yield return StartCoroutine(MoveTwoTransforms(pT, pBumpPos, eT, eBumpPos, 30f));
            yield return StartCoroutine(MoveTwoTransforms(pT, pClashPos, eT, eClashPos, 40f));
            
            PlaySound(drawSound); 
            SpawnParticle(clashParticlePrefab, clashPoint);
            StartCoroutine(CameraShakeRoutine(0.15f, 0.25f));

            yield return new WaitForSeconds(0.2f);
        }

        winner = pCoinsLeft > 0 ? "Player" : "Enemy";
        int winnerCoinsLeft = pCoinsLeft > 0 ? pCoinsLeft : eCoinsLeft;

        // 3. 다단 히트 (코인 갯수만큼 연타)
        if (winner == "Player")
        {
            Debug.Log($"[Clash] 합 최종 승리! (남은 코인: {winnerCoinsLeft}회 다단히트)");
            BattleUIManager.instance.SetClashResultText($"승리! {winnerCoinsLeft}연타 공격!");

            Vector3 eLoserPos = eClashPos + eOffset * 1.5f; 
            yield return StartCoroutine(MoveTransform(eT, eLoserPos, 20f));
            yield return new WaitForSeconds(0.6f);
            BattleUIManager.instance.ShowClashUI(false);

            PlayerMove p = pT.GetComponent<PlayerMove>();
            RangedPlayerMove rp = pT.GetComponent<RangedPlayerMove>();

            int pAtkLvl = 10;
            if (p != null) pAtkLvl = p.attackLevel;
            else if (rp != null) pAtkLvl = rp.attackLevel;
            int eDefLvl = selectedEnemy != null ? selectedEnemy.defenseLevel : 10;
            float modifier = Mathf.Max(0.1f, 1f + ((pAtkLvl - eDefLvl) * 0.03f) + (clashCount * 0.03f));

            if (rp != null) yield return StartCoroutine(MoveTransform(pT, playerStartPos, 40f)); // 원거리 사격 대기
            else yield return StartCoroutine(MoveTransform(pT, eT.position, 40f)); // 근접 돌진

            // ★ 다단 히트 반복문
            for (int hit = 0; hit < winnerCoinsLeft; hit++)
            {
                if (selectedEnemy == null || selectedEnemy.hp <= 0) break; // 적 시체 공격 방지

                // 현재 타격을 위한 코인 UI 세팅
                BattleUIManager.instance.SetupClashCoins(winnerCoinsLeft, 0);
                int hitRoll = activeBasePower;
                BattleUIManager.instance.UpdateClashNumbers(hitRoll, 0);

                // 코인 순차 굴림 시각화 (타격 파워 집계)
                for (int i = 0; i < winnerCoinsLeft; i++)
                {
                    yield return new WaitForSeconds(0.15f); // 타격 중엔 좀 더 빠르게 팅팅팅!
                    bool head = Random.value >= 0.5f;
                    BattleUIManager.instance.UpdatePlayerCoin(i, head ? BattleUIManager.CoinState.Head : BattleUIManager.CoinState.Tail);
                    if (head) hitRoll += activeCoinPower;
                    BattleUIManager.instance.UpdateClashNumbers(hitRoll, 0);
                }

                yield return new WaitForSeconds(0.2f);
                
                int finalDamage = Mathf.Max(1, Mathf.RoundToInt(hitRoll * modifier));
                Debug.Log($"[Clash Hit {hit+1}/{winnerCoinsLeft}] 연산 (주사위: {hitRoll}) -> 데미지: {finalDamage}");

                if (rp != null)
                {
                    rp.ShootTarget(eT, finalDamage);
                    yield return new WaitForSeconds(0.2f);
                    if (selectedEnemy != null) selectedEnemy.TakeDamage(finalDamage);
                }
                else
                {
                    if (selectedEnemy != null) selectedEnemy.TakeDamage(finalDamage);
                }

                PlaySound(hitSound);
                SpawnParticle(hitParticlePrefab, eT.position);
                yield return new WaitForSeconds(0.3f); 
            }
            
            yield return StartCoroutine(MoveTwoTransforms(pT, playerStartPos, eT, enemyStartPos, 12f));
        }
        else if (winner == "Enemy")
        {
            Debug.Log($"[Clash] 합 최종 패배.. (남은 코인: {winnerCoinsLeft}회 다단피격)");
            BattleUIManager.instance.SetClashResultText($"패배.. {winnerCoinsLeft}연속 피격!");

            Vector3 pLoserPos = pClashPos + pOffset * 1.5f; 
            yield return StartCoroutine(MoveTransform(pT, pLoserPos, 20f));
            yield return new WaitForSeconds(0.6f);
            BattleUIManager.instance.ShowClashUI(false);

            yield return StartCoroutine(MoveTransform(eT, pT.position, 40f)); // 적 돌진

            PlayerMove p = pT.GetComponent<PlayerMove>();
            RangedPlayerMove rp = pT.GetComponent<RangedPlayerMove>();

            int eAtkLvl = selectedEnemy != null ? selectedEnemy.attackLevel : 10;
            int pDefLvl = 10;
            if (p != null) pDefLvl = p.defenseLevel;
            else if (rp != null) pDefLvl = rp.defenseLevel;
            float modifier = Mathf.Max(0.1f, 1f + ((eAtkLvl - pDefLvl) * 0.03f) + (clashCount * 0.03f));

            bool playerDied = false;

            // ★ 다단 피격 시작
            for (int hit = 0; hit < winnerCoinsLeft; hit++)
            {
                if (p != null) playerDied = p.isDead;
                else if (rp != null) playerDied = rp.isDead;
                if (playerDied) break; // 시체 공격 그만

                BattleUIManager.instance.SetupClashCoins(0, winnerCoinsLeft);
                int hitRoll = selectedEnemy.eBasePower;
                BattleUIManager.instance.UpdateClashNumbers(0, hitRoll);

                // 코인 순차 굴림 시각화
                for (int i = 0; i < winnerCoinsLeft; i++)
                {
                    yield return new WaitForSeconds(0.15f);
                    bool head = Random.value >= 0.5f;
                    BattleUIManager.instance.UpdateEnemyCoin(i, head ? BattleUIManager.CoinState.Head : BattleUIManager.CoinState.Tail);
                    if (head) hitRoll += selectedEnemy.eCoinPower;
                    BattleUIManager.instance.UpdateClashNumbers(0, hitRoll);
                }

                yield return new WaitForSeconds(0.2f);
                
                int finalDamage = Mathf.Max(1, Mathf.RoundToInt(hitRoll * modifier));
                Debug.Log($"[Clash Hit {hit+1}/{winnerCoinsLeft}] 에너미 연산 (주사위: {hitRoll}) -> 피격: {finalDamage}");

                if (p != null) p.TakeDamage(finalDamage);
                else if (rp != null) rp.TakeDamage(finalDamage);

                PlaySound(hitSound);
                SpawnParticle(hitParticlePrefab, pT.position);
                yield return new WaitForSeconds(0.3f); 
            }

            if (p != null) playerDied = p.isDead;
            else if (rp != null) playerDied = rp.isDead;

            if (playerDied) yield return StartCoroutine(MoveTransform(eT, enemyStartPos, 12f));
            else yield return StartCoroutine(MoveTwoTransforms(pT, playerStartPos, eT, enemyStartPos, 12f));
        }

        BattleUIManager.instance.ShowClashUI(false);
        activeBasePower = 0;
    }

    // 오브젝트 한 개를 부드럽게 물리적으로 이동시키는 코루틴 (죽어서 파괴되었을 때의 Null 방지 포함 안전설계)
    IEnumerator MoveTransform(Transform t, Vector3 targetPos, float speed)
    {
        if (t == null) yield break;
        while (t != null && Vector3.Distance(t.position, targetPos) > 0.05f)
        {
            t.position = Vector3.MoveTowards(t.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }
        if (t != null) t.position = targetPos;
    }

    // 승자와 패자 두 오브젝트를 동시에 물리적으로 이동시키는 코루틴 
    IEnumerator MoveTwoTransforms(Transform t1, Vector3 target1, Transform t2, Vector3 target2, float speed)
    {
        while (true)
        {
            bool t1Moving = false;
            bool t2Moving = false;

            if (t1 != null && Vector3.Distance(t1.position, target1) > 0.05f)
            {
                t1.position = Vector3.MoveTowards(t1.position, target1, speed * Time.deltaTime);
                t1Moving = true;
            }
            if (t2 != null && Vector3.Distance(t2.position, target2) > 0.05f)
            {
                t2.position = Vector3.MoveTowards(t2.position, target2, speed * Time.deltaTime);
                t2Moving = true;
            }

            if (!t1Moving && !t2Moving) break;
            yield return null;
        }

        if (t1 != null) t1.position = target1;
        if (t2 != null) t2.position = target2;
    }

    void EndAttack()
    {
        Debug.Log("[BattleManager] Attack ended");
        selectedSkillName = string.Empty;
        BattleUIManager.instance.ReturnToIdle();
    }

    // ==========================================
    // FX & Audio System
    // ==========================================

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void SpawnParticle(GameObject prefab, Vector3 pos)
    {
        if (prefab != null)
        {
            GameObject fx = Instantiate(prefab, pos, Quaternion.identity);
            Destroy(fx, 2.0f); // 2초 뒤 쓰레기 청소
        }
    }

    IEnumerator CameraShakeRoutine(float duration, float magnitude)
    {
        if (Camera.main == null) yield break;

        Vector3 originalPos = Camera.main.transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            Camera.main.transform.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Camera.main.transform.localPosition = originalPos;
    }

}
