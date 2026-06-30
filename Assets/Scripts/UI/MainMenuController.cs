using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Settings Panel")]
    public GameObject settingsPanel;

    [Header("Scene Management")]
    public string playSceneName = "ExploreScene";

    Slider masterSlider;
    Slider bgmSlider;
    Slider sfxSlider;
    TextMeshProUGUI masterLabel;
    TextMeshProUGUI bgmLabel;
    TextMeshProUGUI sfxLabel;

    void Awake()
    {
        EnsureAudioManager();
    }

    TMPro.TMP_FontAsset GetDefaultFont()
    {
        TMPro.TMP_FontAsset font = TMPro.TMP_Settings.defaultFontAsset;
        if (font != null) return font;

        font = Resources.Load<TMPro.TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null) return font;

        var fonts = Resources.FindObjectsOfTypeAll<TMPro.TMP_FontAsset>();
        if (fonts != null && fonts.Length > 0) return fonts[0];

        return null;
    }

    void Start()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        BuildSettingsUI();
        RefreshVolumeLabels();

        // 런타임에 PlayButton 자동 링킹 추가
        Button playBtn = GameObject.Find("PlayButton")?.GetComponent<Button>();
        if (playBtn == null) playBtn = GameObject.Find("Play Button")?.GetComponent<Button>();
        if (playBtn == null) playBtn = GameObject.Find("StartButton")?.GetComponent<Button>();
        if (playBtn == null) playBtn = GameObject.Find("Start Button")?.GetComponent<Button>();
        if (playBtn != null)
        {
            playBtn.onClick.RemoveAllListeners();
            playBtn.onClick.AddListener(OnPlayButtonClicked);
        }
    }

    void EnsureAudioManager()
    {
        if (AudioManager.instance != null) return;
        var go = new GameObject("AudioManager");
        go.AddComponent<AudioManager>();
    }

    void BuildSettingsUI()
    {
        if (settingsPanel == null) return;

        TMPro.TMP_FontAsset useFont = GetDefaultFont();

        Transform title = settingsPanel.transform.Find("Title");
        if (title != null)
        {
            var titleText = title.GetComponent<TextMeshProUGUI>();
            if (titleText != null)
            {
                if (useFont != null) titleText.font = useFont;
                titleText.text = "설정";
                titleText.fontSize = 48;
            }
        }

        if (settingsPanel.transform.Find("VolumeContainer") != null)
            return;

        GameObject container = new GameObject("VolumeContainer");
        container.transform.SetParent(settingsPanel.transform, false);
        RectTransform containerRt = container.AddComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0.1f, 0.25f);
        containerRt.anchorMax = new Vector2(0.9f, 0.75f);
        containerRt.offsetMin = Vector2.zero;
        containerRt.offsetMax = Vector2.zero;

        var layout = container.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 28;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        masterSlider = CreateVolumeRow(container.transform, "전체 볼륨", 0.55f, out masterLabel);
        bgmSlider    = CreateVolumeRow(container.transform, "배경음", 0.75f, out bgmLabel);
        sfxSlider    = CreateVolumeRow(container.transform, "효과음", 0.9f, out sfxLabel);

        if (useFont != null)
        {
            if (masterLabel != null) masterLabel.font = useFont;
            if (bgmLabel != null)    bgmLabel.font    = useFont;
            if (sfxLabel != null)    sfxLabel.font    = useFont;
        }

        if (AudioManager.instance != null)
        {
            masterSlider.value = AudioManager.instance.masterVolume;
            bgmSlider.value    = AudioManager.instance.bgmVolume;
            sfxSlider.value    = AudioManager.instance.sfxVolume;
        }

        masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
    }

    Slider CreateVolumeRow(Transform parent, string label, float defaultValue, out TextMeshProUGUI valueLabel)
    {
        TMPro.TMP_FontAsset useFont = GetDefaultFont();

        GameObject row = new GameObject(label + "Row");
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.AddComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(0, 70);

        var rowLayout = row.AddComponent<VerticalLayoutGroup>();
        rowLayout.spacing = 8;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = false;

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(row.transform, false);
        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        if (useFont != null) labelText.font = useFont;
        labelText.text = label;
        labelText.fontSize = 28;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.Left;

        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(row.transform, false);
        RectTransform sliderRt = sliderObj.AddComponent<RectTransform>();
        sliderRt.sizeDelta = new Vector2(0, 30);

        Image bg = sliderObj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = defaultValue;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.offsetMin = new Vector2(10, 6);
        fillAreaRt.offsetMax = new Vector2(-10, -6);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.35f, 0.75f, 0.45f, 1f);
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        slider.fillRect = fillRt;

        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRt = handleArea.AddComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = new Vector2(10, 0);
        handleAreaRt.offsetMax = new Vector2(-10, 0);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        RectTransform handleRt = handle.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(24, 24);
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;

        GameObject valueObj = new GameObject("Value");
        valueObj.transform.SetParent(row.transform, false);
        valueLabel = valueObj.AddComponent<TextMeshProUGUI>();
        if (useFont != null) valueLabel.font = useFont;
        valueLabel.fontSize = 22;
        valueLabel.color = new Color(0.8f, 0.9f, 0.8f, 1f);
        valueLabel.alignment = TextAlignmentOptions.Right;

        return slider;
    }

    void OnMasterVolumeChanged(float value)
    {
        AudioManager.instance?.SetMasterVolume(value);
        RefreshVolumeLabels();
    }

    void OnBgmVolumeChanged(float value)
    {
        AudioManager.instance?.SetBgmVolume(value);
        RefreshVolumeLabels();
    }

    void OnSfxVolumeChanged(float value)
    {
        AudioManager.instance?.SetSfxVolume(value);
        RefreshVolumeLabels();
    }

    void RefreshVolumeLabels()
    {
        if (AudioManager.instance == null) return;
        if (masterLabel != null) masterLabel.text = $"{Mathf.RoundToInt(AudioManager.instance.masterVolume * 100)}%";
        if (bgmLabel != null)    bgmLabel.text    = $"{Mathf.RoundToInt(AudioManager.instance.bgmVolume * 100)}%";
        if (sfxLabel != null)    sfxLabel.text    = $"{Mathf.RoundToInt(AudioManager.instance.sfxVolume * 100)}%";
    }

    public void OnPlayButtonClicked()
    {
        Debug.Log("Play Button Clicked. Setting NewGameStart flag and loading scene: " + playSceneName);
        PlayerPrefs.SetInt("NewGameStart", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(playSceneName);
    }

    public void OnSettingsButtonClicked()
    {
        if (settingsPanel == null) return;

        bool isActive = settingsPanel.activeSelf;
        settingsPanel.SetActive(!isActive);

        if (!isActive)
            RefreshVolumeLabels();
    }

    public void OnQuitButtonClicked()
    {
        Debug.Log("Quit Button Clicked. Exiting game.");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
