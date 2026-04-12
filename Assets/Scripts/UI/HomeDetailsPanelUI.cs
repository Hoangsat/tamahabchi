using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum HomeDetailsTab
{
    Pet,
    Stats,
    Relic,
    Mastery,
    Trait
}

public class HomeDetailsPanelUI : MonoBehaviour
{
    public GameObject panelRoot;
    public Button closeButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
    public TextMeshProUGUI statusText;

    private readonly Dictionary<HomeDetailsTab, Button> tabButtons = new Dictionary<HomeDetailsTab, Button>();
    private readonly Dictionary<HomeDetailsTab, TextMeshProUGUI> tabLabels = new Dictionary<HomeDetailsTab, TextMeshProUGUI>();

    private GameManager gameManager;
    private HomeDetailsTab selectedTab = HomeDetailsTab.Pet;

    private RectTransform screenRoot;
    private RectTransform contentRoot;
    private TextMeshProUGUI contentTitleText;
    private TextMeshProUGUI contentBodyText;
    private TextMeshProUGUI footerNoteText;

    private void Awake()
    {
        BuildUiIfNeeded();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
            closeButton.gameObject.SetActive(false);
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void OnEnable()
    {
        AttachEvents();
    }

    private void OnDisable()
    {
        DetachEvents();
    }

    public void SetGameManager(GameManager manager)
    {
        if (gameManager == manager)
        {
            return;
        }

        DetachEvents();
        gameManager = manager;
        AttachEvents();
        RefreshUI();
    }

    public void ShowPanel()
    {
        BuildUiIfNeeded();
        EnsurePanelLayering();

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(false);
        }

        selectedTab = HomeDetailsTab.Pet;
        SetStatus("Scaffold view. We can reshape this later.");
        RefreshUI();
    }

