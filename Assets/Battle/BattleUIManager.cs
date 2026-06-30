using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BattleUIManager : MonoBehaviour
{
    public static BattleUIManager instance;

    [Header("Clash UI Sprites (커스텀 이미지)")]
    public Sprite skillBgSprite;     // 스킬 이름 배경
    public Sprite powerCircleSprite; // 위력 표시 배경
    public Sprite coinReadySprite;   // 대기 코인
    public Sprite coinHeadSprite;    // 앞면 코인
    public Sprite coinTailSprite;    // 뒷면 코인

    [Header("Clash UI (합 시스템)")]
    public GameObject clashPanel;
    public TextMeshProUGUI playerClashText;
    public TextMeshProUGUI enemyClashText;
    public TextMeshProUGUI clashResultText;

    [Header("Dynamic Coin UI")]
    public Transform playerCoinContainer; 
    public Transform enemyCoinContainer;
    List<Image> pCoinImages = new List<Image>();
    List<Image> eCoinImages = new List<Image>();

    // [신규] 플로팅 UI 관련 변수들
    public GameObject currentClashPlayer;
    public GameObject currentClashEnemy;
    private bool isOneSidedClash;

    private RectTransform playerClashGroup;
    private RectTransform enemyClashGroup;
    private TextMeshProUGUI playerSkillText;
    private TextMeshProUGUI enemySkillText;
    
    [Header("Background Settings (UI)")]
    public Texture2D backgroundTexture;
    public Image backgroundImage;
    
    public enum CoinState { Ready, Head, Tail, Broken }

    private GameObject worldDimmer;

    void Awake()
    {
        instance = this;
        InitializeBackground();
        EnsureClashGroups(); // 시작할 때 미리 그룹을 생성하고 텍스트들을 묶어둠

        // [추가] 월드 공간에서 화면을 어둡게 만들어줄 별도의 회색 판(Dimmer)을 생성합니다.
        CreateWorldDimmer();

        ShowClashUI(false);  // 그리고 즉시 숨김
    }

    void CreateWorldDimmer()
    {
        if (worldDimmer != null) return;
        worldDimmer = new GameObject("WorldSpaceDimmer");
        SpriteRenderer sr = worldDimmer.AddComponent<SpriteRenderer>();
        
        // 1x1 하얀색 텍스처 생성, PPU를 1로 줘서 스케일 조절이 쉽도록 함
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        sr.color = new Color(0.05f, 0.05f, 0.05f, 0.75f); // 어두운 회색 (알파 0.75)
        
        worldDimmer.transform.localScale = new Vector3(100f, 100f, 1f); // 100x100 유닛으로 거대하게
        worldDimmer.transform.position = new Vector3(Camera.main != null ? Camera.main.transform.position.x : 0, Camera.main != null ? Camera.main.transform.position.y : 0, 5f); // 카메라 중심
        sr.sortingOrder = 5000; // 가만히 있는 캐릭터들보다 앞에 오도록 설정

        worldDimmer.SetActive(false);
    }

    void Update()
    {
        if (clashPanel != null && clashPanel.activeSelf)
        {
            UpdateFloatingClashPositions();
        }
    }

    void InitializeBackground()
    {
        if (backgroundImage == null && backgroundTexture != null)
        {
            // Canvas를 찾아 그 바로 아래에 배경 이미지를 생성합니다.
            Canvas canvas = GetComponentInParent<Canvas>();
            GameObject bgObj = new GameObject("FullBackground", typeof(RectTransform), typeof(Image));
            
            if (canvas != null) bgObj.transform.SetParent(canvas.transform, false);
            else bgObj.transform.SetParent(this.transform, false);

            bgObj.transform.SetAsFirstSibling(); // 가장 뒤쪽 레이어로 보냄
            
            RectTransform rt = bgObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            
            backgroundImage = bgObj.GetComponent<Image>();
            backgroundImage.sprite = Sprite.Create(backgroundTexture, 
                new Rect(0, 0, backgroundTexture.width, backgroundTexture.height), 
                new Vector2(0.5f, 0.5f));
            backgroundImage.raycastTarget = false; // 클릭 방해 방지
        }
    }

    public void ShowClashUI(bool show, GameObject player = null, GameObject enemy = null, string playerSkill = "", string enemySkill = "", bool isOneSided = false)
    {
        if (clashPanel != null)
        {
            clashPanel.SetActive(show);
            // 기존 캔버스 UI의 회색 패널을 투명하게 만들어 줍니다 (월드 딤머를 대신 사용)
            Image panelBg = clashPanel.GetComponent<Image>();
            if (panelBg != null) panelBg.color = new Color(0, 0, 0, 0);
        }

        if (worldDimmer != null) worldDimmer.SetActive(show);

        if (!show)
        {
            // 숨길때 코인 정리
            SetupClashCoins(0, 0);
            SetClashFront(null, null); // 정렬 오더 복구
            currentClashPlayer = null;
            currentClashEnemy = null;
        }
        else
        {
            currentClashPlayer = player;
            currentClashEnemy = enemy;
            isOneSidedClash = isOneSided;

            EnsureClashGroups();

            if (playerSkillText != null) playerSkillText.text = playerSkill;
            if (enemySkillText != null) enemySkillText.text = isOneSided ? "" : enemySkill;

            // 서클별 테두리 및 배너 활성화 유무 설정
            if (playerClashGroup != null) playerClashGroup.gameObject.SetActive(player != null);
            if (enemyClashGroup != null) enemyClashGroup.gameObject.SetActive(!isOneSided && enemy != null);

            UpdateFloatingClashPositions();

            // 참여 캐릭터를 맨 앞으로 가져오기
            SetClashFront(player, enemy);

            // 결과 텍스트 위치 조절
            if (clashResultText != null)
            {
                RectTransform rt = clashResultText.GetComponent<RectTransform>();
                if (rt != null) { rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.anchoredPosition = new Vector2(0f, 180f); }
            }
        }
    }

    private System.Collections.Generic.Dictionary<SpriteRenderer, int> originalSortingOrders = new System.Collections.Generic.Dictionary<SpriteRenderer, int>();
    private System.Collections.Generic.Dictionary<Canvas, int> originalCanvasOrders = new System.Collections.Generic.Dictionary<Canvas, int>();

    void SetClashFront(GameObject clashPlayer, GameObject clashEnemy)
    {
        // 원래 소팅 오더로 전부 복구
        foreach(var kv in originalSortingOrders)
        {
            if (kv.Key != null) kv.Key.sortingOrder = kv.Value;
        }
        originalSortingOrders.Clear();

        foreach(var kv in originalCanvasOrders)
        {
            if (kv.Key != null) kv.Key.sortingOrder = kv.Value;
        }
        originalCanvasOrders.Clear();

        if (clashPlayer == null && clashEnemy == null) return;

        // 합에 참여하는 케릭터들 목록
        System.Collections.Generic.HashSet<GameObject> participants = new System.Collections.Generic.HashSet<GameObject>();
        if (clashPlayer != null) participants.Add(clashPlayer);
        if (clashEnemy != null) participants.Add(clashEnemy);

        foreach(var obj in participants)
        {
            if (obj == null) continue;

            SpriteRenderer[] srs = obj.GetComponentsInChildren<SpriteRenderer>();
            foreach(var sr in srs)
            {
                if (sr != null)
                {
                    originalSortingOrders[sr] = sr.sortingOrder;
                    sr.sortingOrder += 10000; // 엄청 높여서 제일 앞으로 가져옴
                }
            }
            
            Canvas[] canvases = obj.GetComponentsInChildren<Canvas>();
            foreach(var c in canvases)
            {
                if (c != null)
                {
                    originalCanvasOrders[c] = c.sortingOrder;
                    c.sortingOrder += 10000;
                }
            }
        }
    }

    void EnsureClashGroups()
    {
        if (clashPanel == null) return;

        // Player Group
        if (playerClashGroup == null)
        {
            GameObject pGroupObj = new GameObject("PlayerClashGroup", typeof(RectTransform));
            pGroupObj.transform.SetParent(clashPanel.transform, false);
            playerClashGroup = pGroupObj.GetComponent<RectTransform>();

            // 스킬 이름 배너 백그라운드 (크기 축소 및 좌측 하단 배치)
            GameObject pSkillBg = new GameObject("SkillBg", typeof(RectTransform), typeof(Image));
            pSkillBg.transform.SetParent(playerClashGroup, false);
            RectTransform pSkillBgRt = pSkillBg.GetComponent<RectTransform>();
            pSkillBgRt.sizeDelta = new Vector2(160, 42);
            pSkillBgRt.anchoredPosition = new Vector2(-100, -20); // 더 왼쪽으로, Y간격 좁힘
            Image bgImg = pSkillBg.GetComponent<Image>();
            if (skillBgSprite != null)
            {
                bgImg.sprite = skillBgSprite;
                bgImg.color = Color.white;
            }
            else
            {
                bgImg.color = new Color(0.12f, 0.02f, 0.02f, 0.9f); // 짙은 적갈색
            }

            Outline bgOutline = pSkillBg.AddComponent<Outline>();
            bgOutline.effectColor = new Color(0.9f, 0.15f, 0.15f);
            bgOutline.effectDistance = new Vector2(1.5f, 1.5f);

            // 스킬 이름 텍스트
            GameObject pSkillTextObj = new GameObject("SkillText", typeof(RectTransform), typeof(TextMeshProUGUI));
            pSkillTextObj.transform.SetParent(pSkillBg.transform, false);
            playerSkillText = pSkillTextObj.GetComponent<TextMeshProUGUI>();
            playerSkillText.font = playerClashText != null ? playerClashText.font : null;
            playerSkillText.fontSize = 17;
            playerSkillText.alignment = TextAlignmentOptions.Center;
            playerSkillText.color = Color.white;
            playerSkillText.fontStyle = FontStyles.Bold;
            
            RectTransform pSkillTextRt = pSkillTextObj.GetComponent<RectTransform>();
            pSkillTextRt.anchorMin = Vector2.zero; pSkillTextRt.anchorMax = Vector2.one;
            pSkillTextRt.offsetMin = Vector2.zero; pSkillTextRt.offsetMax = Vector2.zero;

            // 위력 표시용 어두운 서클 배경판 생성
            if (playerClashText != null)
            {
                GameObject pClashCircle = new GameObject("PowerCircleBg", typeof(RectTransform), typeof(Image));
                pClashCircle.transform.SetParent(playerClashGroup, false);
                RectTransform pClashCircleRt = pClashCircle.GetComponent<RectTransform>();
                pClashCircleRt.sizeDelta = new Vector2(65, 65); // 크기 축소
                pClashCircleRt.anchoredPosition = new Vector2(0, 0);
                Image circleImg = pClashCircle.GetComponent<Image>();
                if (powerCircleSprite != null)
                {
                    circleImg.sprite = powerCircleSprite;
                    circleImg.color = Color.white;
                }
                else
                {
                    circleImg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f); // 어두운 서클
                }

                Outline circleOutline = pClashCircle.AddComponent<Outline>();
                circleOutline.effectColor = new Color(0.9f, 0.15f, 0.15f);
                circleOutline.effectDistance = new Vector2(2, 2);

                // 기존 플레이어 Clash Text를 서클 내부로 가져옴
                playerClashText.transform.SetParent(pClashCircle.transform, false);
                RectTransform pTextRt = playerClashText.GetComponent<RectTransform>();
                pTextRt.anchorMin = Vector2.zero; pTextRt.anchorMax = Vector2.one;
                pTextRt.offsetMin = Vector2.zero; pTextRt.offsetMax = Vector2.zero;
                pTextRt.anchoredPosition = Vector2.zero;
                playerClashText.alignment = TextAlignmentOptions.Center;
                playerClashText.fontSize = 26; // 폰트 크기 축소
                playerClashText.fontStyle = FontStyles.Bold;
            }
        }

        // Enemy Group
        if (enemyClashGroup == null)
        {
            GameObject eGroupObj = new GameObject("EnemyClashGroup", typeof(RectTransform));
            eGroupObj.transform.SetParent(clashPanel.transform, false);
            enemyClashGroup = eGroupObj.GetComponent<RectTransform>();

            // 스킬 이름 배너 백그라운드 (우측 하단 배치)
            GameObject eSkillBg = new GameObject("SkillBg", typeof(RectTransform), typeof(Image));
            eSkillBg.transform.SetParent(enemyClashGroup, false);
            RectTransform eSkillBgRt = eSkillBg.GetComponent<RectTransform>();
            eSkillBgRt.sizeDelta = new Vector2(160, 42);
            eSkillBgRt.anchoredPosition = new Vector2(100, -20); // 더 오른쪽으로, Y간격 좁힘
            Image bgImg = eSkillBg.GetComponent<Image>();
            if (skillBgSprite != null)
            {
                bgImg.sprite = skillBgSprite;
                bgImg.color = Color.white;
            }
            else
            {
                bgImg.color = new Color(0.02f, 0.08f, 0.03f, 0.9f); // 짙은 녹적색
            }

            Outline bgOutline = eSkillBg.AddComponent<Outline>();
            bgOutline.effectColor = new Color(0.2f, 0.8f, 0.3f);
            bgOutline.effectDistance = new Vector2(1.5f, 1.5f);

            // 스킬 이름 텍스트
            GameObject eSkillTextObj = new GameObject("SkillText", typeof(RectTransform), typeof(TextMeshProUGUI));
            eSkillTextObj.transform.SetParent(eSkillBg.transform, false);
            enemySkillText = eSkillTextObj.GetComponent<TextMeshProUGUI>();
            enemySkillText.font = enemyClashText != null ? enemyClashText.font : null;
            enemySkillText.fontSize = 17;
            enemySkillText.alignment = TextAlignmentOptions.Center;
            enemySkillText.color = Color.white;
            enemySkillText.fontStyle = FontStyles.Bold;
            
            RectTransform eSkillTextRt = eSkillTextObj.GetComponent<RectTransform>();
            eSkillTextRt.anchorMin = Vector2.zero; eSkillTextRt.anchorMax = Vector2.one;
            eSkillTextRt.offsetMin = Vector2.zero; eSkillTextRt.offsetMax = Vector2.zero;

            // 위력 표시용 어두운 서클 배경판 생성
            if (enemyClashText != null)
            {
                GameObject eClashCircle = new GameObject("PowerCircleBg", typeof(RectTransform), typeof(Image));
                eClashCircle.transform.SetParent(enemyClashGroup, false);
                RectTransform eClashCircleRt = eClashCircle.GetComponent<RectTransform>();
                eClashCircleRt.sizeDelta = new Vector2(65, 65);
                eClashCircleRt.anchoredPosition = new Vector2(0, 0);
                Image circleImg = eClashCircle.GetComponent<Image>();
                if (powerCircleSprite != null)
                {
                    circleImg.sprite = powerCircleSprite;
                    circleImg.color = Color.white;
                }
                else
                {
                    circleImg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
                }

                Outline circleOutline = eClashCircle.AddComponent<Outline>();
                circleOutline.effectColor = new Color(0.2f, 0.8f, 0.3f);
                circleOutline.effectDistance = new Vector2(2, 2);

                // 기존 적군 Clash Text를 서클 내부로 가져옴
                enemyClashText.transform.SetParent(eClashCircle.transform, false);
                RectTransform eTextRt = enemyClashText.GetComponent<RectTransform>();
                eTextRt.anchorMin = Vector2.zero; eTextRt.anchorMax = Vector2.one;
                eTextRt.offsetMin = Vector2.zero; eTextRt.offsetMax = Vector2.zero;
                eTextRt.anchoredPosition = Vector2.zero;
                enemyClashText.alignment = TextAlignmentOptions.Center;
                enemyClashText.fontSize = 26;
                enemyClashText.fontStyle = FontStyles.Bold;
            }
        }
    }

    void UpdateFloatingClashPositions()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Canvas parentCanvas = clashPanel.GetComponentInParent<Canvas>();
        if (parentCanvas == null) return;

        RectTransform panelRt = clashPanel.GetComponent<RectTransform>();

        // Player UI 위치 갱신
        if (currentClashPlayer != null && playerClashGroup != null)
        {
            SpriteRenderer sr = currentClashPlayer.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                // bounds.max는 우측 상단이므로, x는 중앙, y는 최상단으로 설정하여 정확히 머리 위를 추적합니다.
                Vector3 headWorld = new Vector3(sr.bounds.center.x, sr.bounds.max.y, sr.bounds.center.z);
                Vector3 screenPos = cam.WorldToScreenPoint(headWorld);
                Vector2 localPos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRt, screenPos, parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam, out localPos))
                {
                    playerClashGroup.anchoredPosition = localPos + new Vector2(0, 50f);
                }
            }
        }

        // Enemy UI 위치 갱신
        if (currentClashEnemy != null && enemyClashGroup != null && !isOneSidedClash)
        {
            SpriteRenderer sr = currentClashEnemy.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                Vector3 headWorld = new Vector3(sr.bounds.center.x, sr.bounds.max.y, sr.bounds.center.z);
                Vector3 screenPos = cam.WorldToScreenPoint(headWorld);
                Vector2 localPos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRt, screenPos, parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam, out localPos))
                {
                    enemyClashGroup.anchoredPosition = localPos + new Vector2(0, 50f);
                }
            }
        }
    }

    public void UpdateClashNumbers(int pRoll, int eRoll)
    {
        if (playerClashText != null && playerClashText.text != pRoll.ToString())
        {
            playerClashText.text = pRoll.ToString();
            PlayPopupAnim(playerClashText);
        }
        
        if (enemyClashText != null && enemyClashText.text != eRoll.ToString())
        {
            enemyClashText.text = eRoll.ToString();
            PlayPopupAnim(enemyClashText);
        }
    }

    private System.Collections.Generic.Dictionary<TextMeshProUGUI, Coroutine> popupCoroutines = new System.Collections.Generic.Dictionary<TextMeshProUGUI, Coroutine>();

    private void PlayPopupAnim(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        if (popupCoroutines.ContainsKey(tmp) && popupCoroutines[tmp] != null)
        {
            StopCoroutine(popupCoroutines[tmp]);
        }
        popupCoroutines[tmp] = StartCoroutine(AnimateTextPopup(tmp));
    }

    private System.Collections.IEnumerator AnimateTextPopup(TextMeshProUGUI tmp)
    {
        RectTransform rt = tmp.GetComponent<RectTransform>();
        if (rt == null) yield break;

        float duration = 0.1f; // 커지는 속도
        float elapsed = 0f;
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = new Vector3(1.6f, 1.6f, 1f);

        // 커지기 (빠르게)
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rt.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            yield return null;
        }
        
        // 작아지기 (조금 더 천천히)
        duration = 0.15f;
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rt.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            yield return null;
        }

        rt.localScale = originalScale;
    }

    public void ShowResult(string message)
    {
        if (clashResultText != null)
        {
            clashResultText.text = message;
            clashResultText.gameObject.SetActive(true);
            clashResultText.transform.SetAsLastSibling(); // ClashPanel 등에 가려지지 않고 무조건 맨 위에 렌더링되게 함
        }
    }

    // =========================
    // Dynamic Coin UI System
    // =========================
    
    public void SetupClashCoins(int pCount, int eCount)
    {
        EnsureCoinContainers();

        // 플레이어 코인 풀링 및 활성화
        while (pCoinImages.Count < pCount)
        {
            Image img = CreateCoinImage(playerCoinContainer);
            pCoinImages.Add(img);
        }

        for (int i = 0; i < pCoinImages.Count; i++)
        {
            if (i < pCount)
            {
                pCoinImages[i].gameObject.SetActive(true);
                pCoinImages[i].color = Color.black;
                pCoinImages[i].transform.SetAsFirstSibling();
            }
            else
            {
                pCoinImages[i].gameObject.SetActive(false);
            }
        }

        // 적 코인 풀링 및 활성화
        while (eCoinImages.Count < eCount)
        {
            Image img = CreateCoinImage(enemyCoinContainer);
            eCoinImages.Add(img);
        }

        for (int i = 0; i < eCoinImages.Count; i++)
        {
            if (i < eCount)
            {
                eCoinImages[i].gameObject.SetActive(true);
                eCoinImages[i].color = Color.black;
                eCoinImages[i].transform.SetAsLastSibling();
            }
            else
            {
                eCoinImages[i].gameObject.SetActive(false);
            }
        }
    }

    public void UpdatePlayerCoin(int index, CoinState state) { ApplyCoinState(pCoinImages, index, state); }
    public void UpdateEnemyCoin(int index, CoinState state) { ApplyCoinState(eCoinImages, index, state); }

    void ApplyCoinState(List<Image> list, int index, CoinState state)
    {
        if (index < 0 || index >= list.Count) return;
        Image img = list[index];
        if (img == null) return;

        switch(state)
        {
            case CoinState.Ready: 
                if (coinReadySprite != null) { img.sprite = coinReadySprite; img.color = Color.white; }
                else img.color = Color.black; 
                break;
            case CoinState.Head: 
                if (coinHeadSprite != null) { img.sprite = coinHeadSprite; img.color = Color.white; }
                else img.color = new Color(1f, 0.8f, 0f); 
                break;
            case CoinState.Tail: 
                if (coinTailSprite != null) { img.sprite = coinTailSprite; img.color = Color.white; }
                else img.color = Color.gray; 
                break;
            case CoinState.Broken: 
                img.color = Color.clear; 
                break;
        }
    }

    Image CreateCoinImage(Transform parent)
    {
        GameObject obj = new GameObject("DynamicCoin", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(26, 26);

        Image img = obj.GetComponent<Image>();
        img.color = Color.black; // 초기색상 Ready (검은색)
        
        return img;
    }

    void EnsureCoinContainers()
    {
        if (playerClashText == null || enemyClashText == null) return;

        if (playerCoinContainer == null)
        {
            GameObject pContainer = new GameObject("PlayerCoinContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            pContainer.transform.SetParent(playerClashText.transform.parent, false);
            
            RectTransform rt = pContainer.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f); // 우측 기준 정렬
            rt.anchoredPosition = new Vector2(-45f, 15f); // 스킬 이름과의 간격을 줄이기 위해 Y위치 낮춤

            ContentSizeFitter csf = pContainer.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            HorizontalLayoutGroup layout = pContainer.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 2; // 코인 사이에 약간의 간격 유지
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = false; layout.childControlHeight = false;
            layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
            
            playerCoinContainer = pContainer.transform;
            Debug.Log("[BattleUIManager] Player Coin Container 자동 생성");
        }

        if (enemyCoinContainer == null)
        {
            GameObject eContainer = new GameObject("EnemyCoinContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            eContainer.transform.SetParent(enemyClashText.transform.parent, false);

            RectTransform rt = eContainer.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f); // 좌측 기준 정렬
            rt.anchoredPosition = new Vector2(45f, 15f); // 스킬 이름과의 간격을 줄이기 위해 Y위치 낮춤

            ContentSizeFitter csf = eContainer.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            HorizontalLayoutGroup layout = eContainer.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 2; // 코인 사이에 약간의 간격 유지
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false; layout.childControlHeight = false;
            layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;

            enemyCoinContainer = eContainer.transform;
            Debug.Log("[BattleUIManager] Enemy Coin Container 자동 생성");
        }
    }
}






