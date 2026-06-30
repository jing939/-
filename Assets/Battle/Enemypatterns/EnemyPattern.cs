using UnityEngine;

/// <summary>
/// 적 패턴의 기본 추상 클래스.
/// 모든 적 패턴은 이 클래스를 상속받아 구현합니다.
/// </summary>
public abstract class EnemyPattern : ScriptableObject
{
    [Header("패턴 설정")]
    public string patternName;
    
    [Tooltip("전투 중 표시될 기술 이름")]
    public string skillName = "공격";

    /// <summary>
    /// 현재 턴에서의 스킬 위력을 계산합니다.
    /// </summary>
    /// <param name="enemy">이 패턴을 사용하는 적</param>
    /// <param name="battle">현재 전투 매니저 (플레이어 정보 포함)</param>
    /// <param name="turnCount">현재까지의 턴 수</param>
    /// <returns>계산된 스킬 위력</returns>
    public abstract int GetSkillPower(Enemy enemy, BattleManager battle, int turnCount);

    /// <summary>
    /// (선택) 패턴이 적용될 때 초기화 처리
    /// </summary>
    public virtual void OnPatternStart(Enemy enemy) { }
}

// ─────────────────────────────────────────────
// 1. 공격적 패턴 (AggressivePattern)
//    - 높은 위력 범위, 체력이 낮을수록 더 강해집니다.
// ─────────────────────────────────────────────
[CreateAssetMenu(menuName = "적 패턴/공격적 패턴")]
public class AggressivePattern : EnemyPattern
{
    [Header("기본 위력 범위")]
    public int minPower = 10;
    public int maxPower = 20;

    [Header("체력 낮을 때 보너스")]
    [Tooltip("적 체력이 이 비율 이하일 때 보너스 적용")]
    public float hpThreshold = 0.3f;
    public int bonusPower = 5;

    public override int GetSkillPower(Enemy enemy, BattleManager battle, int turnCount)
    {
        int power = Random.Range(minPower, maxPower + 1);

        // 체력이 낮으면 더 강해짐 (광폭화)
        float hpRatio = (float)enemy.hp / enemy.maxHp;
        if (hpRatio <= hpThreshold)
        {
            power += bonusPower;
        }

        return power;
    }
}

// ─────────────────────────────────────────────
// 2. 방어적 패턴 (DefensivePattern)
//    - 기본 위력은 낮지만, 체력이 낮으면 회복합니다.
// ─────────────────────────────────────────────
[CreateAssetMenu(menuName = "적 패턴/방어적 패턴")]
public class DefensivePattern : EnemyPattern
{
    [Header("기본 위력 범위")]
    public int minPower = 3;
    public int maxPower = 8;

    [Header("회복 설정")]
    [Tooltip("적 체력이 이 비율 이하일 때 회복")]
    public float healThreshold = 0.4f;
    public int healAmount = 10;

    public override int GetSkillPower(Enemy enemy, BattleManager battle, int turnCount)
    {
        float hpRatio = (float)enemy.hp / enemy.maxHp;

        // 체력이 낮으면 회복 후 낮은 위력 사용
        if (hpRatio <= healThreshold)
        {
            enemy.Heal(healAmount);
        }

        return Random.Range(minPower, maxPower + 1);
    }
}

// ─────────────────────────────────────────────
// 3. 순서 패턴 (SequencePattern)
//    - 미리 정해진 순서대로 위력이 변합니다.
//    - 보스 몬스터 등에 적합합니다.
// ─────────────────────────────────────────────
[CreateAssetMenu(menuName = "적 패턴/순서 패턴")]
public class SequencePattern : EnemyPattern
{
    [Header("턴별 위력 (순서대로 반복)")]
    public int[] powerSequence = { 5, 8, 15, 20, 5 };

    public override int GetSkillPower(Enemy enemy, BattleManager battle, int turnCount)
    {
        // 시퀀스 길이만큼 반복
        int index = turnCount % powerSequence.Length;
        return powerSequence[index];
    }
}

// ─────────────────────────────────────────────
// 4. 반응형 패턴 (ReactivePattern)
//    - 플레이어가 선택한 스킬의 위력(min/maxPower)을 분석하여 대응합니다.
// ─────────────────────────────────────────────
[CreateAssetMenu(menuName = "적 패턴/반응형 패턴")]
public class ReactivePattern : EnemyPattern
{
    [Header("대응 설정")]
    [Range(0, 1)]
    [Tooltip("플레이어의 강한 공격에 반응할 확률 (0~1)")]
    public float reactiveChance = 0.5f;

    [Header("수치 설정")]
    public int baseMinPower = 10;
    public int baseMaxPower = 20;

    public override int GetSkillPower(Enemy enemy, BattleManager battle, int turnCount)
    {
        // 랜덤 확률 체크 (피로도 방지)
        bool triggerCounter = Random.value <= reactiveChance;

        // 플레이어의 최대 위력이 높을 때 (강한 스킬) 반응 확률 체크
        if (battle.maxPower >= 15 && triggerCounter)
        {
            // 플레이어 수치에 맞춰 대응
            return Random.Range(battle.minPower, battle.maxPower + 5);
        }
        else
        {
            // 평상시 또는 확률 실패 시 기본 위력
            return Random.Range(baseMinPower, baseMaxPower + 1);
        }
    }
}
