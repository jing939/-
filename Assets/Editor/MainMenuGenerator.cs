using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuGenerator
{
    [MenuItem("Custom/Generate Main Menu Scene")]
    public static void Generate()
    {
        // 1. 새 씬 생성
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        // 2. 메인 카메라
        GameObject cameraObj = new GameObject("Main Camera");
        Camera cam = cameraObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cameraObj.AddComponent<AudioListener>();

        // 3. 캔버스 및 제어 스크립트
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // 페이드인 애니메이션
        CanvasGroup group = canvasObj.AddComponent<CanvasGroup>();
        canvasObj.AddComponent<UIFadeAnimation>();
        
        // 메인 메뉴 컨트롤러
        MainMenuController controller = canvasObj.AddComponent<MainMenuController>();

        // 4. 배경 이미지
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        
        // 생성된 배경 이미지 불러오기
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MainMenuBG.png");
        if (tex != null)
        {
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            bgImage.sprite = sprite;
        }
        else
        {
            bgImage.color = Color.darkGray;
            Debug.LogWarning("Assets/MainMenuBG.png 파일을 찾을 수 없습니다.");
        }

        // 5. 타이틀 텍스트
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "어느 날 갑자기";
        titleText.fontSize = 120;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyles.Bold;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.8f);
        titleRect.anchorMax = new Vector2(0.5f, 0.8f);
        titleRect.sizeDelta = new Vector2(1000, 200);
        titleRect.anchoredPosition = Vector2.zero;

        // 6. 버튼 생성 헬퍼 함수
        GameObject CreateButton(string name, string text, Vector2 pos, UnityEngine.Events.UnityAction action)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(canvasObj.transform, false);
            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0, 0, 0, 0.7f);
            Button btn = btnObj.AddComponent<Button>();
            
            if (action != null)
                UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, action);

            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(400, 100);
            rect.anchoredPosition = pos;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 40;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            RectTransform txtRect = textObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;

            return btnObj;
        }

        // 플레이 버튼, 설정 버튼, 종료 버튼
        CreateButton("PlayButton", "게임 시작", new Vector2(0, -50), controller.OnPlayButtonClicked);
        CreateButton("SettingsButton", "설정", new Vector2(0, -170), controller.OnSettingsButtonClicked);
        CreateButton("QuitButton", "게임 종료", new Vector2(0, -290), controller.OnQuitButtonClicked);

        // 7. 설정 창 패널
        GameObject setPanelObj = new GameObject("SettingsPanel");
        setPanelObj.transform.SetParent(canvasObj.transform, false);
        Image setPanelImg = setPanelObj.AddComponent<Image>();
        setPanelImg.color = new Color(0, 0, 0, 0.9f);
        RectTransform spRect = setPanelObj.GetComponent<RectTransform>();
        spRect.anchorMin = new Vector2(0.2f, 0.2f);
        spRect.anchorMax = new Vector2(0.8f, 0.8f);
        spRect.sizeDelta = Vector2.zero;
        
        GameObject spTextObj = new GameObject("Title");
        spTextObj.transform.SetParent(setPanelObj.transform, false);
        TextMeshProUGUI spText = spTextObj.AddComponent<TextMeshProUGUI>();
        spText.text = "설정";
        spText.fontSize = 60;
        spText.alignment = TextAlignmentOptions.Center;
        spText.color = Color.white;
        RectTransform sptRect = spTextObj.GetComponent<RectTransform>();
        sptRect.anchorMin = new Vector2(0.5f, 0.8f);
        sptRect.anchorMax = new Vector2(0.5f, 0.8f);
        sptRect.sizeDelta = new Vector2(600, 100);
        sptRect.anchoredPosition = Vector2.zero;
        
        GameObject spCloseBtn = CreateButton("CloseButton", "닫기", new Vector2(0, -200), controller.OnSettingsButtonClicked);
        spCloseBtn.transform.SetParent(setPanelObj.transform, false);

        controller.settingsPanel = setPanelObj;
        setPanelObj.SetActive(false);

        // 8. EventSystem
        GameObject esObj = new GameObject("EventSystem");
        esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // 씬 저장
        bool saved = EditorSceneManager.SaveScene(newScene, "Assets/Scenes/MainMenu.unity");
        if (saved)
        {
            Debug.Log("메인 메뉴 씬이 성공적으로 생성되고 저장되었습니다! (Assets/Scenes/MainMenu.unity)");
        }
    }
}
