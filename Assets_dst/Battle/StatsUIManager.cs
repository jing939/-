using UnityEngine;
using TMPro;

public class StatsUIManager : MonoBehaviour
{
    public enum GameState
    {
        Idle,
        Battle
    }

    [Header("State")]
    public GameState state = GameState.Idle;

    [Header("UI Panels")]
    public GameObject characterPanel;   // 오른쪽 캐릭터 스탯 패널
    public GameObject monsterPanel;     // 왼쪽 몬스터 스탯 패널

    [Header("Character UI")]
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI characterHpText;
    public TextMeshProUGUI characterAtkText;
    public TextMeshProUGUI characterDefText;
    public TextMeshProUGUI characterSpdText; // 캐릭터용 속력 주사위 출력

    [Header("Monster UI")]
    public TextMeshProUGUI monsterNameText;
    public TextMeshProUGUI monsterHpText;
    public TextMeshProUGUI monsterAtkText;
    public TextMeshProUGUI monsterDefText;
    public TextMeshProUGUI monsterSpdText; // 몬스터용 속력 주사위 출력

    [Header("Tags / Layers")]
    public string characterTag = "Player";
    public string monsterTag = "Enemy";

    Camera cam;

    void Awake()
    {
        cam = Camera.main;

        // 자동 연결 로직: Inspector에서 할당하지 않았을 경우 (비활성화 상태여도 찾을 수 있도록 개선)
        if (characterPanel == null || monsterPanel == null)
        {
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform t in allChildren)
                {
                    if (characterPanel == null && t.name == "CharacterPanel")
                    {
                        characterPanel = t.gameObject;
                        Debug.Log("[StatsUIManager] CharacterPanel 자동 할당 완료 (비활성화 상태 포함 검색 성공).");
                    }

                    if (monsterPanel == null && t.name == "MonsterPanel")
                    {
                        monsterPanel = t.gameObject;
                        Debug.Log("[StatsUIManager] MonsterPanel 자동 할당 완료 (비활성화 상태 포함 검색 성공).");
                    }
                }
            }
        }

        if (characterPanel == null)
        {
            Debug.LogError("[StatsUIManager] characterPanel(우측 스탯 UI)을 찾을 수 없습니다! 씬 구조 내에 'CharacterPanel' 이라는 오브젝트가 존재하지 않거나 오타가 있습니다.");
        }

        if (monsterPanel == null)
        {
            Debug.LogError("[StatsUIManager] monsterPanel(좌측 스탯 UI)을 찾을 수 없습니다! 씬 구조 내에 'MonsterPanel' 이라는 오브젝트가 존재하지 않거나 오타가 있습니다.");
        }

        // 스탯창이 화면 절반을 정확히 채우도록 크기 자동 조절
        ApplyHalfScreenLayout(characterPanel, true);
        ApplyHalfScreenLayout(monsterPanel, false);

        SetPanels(false, false);
        Debug.Log("[StatsUIManager] Init -> state=Idle");
    }

    void ApplyHalfScreenLayout(GameObject panel, bool isRightHalf)
    {
        if (panel == null) return;
        RectTransform rt = panel.GetComponent<RectTransform>();
        if (rt == null) return;

        // 우측 패널 (x min: 0.5 ~ x max: 1.0) / 좌측 패널 (x min: 0.0 ~ x max: 0.5)
        rt.anchorMin = isRightHalf ? new Vector2(0.5f, 0f) : new Vector2(0f, 0f);
        rt.anchorMax = isRightHalf ? new Vector2(1f, 1f) : new Vector2(0.5f, 1f);
        
        // 피벗 기준점을 가운데로
        rt.pivot = new Vector2(0.5f, 0.5f);
        // 여백 지우기 (위,아래,좌,우 꽉 채움)
        rt.offsetMin = Vector2.zero; // Left, Bottom
        rt.offsetMax = Vector2.zero; // Right, Top
    }

    public void OnBattleStartButtonClicked()
    {
        SetState(GameState.Battle);
        SetPanels(false, false);                  // 전투 시작 시 스탯 창 비활성화
    }

    public void SetState(GameState next)
    {
        state = next;
        Debug.Log($"[StatsUIManager] State -> {state}");

        if (state != GameState.Idle)
        {
            SetPanels(false, false);
        }
    }

    public void ShowCharacterStats(GameObject character)
    {
        if (state != GameState.Idle)
        {
            Debug.Log("[StatsUIManager] Not Idle state. Cannot show character stats.");
            return;
        }

        if (characterPanel == null)
        {
            Debug.LogError("[StatsUIManager] characterPanel이 null입니다! characterPanel이 연결되지 않아 UI를 띄울 수 없습니다.");
            return;
        }

        PlayerMove playerMove = character.GetComponent<PlayerMove>();
        RangedPlayerMove rangedPlayer = character.GetComponent<RangedPlayerMove>();

        if (playerMove == null && rangedPlayer == null)
        {
            Debug.LogWarning("[StatsUIManager] PlayerMove/RangedPlayerMove component not found on character");
        }

        if (characterNameText != null)
        {
            if (playerMove != null && !string.IsNullOrEmpty(playerMove.characterName))
                characterNameText.text = playerMove.characterName;
            else if (rangedPlayer != null && !string.IsNullOrEmpty(rangedPlayer.characterName))
                characterNameText.text = rangedPlayer.characterName;
            else
                characterNameText.text = character.name;
        }

        if (playerMove != null)
        {
            if (characterHpText != null)  characterHpText.text  = "HP : "   + playerMove.hp;
            if (characterAtkText != null) characterAtkText.text = "ATK : "  + playerMove.attackLevel;
            if (characterDefText != null) characterDefText.text = "DEF : "  + playerMove.defenseLevel;
            if (characterSpdText != null) characterSpdText.text = "SPD : "  + playerMove.currentSpeed + $" ({playerMove.minSpeed}~{playerMove.maxSpeed})";
        }
        else if (rangedPlayer != null)
        {
            if (characterHpText != null)  characterHpText.text  = "HP : "   + rangedPlayer.hp;
            if (characterAtkText != null) characterAtkText.text = "ATK : "  + rangedPlayer.attackLevel;
            if (characterDefText != null) characterDefText.text = "DEF : "  + rangedPlayer.defenseLevel;
            if (characterSpdText != null) characterSpdText.text = "SPD : "  + rangedPlayer.currentSpeed + $" ({rangedPlayer.minSpeed}~{rangedPlayer.maxSpeed})";
        }

        Debug.Log($"[StatsUIManager] 캐릭터 스탯창 표시: {character.name}");
        SetPanels(true, false); // 캐릭터 패널만 활성화
    }

    public void ShowMonsterStats(GameObject monster)
    {
        if (state != GameState.Idle)
        {
            Debug.Log("[StatsUIManager] Not Idle state. Cannot show monster stats.");
            return;
        }

        if (monsterPanel == null)
        {
            Debug.LogError("[StatsUIManager] monsterPanel이 null입니다! monsterPanel이 연결되지 않아 UI를 띄울 수 없습니다.");
            return;
        }

        Enemy enemy = monster.GetComponent<Enemy>();
        if (enemy == null)
        {
            Debug.LogWarning("[StatsUIManager] Enemy component not found on monster");
        }

        if (monsterNameText != null)
        {
            if (enemy != null && !string.IsNullOrEmpty(enemy.characterName))
                monsterNameText.text = enemy.characterName;
            else
                monsterNameText.text = monster.name;
        }

        if (enemy != null)
        {
            if (monsterHpText != null)  monsterHpText.text  = "HP : "   + enemy.hp;
            if (monsterAtkText != null) monsterAtkText.text = "ATK : "  + enemy.attackLevel;
            if (monsterDefText != null) monsterDefText.text = "DEF : "  + enemy.defenseLevel;
            if (monsterSpdText != null) monsterSpdText.text = "SPD : "  + enemy.currentSpeed + $" ({enemy.minSpeed}~{enemy.maxSpeed})";
        }

        Debug.Log($"[StatsUIManager] 몬스터 스탯창 표시: {monster.name}");
        SetPanels(false, true); // 몬스터 패널만 활성화
    }

    public void HideAllStats()
    {
        Debug.Log("[StatsUIManager] 스탯창 및 모든 UI 패널 강제 비활성화");
        SetPanels(false, false);
    }

    void SetPanels(bool characterOn, bool monsterOn)
    {
        if (characterPanel != null)
            characterPanel.SetActive(characterOn);

        if (monsterPanel != null)
            monsterPanel.SetActive(monsterOn);
    }
}