    public void HidePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        SetStatus(string.Empty);
    }

    public bool IsPanelVisible()
    {
        return panelRoot != null && panelRoot.activeSelf;
    }

    public void SelectTab(HomeDetailsTab tab)
    {
        selectedTab = tab;
        RefreshUI();
    }

    public string GetSelectedTabLabel()
    {
        return HomeDetailsPresenter.GetTabLabel(selectedTab);
    }

    private void ClosePanel()
    {
        HidePanel();
    }

    private void AttachEvents()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnCoinsChanged -= RefreshUI;
        gameManager.OnPetChanged -= RefreshUI;
        gameManager.OnPetFlowChanged -= RefreshUI;
        gameManager.OnProgressionChanged -= RefreshUI;
        gameManager.OnSkillsChanged -= RefreshUI;

        gameManager.OnCoinsChanged += RefreshUI;
        gameManager.OnPetChanged += RefreshUI;
        gameManager.OnPetFlowChanged += RefreshUI;
        gameManager.OnProgressionChanged += RefreshUI;
        gameManager.OnSkillsChanged += RefreshUI;
    }

    private void DetachEvents()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnCoinsChanged -= RefreshUI;
        gameManager.OnPetChanged -= RefreshUI;
        gameManager.OnPetFlowChanged -= RefreshUI;
        gameManager.OnProgressionChanged -= RefreshUI;
        gameManager.OnSkillsChanged -= RefreshUI;
    }

    private void RefreshUI()
    {
        if (panelRoot != null && !panelRoot.activeSelf)
        {
            return;
        }

        BuildUiIfNeeded();
        EnsurePanelLayering();
        ApplyScreenVisuals();

        if (titleText != null)
        {
            titleText.text = "Home";
        }

        if (gameManager == null)
        {
            ApplyViewData(HomeDetailsPresenter.BuildUnavailable(selectedTab));
            UpdateTabSelection();
            return;
        }

        ApplyViewData(gameManager.GetHomeDetailsView(selectedTab));
        UpdateTabSelection();
    }

    private void ApplyViewData(HomeDetailsViewData viewData)
    {
        if (subtitleText != null)
        {
            subtitleText.text = viewData.Subtitle;
        }

        if (contentTitleText != null)
        {
            contentTitleText.text = viewData.Title;
        }

        if (contentBodyText != null)
        {
            contentBodyText.text = viewData.Body;
        }

        if (footerNoteText != null)
        {
            footerNoteText.text = viewData.Footer;
        }
    }

    private void SetStatus(string message)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = message ?? string.Empty;
        statusText.gameObject.SetActive(!string.IsNullOrEmpty(statusText.text));
    }

    private void UpdateTabSelection()
    {
        foreach (KeyValuePair<HomeDetailsTab, Button> pair in tabButtons)
        {
            Image buttonImage = pair.Value != null ? pair.Value.GetComponent<Image>() : null;
            bool selected = pair.Key == selectedTab;
            if (buttonImage != null)
            {
                buttonImage.color = selected
                    ? new Color(0.55f, 0.34f, 0.84f, 1f)
                    : new Color(0.2f, 0.24f, 0.32f, 0.98f);
            }

            if (tabLabels.TryGetValue(pair.Key, out TextMeshProUGUI label) && label != null)
            {
                label.color = selected
                    ? new Color(0.98f, 0.96f, 1f, 1f)
                    : new Color(0.82f, 0.87f, 0.95f, 0.95f);
                label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
            }
        }
    }

    private void BuildUiIfNeeded()
    {
        if (panelRoot != null)
        {
            return;
        }

        RectTransform canvasRect = transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        panelRoot = CreatePanel(canvasRect, "HomeDetailsPanelRoot", new Color(0.07f, 0.08f, 0.14f, 0.96f));
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = new Vector2(0f, 146f);
        panelRect.offsetMax = Vector2.zero;
        panelRoot.AddComponent<RectMask2D>();

        screenRoot = CreateObject("HomeDetailsScreenRoot", panelRect);
        screenRoot.anchorMin = Vector2.zero;
        screenRoot.anchorMax = Vector2.one;
        screenRoot.offsetMin = new Vector2(20f, 20f);
        screenRoot.offsetMax = new Vector2(-20f, -20f);

        VerticalLayoutGroup screenLayout = screenRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        screenLayout.spacing = 14f;
        screenLayout.childAlignment = TextAnchor.UpperCenter;
        screenLayout.childControlWidth = true;
        screenLayout.childControlHeight = false;
        screenLayout.childForceExpandWidth = true;
        screenLayout.childForceExpandHeight = false;

        RectTransform headerRow = CreateObject("HeaderRow", screenRoot);
        HorizontalLayoutGroup headerLayout = headerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 12f;
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.childControlWidth = false;
        headerLayout.childControlHeight = false;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;

        closeButton = CreateActionButton(headerRow, "CloseButton", "Back", ClosePanel);
        LayoutElement closeLayout = closeButton.gameObject.AddComponent<LayoutElement>();
        closeLayout.preferredWidth = 120f;
        closeLayout.preferredHeight = 52f;

        RectTransform titleBlock = CreateObject("TitleBlock", headerRow);
        LayoutElement titleLayoutElement = titleBlock.gameObject.AddComponent<LayoutElement>();
        titleLayoutElement.preferredWidth = 360f;
        titleLayoutElement.flexibleWidth = 1f;

        VerticalLayoutGroup titleLayout = titleBlock.gameObject.AddComponent<VerticalLayoutGroup>();
        titleLayout.spacing = 4f;
        titleLayout.childAlignment = TextAnchor.MiddleLeft;
        titleLayout.childControlWidth = true;
        titleLayout.childControlHeight = false;
        titleLayout.childForceExpandWidth = true;
        titleLayout.childForceExpandHeight = false;

        titleText = CreateText(titleBlock, "TitleText", "Home", 42f, FontStyles.Bold, TextAlignmentOptions.Left);
        subtitleText = CreateText(titleBlock, "SubtitleText", "Pet profile scaffold", 22f, FontStyles.Normal, TextAlignmentOptions.Left);
        subtitleText.color = new Color(1f, 0.86f, 0.4f, 1f);

        statusText = CreateText(screenRoot, "StatusText", string.Empty, 21f, FontStyles.Normal, TextAlignmentOptions.Left);
        statusText.color = new Color(0.98f, 0.82f, 0.52f, 1f);
        statusText.gameObject.SetActive(false);

        RectTransform heroCard = CreatePanel(screenRoot, "HeroCard", new Color(0.17f, 0.18f, 0.28f, 0.98f)).GetComponent<RectTransform>();
        LayoutElement heroLayout = heroCard.gameObject.AddComponent<LayoutElement>();
        heroLayout.preferredHeight = 110f;
        VerticalLayoutGroup heroCardLayout = heroCard.gameObject.AddComponent<VerticalLayoutGroup>();
        heroCardLayout.padding = new RectOffset(20, 20, 16, 16);
        heroCardLayout.spacing = 6f;
        heroCardLayout.childAlignment = TextAnchor.UpperLeft;
        heroCardLayout.childControlWidth = true;
        heroCardLayout.childControlHeight = false;
        heroCardLayout.childForceExpandWidth = true;
        heroCardLayout.childForceExpandHeight = false;
        CreateText(heroCard, "HeroTitle", "Home Details", 28f, FontStyles.Bold, TextAlignmentOptions.Left);
        CreateText(heroCard, "HeroBody", "Legend-style scaffold: repeat Home tap to inspect your pet sheet and future progression tabs.", 20f, FontStyles.Normal, TextAlignmentOptions.Left);

        RectTransform tabRow = CreateObject("TabRow", screenRoot);
        HorizontalLayoutGroup tabLayout = tabRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabLayout.spacing = 8f;
        tabLayout.childAlignment = TextAnchor.MiddleCenter;
        tabLayout.childControlWidth = true;
        tabLayout.childControlHeight = true;
        tabLayout.childForceExpandWidth = true;
        tabLayout.childForceExpandHeight = false;
        LayoutElement tabRowLayout = tabRow.gameObject.AddComponent<LayoutElement>();
        tabRowLayout.preferredHeight = 58f;

        CreateTabButton(tabRow, HomeDetailsTab.Pet);
        CreateTabButton(tabRow, HomeDetailsTab.Stats);
        CreateTabButton(tabRow, HomeDetailsTab.Relic);
        CreateTabButton(tabRow, HomeDetailsTab.Mastery);
        CreateTabButton(tabRow, HomeDetailsTab.Trait);

        contentRoot = CreatePanel(screenRoot, "ContentRoot", new Color(0.1f, 0.13f, 0.19f, 0.96f)).GetComponent<RectTransform>();
        LayoutElement contentLayoutElement = contentRoot.gameObject.AddComponent<LayoutElement>();
        contentLayoutElement.flexibleHeight = 1f;
        contentLayoutElement.preferredHeight = 520f;

        VerticalLayoutGroup contentLayout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(20, 20, 20, 20);
        contentLayout.spacing = 10f;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        contentTitleText = CreateText(contentRoot, "ContentTitleText", "Pet", 32f, FontStyles.Bold, TextAlignmentOptions.Left);
        contentBodyText = CreateText(contentRoot, "ContentBodyText", string.Empty, 23f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        contentBodyText.textWrappingMode = TextWrappingModes.Normal;
        LayoutElement contentBodyLayout = contentBodyText.gameObject.AddComponent<LayoutElement>();
        contentBodyLayout.flexibleHeight = 1f;
        contentBodyLayout.preferredHeight = 360f;

        footerNoteText = CreateText(contentRoot, "FooterNoteText", string.Empty, 19f, FontStyles.Italic, TextAlignmentOptions.Left);
        footerNoteText.color = new Color(0.72f, 0.79f, 0.9f, 0.92f);
    }

    private void ApplyScreenVisuals()
    {
        if (panelRoot == null)
        {
            return;
        }

        Image background = panelRoot.GetComponent<Image>();
        if (background != null)
        {
            background.color = new Color(0.07f, 0.08f, 0.14f, 0.96f);
        }
    }

    private void EnsurePanelLayering()
    {
        if (panelRoot != null)
        {
            Transform shellRuntimeRoot = transform.Find("ShellRuntimeRoot");
            if (shellRuntimeRoot != null)
            {
                int shellIndex = shellRuntimeRoot.GetSiblingIndex();
                panelRoot.transform.SetSiblingIndex(Mathf.Max(0, shellIndex - 1));
            }
        }
    }

    private void CreateTabButton(RectTransform parent, HomeDetailsTab tab)
    {
        Button button = CreateActionButton(parent, $"{tab}TabButton", HomeDetailsPresenter.GetTabLabel(tab), () => SelectTab(tab));
        LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 52f;
        layout.flexibleWidth = 1f;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        tabButtons[tab] = button;
        tabLabels[tab] = label;
    }

    private GameObject CreatePanel(RectTransform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private RectTransform CreateObject(string name, RectTransform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return rect;
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string name, string value, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateObject(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = new Color(0.94f, 0.97f, 1f, 1f);
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private Button CreateActionButton(RectTransform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreatePanel(parent, name, new Color(0.2f, 0.24f, 0.32f, 0.98f));
        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        RectTransform textRect = CreateObject("Label", buttonObject.transform as RectTransform);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 22f;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(0.98f, 0.96f, 1f, 1f);

        return button;
    }
}
