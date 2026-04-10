using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum AppScreen
{
    Home,
    Skills,
    Missions,
    Shop,
    Room,
    Focus
}

public class AppShellUI : MonoBehaviour
{
    private GameManager gameManager;
    private SkillsPanelUI skillsPanelUI;
    private MissionPanelUI missionPanelUI;
    private FocusPanelUI focusPanelUI;

    private GameObject shellRoot;
    private TextMeshProUGUI currentContextText;
    private TextMeshProUGUI shellHintText;
    private Button homeButton;
    private Button skillsButton;
    private Button missionsButton;
    private Button shopButton;
    private Button roomButton;
    private RectTransform shellRuntimeRoot;

    private AppScreen currentScreen = AppScreen.Home;
    private AppScreen focusOriginScreen = AppScreen.Home;
    private bool wiredSectionButtons;

    private void Awake()
    {
        BuildShellIfNeeded();
    }

    private void OnEnable()
    {
        WireSectionButtons();
        SubscribeToEvents();
        SyncShellToRuntime();
    }

    private void Start()
    {
        SyncShellToRuntime();
    }

    public void SetDependencies(GameManager manager, SkillsPanelUI skillsPanel, MissionPanelUI missionPanel, FocusPanelUI focusPanel)
    {
        UnsubscribeFromEvents();
        gameManager = manager;
        skillsPanelUI = skillsPanel;
        missionPanelUI = missionPanel;
        focusPanelUI = focusPanel;
        wiredSectionButtons = false;
        WireSectionButtons();
        SubscribeToEvents();
        SyncShellToRuntime();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    public bool OpenHome()
    {
        return OpenScreen(AppScreen.Home, false);
    }

    public bool OpenSkills()
    {
        return OpenScreen(AppScreen.Skills, false);
    }

    public bool OpenMissions()
    {
        return OpenScreen(AppScreen.Missions, false);
    }

    public bool OpenShop()
    {
        return OpenScreen(AppScreen.Shop, false);
    }

    public bool OpenRoom()
    {
        return OpenScreen(AppScreen.Room, false);
    }

    public bool OpenFocus(string preselectedSkillId = null)
    {
        if (gameManager == null || focusPanelUI == null || IsBlockingOverlayActive())
        {
            return false;
        }

        focusOriginScreen = GetCurrentSectionContext();
        currentScreen = AppScreen.Focus;
        UpdateShellVisuals();
        UpdateNavigationState();
        focusPanelUI.OpenPanel(preselectedSkillId);
        return true;
    }

    private bool OpenScreen(AppScreen targetScreen, bool force)
    {
        if (!force && IsNavigationBlocked())
        {
            return false;
        }

        currentScreen = targetScreen;
        ApplySectionVisibility();
        UpdateShellVisuals();
        UpdateNavigationState();
        return true;
    }

    private void ApplySectionVisibility()
    {
        if (skillsPanelUI != null)
        {
            if (currentScreen == AppScreen.Skills)
            {
                skillsPanelUI.ShowPanel();
            }
            else
            {
                skillsPanelUI.HidePanel();
            }
        }

        if (missionPanelUI != null)
        {
            if (currentScreen == AppScreen.Missions)
            {
                missionPanelUI.ShowPanel();
            }
            else
            {
                missionPanelUI.HidePanel();
            }
        }
    }

    private void SyncShellToRuntime()
    {
        if (gameManager != null)
        {
            PetStatusSummary summary = gameManager.GetPetStatusSummary();
            if (summary.flowState == PetFlowState.Dead)
            {
                ForceHomeAndClearTransient();
                return;
            }
        }

        if (focusPanelUI != null && focusPanelUI.IsPanelVisible())
        {
            currentScreen = AppScreen.Focus;
            UpdateShellVisuals();
            UpdateNavigationState();
            return;
        }

        if (skillsPanelUI != null && skillsPanelUI.IsPanelVisible())
        {
            currentScreen = AppScreen.Skills;
        }
        else if (missionPanelUI != null && missionPanelUI.IsPanelVisible())
        {
            currentScreen = AppScreen.Missions;
        }
        else
        {
            currentScreen = AppScreen.Home;
        }

        ApplySectionVisibility();
        UpdateShellVisuals();
        UpdateNavigationState();
    }

    private void ForceHomeAndClearTransient()
    {
        currentScreen = AppScreen.Home;
        focusOriginScreen = AppScreen.Home;

        if (focusPanelUI != null && focusPanelUI.IsPanelVisible())
        {
            focusPanelUI.ForceClosePanel(false);
        }

        ApplySectionVisibility();
        UpdateShellVisuals();
        UpdateNavigationState();
    }

    private void HandlePetFlowChanged()
    {
        if (gameManager == null)
        {
            return;
        }

        PetStatusSummary summary = gameManager.GetPetStatusSummary();
        if (summary.flowState == PetFlowState.Dead || summary.flowState == PetFlowState.Revived)
        {
            ForceHomeAndClearTransient();
            return;
        }

        UpdateNavigationState();
        UpdateShellVisuals();
    }

    private void HandleFocusPanelOpened()
    {
        currentScreen = AppScreen.Focus;
        UpdateShellVisuals();
        UpdateNavigationState();
    }

    private void HandleFocusPanelClosed(bool returnedFromResult)
    {
        AppScreen returnScreen = returnedFromResult ? AppScreen.Home : GetReturnScreen();
        currentScreen = returnScreen;
        ApplySectionVisibility();
        UpdateShellVisuals();
        UpdateNavigationState();
    }

    private AppScreen GetReturnScreen()
    {
        switch (focusOriginScreen)
        {
            case AppScreen.Skills:
            case AppScreen.Missions:
            case AppScreen.Shop:
            case AppScreen.Room:
            case AppScreen.Home:
                return focusOriginScreen;
            default:
                return AppScreen.Home;
        }
    }

    private AppScreen GetCurrentSectionContext()
    {
        switch (currentScreen)
        {
            case AppScreen.Skills:
            case AppScreen.Missions:
            case AppScreen.Shop:
            case AppScreen.Room:
            case AppScreen.Home:
                return currentScreen;
            default:
                return AppScreen.Home;
        }
    }

    private bool IsNavigationBlocked()
    {
        return IsBlockingOverlayActive() || (focusPanelUI != null && focusPanelUI.IsPanelVisible());
    }

    private bool IsBlockingOverlayActive()
    {
        if (gameManager == null)
        {
            return false;
        }

        return gameManager.GetPetStatusSummary().flowState == PetFlowState.Dead;
    }

    private void SubscribeToEvents()
    {
        if (gameManager != null)
        {
            gameManager.OnPetFlowChanged -= HandlePetFlowChanged;
            gameManager.OnFocusSessionChanged -= SyncShellToRuntime;
            gameManager.OnPetFlowChanged += HandlePetFlowChanged;
            gameManager.OnFocusSessionChanged += SyncShellToRuntime;
        }

        if (focusPanelUI != null)
        {
            focusPanelUI.OnPanelOpened -= HandleFocusPanelOpened;
            focusPanelUI.OnPanelClosed -= HandleFocusPanelClosed;
            focusPanelUI.OnPanelOpened += HandleFocusPanelOpened;
            focusPanelUI.OnPanelClosed += HandleFocusPanelClosed;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (gameManager != null)
        {
            gameManager.OnPetFlowChanged -= HandlePetFlowChanged;
            gameManager.OnFocusSessionChanged -= SyncShellToRuntime;
        }

        if (focusPanelUI != null)
        {
            focusPanelUI.OnPanelOpened -= HandleFocusPanelOpened;
            focusPanelUI.OnPanelClosed -= HandleFocusPanelClosed;
        }
    }

    private void WireSectionButtons()
    {
        if (wiredSectionButtons)
        {
            return;
        }

        if (skillsPanelUI != null)
        {
            if (skillsPanelUI.openButton != null)
            {
                skillsPanelUI.openButton.onClick.RemoveAllListeners();
                skillsPanelUI.openButton.onClick.AddListener(() => { OpenSkills(); });
            }
        }

        if (missionPanelUI != null)
        {
            if (missionPanelUI.closeButton != null)
            {
                missionPanelUI.closeButton.onClick.RemoveAllListeners();
                missionPanelUI.closeButton.onClick.AddListener(() => { OpenHome(); });
            }
        }

        wiredSectionButtons = true;
    }

    private void BuildShellIfNeeded()
    {
        if (shellRoot != null)
        {
            return;
        }

        RectTransform canvasRect = transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        shellRuntimeRoot = GetOrCreateShellRoot(canvasRect);
        shellRoot = CreatePanel(shellRuntimeRoot != null ? shellRuntimeRoot : canvasRect, "AppShellRoot", new Color(0.05f, 0.07f, 0.1f, 0.9f));
        RectTransform shellRect = shellRoot.GetComponent<RectTransform>();
        shellRect.anchorMin = new Vector2(0f, 0f);
        shellRect.anchorMax = new Vector2(1f, 0f);
        shellRect.pivot = new Vector2(0.5f, 0f);
        shellRect.sizeDelta = new Vector2(0f, 146f);
        shellRect.anchoredPosition = Vector2.zero;
        shellRoot.transform.SetAsLastSibling();

        VerticalLayoutGroup shellLayout = shellRoot.AddComponent<VerticalLayoutGroup>();
        shellLayout.padding = new RectOffset(24, 24, 16, 16);
        shellLayout.spacing = 10f;
        shellLayout.childAlignment = TextAnchor.MiddleCenter;
        shellLayout.childControlHeight = false;
        shellLayout.childControlWidth = true;
        shellLayout.childForceExpandHeight = false;
        shellLayout.childForceExpandWidth = true;

        currentContextText = CreateText(shellRoot.transform as RectTransform, "CurrentContextText", "Home", 24f, FontStyles.Bold);
        currentContextText.alignment = TextAlignmentOptions.Center;

        shellHintText = CreateText(shellRoot.transform as RectTransform, "ShellHintText", string.Empty, 18f, FontStyles.Normal);
        shellHintText.alignment = TextAlignmentOptions.Center;
        shellHintText.color = new Color(0.78f, 0.86f, 0.96f, 0.9f);

        GameObject buttonRow = CreateObject("ShellButtonRow", shellRoot.transform as RectTransform);
        HorizontalLayoutGroup rowLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = false;
        LayoutElement rowLayoutElement = buttonRow.AddComponent<LayoutElement>();
        rowLayoutElement.preferredHeight = 58f;

        homeButton = CreateNavButton(buttonRow.transform as RectTransform, "HomeButton", "Home", () => { OpenHome(); });
        skillsButton = CreateNavButton(buttonRow.transform as RectTransform, "SkillsButton", "Skills", () => { OpenSkills(); });
        missionsButton = CreateNavButton(buttonRow.transform as RectTransform, "MissionsButton", "Missions", () => { OpenMissions(); });
        shopButton = CreateNavButton(buttonRow.transform as RectTransform, "ShopButton", "Shop", () => { OpenShop(); });
        roomButton = CreateNavButton(buttonRow.transform as RectTransform, "RoomButton", "Room", () => { OpenRoom(); });
    }

    private RectTransform GetOrCreateShellRoot(RectTransform canvasRect)
    {
        if (canvasRect == null)
        {
            return null;
        }

        Transform existing = canvasRect.Find("ShellRuntimeRoot");
        RectTransform root;
        if (existing != null)
        {
            root = existing as RectTransform;
        }
        else
        {
            GameObject rootObject = new GameObject("ShellRuntimeRoot", typeof(RectTransform));
            root = rootObject.GetComponent<RectTransform>();
            root.SetParent(canvasRect, false);
            root.localScale = Vector3.one;
        }

        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        return root;
    }

    private void UpdateShellVisuals()
    {
        if (currentContextText != null)
        {
            currentContextText.text = GetContextLabel();
        }

        if (shellHintText != null)
        {
            shellHintText.text = GetContextHint();
            shellHintText.gameObject.SetActive(!string.IsNullOrEmpty(shellHintText.text));
        }

        UpdateButtonSelection(homeButton, currentScreen == AppScreen.Home);
        UpdateButtonSelection(skillsButton, currentScreen == AppScreen.Skills);
        UpdateButtonSelection(missionsButton, currentScreen == AppScreen.Missions);
        UpdateButtonSelection(shopButton, currentScreen == AppScreen.Shop);
        UpdateButtonSelection(roomButton, currentScreen == AppScreen.Room);
    }

    private void UpdateNavigationState()
    {
        bool blockedByDead = IsBlockingOverlayActive();
        bool blockedByFocus = focusPanelUI != null && focusPanelUI.IsPanelVisible();

        SetButtonInteractable(homeButton, !blockedByFocus);
        SetButtonInteractable(skillsButton, !blockedByDead && !blockedByFocus);
        SetButtonInteractable(missionsButton, !blockedByDead && !blockedByFocus);
        SetButtonInteractable(shopButton, !blockedByDead && !blockedByFocus);
        SetButtonInteractable(roomButton, !blockedByDead && !blockedByFocus);

    }

    private string GetContextLabel()
    {
        switch (currentScreen)
        {
            case AppScreen.Skills:
                return "Skills";
            case AppScreen.Missions:
                return "Missions";
            case AppScreen.Shop:
                return "Shop";
            case AppScreen.Room:
                return "Room";
            case AppScreen.Focus:
                return "Focus";
            default:
                return "Home";
        }
    }

    private string GetContextHint()
    {
        switch (currentScreen)
        {
            case AppScreen.Shop:
                return "Shop actions stay on Home in this build.";
            case AppScreen.Room:
                return "Room upgrades stay on Home in this build.";
            case AppScreen.Focus:
                return "Complete or close focus to return to the previous context.";
            default:
                return string.Empty;
        }
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

    private GameObject CreateObject(string name, RectTransform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return obj;
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string name, string value, float fontSize, FontStyles fontStyle)
    {
        GameObject textObject = CreateObject(name, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = new Color(0.94f, 0.97f, 1f, 1f);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private Button CreateNavButton(RectTransform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreatePanel(parent, name, new Color(0.16f, 0.21f, 0.29f, 1f));
        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 54f;
        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateText(buttonObject.transform as RectTransform, "Label", label, 20f, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.Center;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return button;
    }

    private void UpdateButtonSelection(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = selected
                ? new Color(0.32f, 0.59f, 0.94f, 1f)
                : new Color(0.16f, 0.21f, 0.29f, 1f);
        }
    }

    private void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }
}
