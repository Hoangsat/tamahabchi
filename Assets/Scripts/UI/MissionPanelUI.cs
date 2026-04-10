using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionPanelUI : MonoBehaviour
{
    public GameObject panelRoot;
    public Button closeButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI resetInfoText;
    public TextMeshProUGUI emptyStateText;
    public TextMeshProUGUI panelStatusText;
    public RectTransform missionListContainer;
    public MissionRowUI missionRowTemplate;

    private readonly List<GameObject> spawnedContent = new List<GameObject>();
    private readonly List<Button> popupSkillButtons = new List<Button>();
    private readonly List<Button> popupRoutineSkillButtons = new List<Button>();

    private GameManager gameManager;

    private RectTransform screenRoot;
    private RectTransform scrollContent;
    private TextMeshProUGUI headerStatsText;
    private Button footerCreateButton;
    private Button footerRerollButton;

    private GameObject createPopupRoot;
    private TextMeshProUGUI popupTitleText;
    private TextMeshProUGUI popupStatusText;
    private GameObject popupSkillRoot;
    private GameObject popupRoutineRoot;
    private RectTransform popupSkillListContent;
    private RectTransform popupRoutineSkillListContent;
    private Slider popupSkillMinutesSlider;
    private TextMeshProUGUI popupSkillMinutesValueText;
    private TMP_InputField popupRoutineTitleInput;
    private Slider popupRoutineCoinsSlider;
    private Slider popupRoutineXpSlider;
    private Slider popupRoutineMoodSlider;
    private Slider popupRoutineEnergySlider;
    private Slider popupRoutineSkillSlider;
    private TextMeshProUGUI popupRoutineCostText;
    private bool popupSkillModeActive = true;
    private string popupSelectedSkillId = string.Empty;
    private string popupRoutineSkillRewardId = string.Empty;

    private void Awake()
    {
        BuildUiIfNeeded();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
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
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        RefreshUI();
    }

    public void HidePanel()
    {
        if (createPopupRoot != null)
        {
            createPopupRoot.SetActive(false);
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public bool IsPanelVisible()
    {
        return panelRoot != null && panelRoot.activeSelf;
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

        gameManager.OnMissionsChanged -= RefreshUI;
        gameManager.OnSkillsChanged -= RefreshUI;
        gameManager.OnMissionsChanged += RefreshUI;
        gameManager.OnSkillsChanged += RefreshUI;
    }

    private void DetachEvents()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnMissionsChanged -= RefreshUI;
        gameManager.OnSkillsChanged -= RefreshUI;
    }

    private void RefreshUI()
    {
        BuildUiIfNeeded();
        ApplyScreenVisuals();

        if (gameManager == null)
        {
            SetStatus("GameManager missing");
            return;
        }

        titleText.text = "Missions";
        resetInfoText.text = $"Reset in: {gameManager.GetMissionResetCountdownLabel()}";

        List<MissionEntryData> skillMissions = gameManager.GetSkillMissions();
        List<MissionEntryData> routines = gameManager.GetRoutineMissions();
        MissionBonusStatus bonus = gameManager.GetSkillMissionBonusStatus();

        if (headerStatsText != null)
        {
            headerStatsText.text = $"{bonus.completedSelectedSkillMissionCount}/{Mathf.Max(5, bonus.selectedSkillMissionCount)} tracked missions completed";
        }

        RebuildContent(skillMissions, routines, bonus);
        RefreshPopupLists();
        RebuildLayouts();
    }

    private void RebuildContent(List<MissionEntryData> skillMissions, List<MissionEntryData> routines, MissionBonusStatus bonus)
    {
        ClearSpawnedContent();

        AddSectionHeader("Skill Missions", "Choose up to five missions to track through focus sessions.");
        for (int i = 0; i < skillMissions.Count; i++)
        {
            MissionRowUI row = CreateMissionRow();
            row.BindSkillMission(skillMissions[i], HandleSkillToggle, HandleSkillClaim);
        }

        AddBonusCard(bonus);
        AddSectionHeader("Routines", "Quick repeatable actions with instant rewards.");
        for (int i = 0; i < routines.Count; i++)
        {
            MissionRowUI row = CreateMissionRow();
            row.BindRoutine(routines[i], HandleRoutineComplete);
        }

        if (emptyStateText != null)
        {
            bool hasContent = skillMissions.Count > 0 || routines.Count > 0;
            emptyStateText.gameObject.SetActive(!hasContent);
            emptyStateText.text = "No missions available";
        }
    }

    private void HandleSkillToggle(string missionId, bool shouldSelect)
    {
        if (gameManager == null)
        {
            return;
        }

        string message;
        bool success = shouldSelect
            ? gameManager.SelectMission(missionId, out message)
            : gameManager.UnselectMission(missionId, out message);

        SetStatus(success ? (shouldSelect ? "Mission tracking enabled" : "Mission tracking removed") : message);
        if (success)
        {
            RefreshUI();
        }
    }

    private void HandleSkillClaim(string missionId)
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnClaimMissionButton(missionId);
        SetStatus("Mission claimed");
        RefreshUI();
    }

    private void HandleRoutineComplete(string missionId)
    {
        if (gameManager == null)
        {
            return;
        }

        string message;
        bool success = gameManager.CompleteRoutineMission(missionId, out message);
        SetStatus(success ? "Routine completed" : message);
        if (success)
        {
            RefreshUI();
        }
    }

    private void HandleClaimBonus()
    {
        if (gameManager == null)
        {
            return;
        }

        string message;
        bool success = gameManager.ClaimSkillMissionBonus(out message);
        SetStatus(success ? "Bonus claimed" : message);
        if (success)
        {
            RefreshUI();
        }
    }

    private void HandleOpenCreate()
    {
        BuildPopupIfNeeded();
        popupSkillModeActive = true;
        popupSelectedSkillId = string.Empty;
        popupRoutineSkillRewardId = string.Empty;

        if (popupSkillMinutesSlider != null) popupSkillMinutesSlider.value = 30f;
        if (popupRoutineTitleInput != null) popupRoutineTitleInput.text = string.Empty;
        if (popupRoutineCoinsSlider != null) popupRoutineCoinsSlider.value = 20f;
        if (popupRoutineXpSlider != null) popupRoutineXpSlider.value = 10f;
        if (popupRoutineMoodSlider != null) popupRoutineMoodSlider.value = 3f;
        if (popupRoutineEnergySlider != null) popupRoutineEnergySlider.value = 3f;
        if (popupRoutineSkillSlider != null) popupRoutineSkillSlider.value = 0f;

        SwitchPopupMode(true);
        RefreshPopupLists();
        SetPopupStatus(string.Empty);
        createPopupRoot.SetActive(true);
    }

    private void HandleCloseCreate()
    {
        if (createPopupRoot != null)
        {
            createPopupRoot.SetActive(false);
        }
    }

    private void HandleConfirmCreate()
    {
        if (gameManager == null)
        {
            SetPopupStatus("Mission system unavailable");
            return;
        }

        string message;
        bool success;

        if (popupSkillModeActive)
        {
            int minutes = popupSkillMinutesSlider != null ? Mathf.RoundToInt(popupSkillMinutesSlider.value) : 30;
            success = gameManager.CreateCustomSkillMission(popupSelectedSkillId, minutes, out message);
        }
        else
        {
            string title = popupRoutineTitleInput != null ? popupRoutineTitleInput.text : string.Empty;
            int coins = popupRoutineCoinsSlider != null ? Mathf.RoundToInt(popupRoutineCoinsSlider.value) : 0;
            int xp = popupRoutineXpSlider != null ? Mathf.RoundToInt(popupRoutineXpSlider.value) : 0;
            int mood = popupRoutineMoodSlider != null ? Mathf.RoundToInt(popupRoutineMoodSlider.value) : 0;
            int energy = popupRoutineEnergySlider != null ? Mathf.RoundToInt(popupRoutineEnergySlider.value) : 0;
            float skillPercent = popupRoutineSkillSlider != null ? popupRoutineSkillSlider.value : 0f;
            success = gameManager.CreateRoutineMission(title, coins, xp, mood, energy, skillPercent, popupRoutineSkillRewardId, out message);
        }

        if (!success)
        {
            SetPopupStatus(message);
            return;
        }

        HandleCloseCreate();
        SetStatus("Mission created");
        RefreshUI();
    }

    private void HandleReroll()
    {
        SetStatus("Reroll stays out of scope for this milestone");
    }

    private void SwitchPopupMode(bool skillMode)
    {
        popupSkillModeActive = skillMode;
        if (popupSkillRoot != null) popupSkillRoot.SetActive(skillMode);
        if (popupRoutineRoot != null) popupRoutineRoot.SetActive(!skillMode);
        if (popupTitleText != null) popupTitleText.text = skillMode ? "Create Skill Mission" : "Create Routine";
    }

    private void RefreshPopupLists()
    {
        if (gameManager == null || popupSkillListContent == null || popupRoutineSkillListContent == null)
        {
            return;
        }

        List<SkillEntry> skills = gameManager.GetSkills();
        if (string.IsNullOrEmpty(popupSelectedSkillId) && skills.Count > 0)
        {
            popupSelectedSkillId = skills[0].id;
        }

        RebuildSkillChoiceList(
            popupSkillListContent,
            popupSkillButtons,
            skills,
            popupSelectedSkillId,
            skill => $"{skill.name}  {skill.percent:0.#}%",
            id =>
            {
                popupSelectedSkillId = id;
                RefreshPopupLists();
            });

        ClearChildren(popupRoutineSkillListContent);
        popupRoutineSkillButtons.Clear();

        Button noneButton = CreateActionButton(popupRoutineSkillListContent, "NoSkillRewardButton", "No skill reward", () =>
        {
            popupRoutineSkillRewardId = string.Empty;
            RefreshPopupLists();
        });
        StyleChoiceButton(noneButton, string.IsNullOrEmpty(popupRoutineSkillRewardId));
        popupRoutineSkillButtons.Add(noneButton);

        for (int i = 0; i < skills.Count; i++)
        {
            SkillEntry skill = skills[i];
            Button skillButton = CreateActionButton(popupRoutineSkillListContent, $"RoutineSkill_{i}", skill.name, () =>
            {
                popupRoutineSkillRewardId = skill.id;
                RefreshPopupLists();
            });
            StyleChoiceButton(skillButton, popupRoutineSkillRewardId == skill.id);
            popupRoutineSkillButtons.Add(skillButton);
        }

        if (popupRoutineCostText != null)
        {
            int cost = gameManager.GetRoutineCreationCost();
            popupRoutineCostText.text = cost > 0 ? $"Routine cost: {cost} coins" : "Routine cost: free";
        }
    }

    private void RebuildSkillChoiceList(RectTransform root, List<Button> cache, List<SkillEntry> skills, string selectedId, Func<SkillEntry, string> labelFactory, Action<string> onSelect)
    {
        ClearChildren(root);
        cache.Clear();
        for (int i = 0; i < skills.Count; i++)
        {
            SkillEntry skill = skills[i];
            Button button = CreateActionButton(root, $"SkillChoice_{i}", labelFactory(skill), () => onSelect?.Invoke(skill.id));
            StyleChoiceButton(button, selectedId == skill.id);
            cache.Add(button);
        }
    }

    private void BuildUiIfNeeded()
    {
        EnsurePanelRoot();
        EnsureScreenRoot();
        BuildPopupIfNeeded();
    }

    private void EnsurePanelRoot()
    {
        if (panelRoot == null)
        {
            panelRoot = new GameObject("MissionPanel", typeof(RectTransform), typeof(Image));
            RectTransform rect = panelRoot.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
        }

        RectTransform panelRect = panelRoot.transform as RectTransform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panelRoot.GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = panelRoot.AddComponent<Image>();
        }

        panelImage.color = new Color(0.02f, 0.04f, 0.08f, 0.94f);
    }

    private void EnsureScreenRoot()
    {
        if (screenRoot != null)
        {
            return;
        }

        HideLegacyChildren();

        GameObject root = CreateObject("MissionScreenRoot", panelRoot.transform as RectTransform);
        screenRoot = root.GetComponent<RectTransform>();
        screenRoot.anchorMin = Vector2.zero;
        screenRoot.anchorMax = Vector2.one;
        screenRoot.offsetMin = new Vector2(24f, 132f);
        screenRoot.offsetMax = new Vector2(-24f, -24f);
        screenRoot.pivot = new Vector2(0.5f, 0.5f);

        GameObject headerBlock = CreateObject("HeaderBlock", screenRoot);
        RectTransform headerRect = headerBlock.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.offsetMin = new Vector2(0f, -148f);
        headerRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup headerLayout = headerBlock.AddComponent<VerticalLayoutGroup>();
        headerLayout.spacing = 10f;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = true;
        headerLayout.childForceExpandHeight = false;
        ContentSizeFitter headerFitter = headerBlock.AddComponent<ContentSizeFitter>();
        headerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject topRow = CreateObject("HeaderTopRow", headerBlock.transform as RectTransform);
        HorizontalLayoutGroup topRowLayout = topRow.AddComponent<HorizontalLayoutGroup>();
        topRowLayout.spacing = 12f;
        topRowLayout.childControlWidth = true;
        topRowLayout.childControlHeight = true;
        topRowLayout.childForceExpandWidth = false;
        topRowLayout.childForceExpandHeight = false;
        topRowLayout.childAlignment = TextAnchor.MiddleCenter;
        LayoutElement topRowLayoutElement = topRow.AddComponent<LayoutElement>();
        topRowLayoutElement.preferredHeight = 56f;

        titleText = CreateText(topRow.transform as RectTransform, "TitleText", "Missions", 42f, FontStyles.Bold, TextAlignmentOptions.Left);
        LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.flexibleWidth = 1f;
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        titleText.overflowMode = TextOverflowModes.Overflow;

        closeButton = CreateActionButton(topRow.transform as RectTransform, "CloseButton", "Close", ClosePanel);
        ApplyButtonSizing(closeButton, 160f, 56f, false);

        resetInfoText = CreateText(headerBlock.transform as RectTransform, "ResetInfoText", string.Empty, 20f, FontStyles.Normal, TextAlignmentOptions.Left);
        headerStatsText = CreateText(headerBlock.transform as RectTransform, "HeaderStatsText", string.Empty, 18f, FontStyles.Bold, TextAlignmentOptions.Left);
        panelStatusText = CreateText(headerBlock.transform as RectTransform, "PanelStatusText", string.Empty, 18f, FontStyles.Normal, TextAlignmentOptions.Left);
        panelStatusText.color = new Color(0.84f, 0.9f, 0.98f, 0.9f);

        scrollContent = CreateScrollContent(screenRoot, "MissionScroll");
        RectTransform scrollRoot = scrollContent.parent != null ? scrollContent.parent.parent as RectTransform : null;
        if (scrollRoot != null)
        {
            scrollRoot.anchorMin = new Vector2(0f, 0f);
            scrollRoot.anchorMax = new Vector2(1f, 1f);
            scrollRoot.offsetMin = new Vector2(0f, 92f);
            scrollRoot.offsetMax = new Vector2(0f, -172f);
            scrollRoot.pivot = new Vector2(0.5f, 0.5f);
        }

        missionListContainer = scrollContent;
        emptyStateText = CreateText(screenRoot, "EmptyStateText", string.Empty, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform emptyStateRect = emptyStateText.rectTransform;
        emptyStateRect.anchorMin = new Vector2(0f, 0f);
        emptyStateRect.anchorMax = new Vector2(1f, 1f);
        emptyStateRect.offsetMin = new Vector2(48f, 120f);
        emptyStateRect.offsetMax = new Vector2(-48f, -188f);

        GameObject footer = CreateObject("FooterActions", screenRoot);
        RectTransform footerRect = footer.GetComponent<RectTransform>();
        footerRect.anchorMin = new Vector2(0f, 0f);
        footerRect.anchorMax = new Vector2(1f, 0f);
        footerRect.pivot = new Vector2(0.5f, 0f);
        footerRect.offsetMin = Vector2.zero;
        footerRect.offsetMax = new Vector2(0f, 72f);

        HorizontalLayoutGroup footerLayout = footer.AddComponent<HorizontalLayoutGroup>();
        footerLayout.spacing = 14f;
        footerLayout.childControlWidth = true;
        footerLayout.childControlHeight = true;
        footerLayout.childForceExpandWidth = true;
        footerLayout.childForceExpandHeight = false;

        footerCreateButton = CreateActionButton(footer.transform as RectTransform, "CreateButton", "Create", HandleOpenCreate);
        footerRerollButton = CreateActionButton(footer.transform as RectTransform, "RerollButton", "Reroll", HandleReroll);
        ApplyButtonSizing(footerCreateButton, 0f, 64f, true);
        ApplyButtonSizing(footerRerollButton, 0f, 64f, true);
        footerRerollButton.interactable = false;

        if (missionRowTemplate != null)
        {
            missionRowTemplate.gameObject.SetActive(false);
        }

        RebuildLayouts();
    }

    private void HideLegacyChildren()
    {
        if (panelRoot == null)
        {
            return;
        }

        for (int i = 0; i < panelRoot.transform.childCount; i++)
        {
            Transform child = panelRoot.transform.GetChild(i);
            if (child != null && child.name != "MissionScreenRoot" && child.name != "CreatePopupRoot")
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private void BuildPopupIfNeeded()
    {
        if (createPopupRoot != null || panelRoot == null)
        {
            return;
        }

        createPopupRoot = CreatePanel(panelRoot.transform as RectTransform, "CreatePopupRoot", new Color(0.01f, 0.02f, 0.04f, 0.82f), true);
        RectTransform popupOverlayRect = createPopupRoot.transform as RectTransform;
        popupOverlayRect.SetAsLastSibling();

        GameObject popupBox = CreatePanel(createPopupRoot.transform as RectTransform, "CreatePopupBox", new Color(0.12f, 0.16f, 0.24f, 1f), false);
        RectTransform popupBoxRect = popupBox.GetComponent<RectTransform>();
        popupBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupBoxRect.pivot = new Vector2(0.5f, 0.5f);
        popupBoxRect.sizeDelta = new Vector2(920f, 1440f);

        VerticalLayoutGroup popupLayout = popupBox.AddComponent<VerticalLayoutGroup>();
        popupLayout.padding = new RectOffset(28, 28, 28, 28);
        popupLayout.spacing = 18f;
        popupLayout.childControlWidth = true;
        popupLayout.childControlHeight = true;
        popupLayout.childForceExpandWidth = true;
        popupLayout.childForceExpandHeight = false;

        GameObject popupHeader = CreateObject("PopupHeader", popupBox.transform as RectTransform);
        VerticalLayoutGroup popupHeaderLayout = popupHeader.AddComponent<VerticalLayoutGroup>();
        popupHeaderLayout.spacing = 14f;
        popupHeaderLayout.childControlWidth = true;
        popupHeaderLayout.childControlHeight = true;
        popupHeaderLayout.childForceExpandWidth = true;
        popupHeaderLayout.childForceExpandHeight = false;
        ContentSizeFitter popupHeaderFitter = popupHeader.AddComponent<ContentSizeFitter>();
        popupHeaderFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject popupTitleRow = CreateObject("PopupTitleRow", popupHeader.transform as RectTransform);
        HorizontalLayoutGroup popupTitleRowLayout = popupTitleRow.AddComponent<HorizontalLayoutGroup>();
        popupTitleRowLayout.spacing = 12f;
        popupTitleRowLayout.childControlWidth = true;
        popupTitleRowLayout.childControlHeight = true;
        popupTitleRowLayout.childForceExpandWidth = false;
        popupTitleRowLayout.childForceExpandHeight = false;
        LayoutElement popupTitleRowElement = popupTitleRow.AddComponent<LayoutElement>();
        popupTitleRowElement.preferredHeight = 56f;

        popupTitleText = CreateText(popupTitleRow.transform as RectTransform, "PopupTitleText", "Create Skill Mission", 34f, FontStyles.Bold, TextAlignmentOptions.Left);
        LayoutElement popupTitleLayout = popupTitleText.gameObject.AddComponent<LayoutElement>();
        popupTitleLayout.flexibleWidth = 1f;
        popupTitleText.textWrappingMode = TextWrappingModes.NoWrap;
        popupTitleText.overflowMode = TextOverflowModes.Overflow;

        Button popupCloseButton = CreateActionButton(popupTitleRow.transform as RectTransform, "PopupCloseButton", "Close", HandleCloseCreate);
        ApplyButtonSizing(popupCloseButton, 170f, 56f, false);

        GameObject popupModeRow = CreateObject("PopupModeRow", popupHeader.transform as RectTransform);
        HorizontalLayoutGroup popupModeLayout = popupModeRow.AddComponent<HorizontalLayoutGroup>();
        popupModeLayout.spacing = 12f;
        popupModeLayout.childControlWidth = true;
        popupModeLayout.childControlHeight = true;
        popupModeLayout.childForceExpandWidth = true;
        popupModeLayout.childForceExpandHeight = false;

        Button popupSkillModeButton = CreateActionButton(popupModeRow.transform as RectTransform, "SkillModeButton", "Skill Mission", () => SwitchPopupMode(true));
        Button popupRoutineModeButton = CreateActionButton(popupModeRow.transform as RectTransform, "RoutineModeButton", "Routine", () => SwitchPopupMode(false));
        ApplyButtonSizing(popupSkillModeButton, 0f, 60f, true);
        ApplyButtonSizing(popupRoutineModeButton, 0f, 60f, true);

        popupStatusText = CreateText(popupBox.transform as RectTransform, "PopupStatusText", string.Empty, 18f, FontStyles.Normal, TextAlignmentOptions.Left);
        popupSkillRoot = CreateColumn(popupBox.transform as RectTransform, "PopupSkillRoot");
        popupRoutineRoot = CreateColumn(popupBox.transform as RectTransform, "PopupRoutineRoot");

        BuildSkillPopup(popupSkillRoot.transform as RectTransform);
        BuildRoutinePopup(popupRoutineRoot.transform as RectTransform);

        Button popupCreateButton = CreateActionButton(popupBox.transform as RectTransform, "PopupCreateButton", "Create", HandleConfirmCreate);
        ApplyButtonSizing(popupCreateButton, 0f, 64f, true);
        createPopupRoot.SetActive(false);
    }

    private void BuildSkillPopup(RectTransform parent)
    {
        CreateText(parent, "SkillHint", "Choose the skill and duration for this mission.", 22f, FontStyles.Normal, TextAlignmentOptions.Left);
        popupSkillListContent = CreateScrollContent(parent, "PopupSkillList");
        ApplyPreferredHeight(popupSkillListContent.parent as RectTransform, 260f);
        popupSkillMinutesValueText = CreateText(parent, "PopupSkillMinutesValue", "30 min", 24f, FontStyles.Bold, TextAlignmentOptions.Left);
        popupSkillMinutesSlider = CreateSlider(parent, "PopupSkillMinutesSlider", 15f, 120f, 30f, true, value =>
        {
            popupSkillMinutesValueText.text = $"{Mathf.RoundToInt(value)} min";
        });
    }

    private void BuildRoutinePopup(RectTransform parent)
    {
        CreateText(parent, "RoutineHint", "Create a routine with an instant reward bundle.", 22f, FontStyles.Normal, TextAlignmentOptions.Left);
        popupRoutineTitleInput = CreateInput(parent, "PopupRoutineTitleInput", "Routine name (max 30)");
        popupRoutineTitleInput.characterLimit = 30;
        popupRoutineCoinsSlider = CreateSliderWithLabel(parent, "PopupRoutineCoinsSlider", 0f, 100f, 20f, true, "Coins");
        popupRoutineXpSlider = CreateSliderWithLabel(parent, "PopupRoutineXpSlider", 0f, 80f, 10f, true, "XP");
        popupRoutineMoodSlider = CreateSliderWithLabel(parent, "PopupRoutineMoodSlider", 0f, 20f, 3f, true, "Mood");
        popupRoutineEnergySlider = CreateSliderWithLabel(parent, "PopupRoutineEnergySlider", 0f, 20f, 3f, true, "Energy");
        popupRoutineSkillSlider = CreateSliderWithLabel(parent, "PopupRoutineSkillSlider", 0f, 10f, 0f, false, "Skill %");
        popupRoutineCostText = CreateText(parent, "PopupRoutineCostText", string.Empty, 22f, FontStyles.Bold, TextAlignmentOptions.Left);
        CreateText(parent, "PopupRoutineSkillTargetLabel", "Skill reward target", 22f, FontStyles.Bold, TextAlignmentOptions.Left);
        popupRoutineSkillListContent = CreateScrollContent(parent, "PopupRoutineSkillList");
        ApplyPreferredHeight(popupRoutineSkillListContent.parent as RectTransform, 220f);
    }

    private void AddSectionHeader(string title, string subtitle)
    {
        GameObject card = CreatePanel(scrollContent, $"Section_{spawnedContent.Count}", new Color(0.09f, 0.13f, 0.2f, 0.96f), false);
        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 18, 18);
        layout.spacing = 6f;
        CreateText(card.transform as RectTransform, "Title", title, 30f, FontStyles.Bold, TextAlignmentOptions.Left);
        TextMeshProUGUI subtitleText = CreateText(card.transform as RectTransform, "Subtitle", subtitle, 18f, FontStyles.Normal, TextAlignmentOptions.Left);
        subtitleText.color = new Color(0.83f, 0.89f, 0.98f, 0.84f);
        spawnedContent.Add(card);
    }

    private void AddBonusCard(MissionBonusStatus bonus)
    {
        GameObject card = CreatePanel(scrollContent, $"Bonus_{spawnedContent.Count}", new Color(0.16f, 0.21f, 0.14f, 0.98f), false);
        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 10f;
        CreateText(card.transform as RectTransform, "Title", "5/5 Skill Mission Bonus", 28f, FontStyles.Bold, TextAlignmentOptions.Left);
        TextMeshProUGUI progressText = CreateText(card.transform as RectTransform, "Progress", $"{bonus.completedSelectedSkillMissionCount}/5 completed", 20f, FontStyles.Normal, TextAlignmentOptions.Left);
        progressText.color = bonus.isReady ? new Color(0.63f, 0.96f, 0.72f, 1f) : new Color(0.9f, 0.94f, 1f, 0.9f);
        Button claimButton = CreateActionButton(card.transform as RectTransform, "BonusClaimButton", bonus.isClaimed ? "Claimed" : "Claim Bonus", HandleClaimBonus);
        ApplyButtonSizing(claimButton, 0f, 60f, true);
        claimButton.interactable = bonus.isReady && !bonus.isClaimed;
        spawnedContent.Add(card);
    }

    private MissionRowUI CreateMissionRow()
    {
        GameObject root = new GameObject($"MissionRow_{spawnedContent.Count}", typeof(RectTransform), typeof(Image), typeof(MissionRowUI), typeof(LayoutElement));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(scrollContent, false);
        rect.localScale = Vector3.one;
        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.minHeight = 250f;
        layout.preferredHeight = -1f;
        layout.flexibleHeight = 0f;
        spawnedContent.Add(root);
        return root.GetComponent<MissionRowUI>();
    }

    private RectTransform CreateScrollContent(RectTransform parent, string name)
    {
        GameObject root = CreateObject(name, parent);
        LayoutElement rootLayout = root.AddComponent<LayoutElement>();
        rootLayout.flexibleWidth = 1f;
        rootLayout.flexibleHeight = 1f;
        rootLayout.minHeight = 400f;

        ScrollRect scrollRect = root.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        GameObject viewport = CreateObject("Viewport", root.transform as RectTransform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.AddComponent<RectMask2D>();

        GameObject content = CreateObject("Content", viewportRect);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 18f;
        contentLayout.padding = new RectOffset(0, 0, 0, 24);
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        return contentRect;
    }

    private GameObject CreateColumn(RectTransform parent, string name)
    {
        GameObject root = CreateObject(name, parent);
        VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 14f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = root.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return root;
    }

    private Button CreateActionButton(RectTransform parent, string name, string label, UnityEngine.Events.UnityAction action)
    {
        GameObject root = CreatePanel(parent, name, new Color(0.24f, 0.33f, 0.5f, 1f), false);
        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.preferredHeight = 60f;
        Button button = root.AddComponent<Button>();
        button.onClick.AddListener(action);

        TextMeshProUGUI text = CreateText(root.transform as RectTransform, "Label", label, 22f, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        return button;
    }

    private Slider CreateSliderWithLabel(RectTransform parent, string name, float min, float max, float value, bool wholeNumbers, string label)
    {
        TextMeshProUGUI valueText = CreateText(parent, $"{name}_Label", string.Empty, 20f, FontStyles.Bold, TextAlignmentOptions.Left);
        return CreateSlider(parent, name, min, max, value, wholeNumbers, sliderValue =>
        {
            string formatted = wholeNumbers ? Mathf.RoundToInt(sliderValue).ToString() : sliderValue.ToString("0.#");
            valueText.text = $"{label}: {formatted}";
        });
    }

    private Slider CreateSlider(RectTransform parent, string name, float min, float max, float value, bool wholeNumbers, UnityEngine.Events.UnityAction<float> onChanged)
    {
        GameObject root = CreatePanel(parent, name, new Color(0.08f, 0.11f, 0.17f, 0.98f), false);
        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.preferredHeight = 44f;
        Slider slider = root.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;
        slider.wholeNumbers = wholeNumbers;
        slider.onValueChanged.AddListener(onChanged);
        onChanged?.Invoke(value);
        return slider;
    }

    private TMP_InputField CreateInput(RectTransform parent, string name, string placeholderText)
    {
        GameObject root = CreatePanel(parent, name, new Color(0.08f, 0.11f, 0.17f, 0.98f), false);
        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.preferredHeight = 76f;
        TMP_InputField input = root.AddComponent<TMP_InputField>();

        GameObject viewport = CreateObject("Viewport", root.transform as RectTransform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(16f, 14f);
        viewportRect.offsetMax = new Vector2(-16f, -14f);
        viewport.AddComponent<RectMask2D>();

        TextMeshProUGUI text = CreateText(viewportRect, "Text", string.Empty, 24f, FontStyles.Normal, TextAlignmentOptions.Left);
        TextMeshProUGUI placeholder = CreateText(viewportRect, "Placeholder", placeholderText, 24f, FontStyles.Italic, TextAlignmentOptions.Left);
        placeholder.color = new Color(0.8f, 0.85f, 0.94f, 0.45f);

        input.textViewport = viewportRect;
        input.textComponent = text;
        input.placeholder = placeholder;
        return input;
    }

    private void StyleChoiceButton(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = selected ? new Color(0.28f, 0.56f, 0.92f, 1f) : new Color(0.17f, 0.24f, 0.36f, 1f);
        }
    }

    private void ApplyScreenVisuals()
    {
        if (panelRoot == null)
        {
            return;
        }

        Image panelImage = panelRoot.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = new Color(0.01f, 0.02f, 0.05f, 0.94f);
        }

        if (resetInfoText != null)
        {
            resetInfoText.color = new Color(0.84f, 0.89f, 0.98f, 0.88f);
        }

        if (headerStatsText != null)
        {
            headerStatsText.color = new Color(0.88f, 0.93f, 1f, 0.92f);
        }
    }

    private void ApplyButtonSizing(Button button, float preferredWidth, float preferredHeight, bool flexibleWidth)
    {
        if (button == null)
        {
            return;
        }

        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = button.gameObject.AddComponent<LayoutElement>();
        }

        if (preferredWidth > 0f)
        {
            layout.preferredWidth = preferredWidth;
            layout.minWidth = preferredWidth;
        }

        layout.preferredHeight = preferredHeight;
        layout.minHeight = preferredHeight;
        layout.flexibleWidth = flexibleWidth ? 1f : 0f;
    }

    private void ApplyPreferredHeight(RectTransform rect, float height)
    {
        if (rect == null)
        {
            return;
        }

        LayoutElement layout = rect.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = rect.gameObject.AddComponent<LayoutElement>();
        }

        layout.preferredHeight = height;
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string name, string value, float size, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject root = CreateObject(name, parent);
        TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = new Color(0.96f, 0.98f, 1f, 1f);
        text.textWrappingMode = TextWrappingModes.Normal;
        text.enableAutoSizing = false;
        return text;
    }

    private GameObject CreatePanel(RectTransform parent, string name, Color color, bool stretch)
    {
        GameObject root = CreateObject(name, parent);
        Image image = root.AddComponent<Image>();
        image.color = color;
        if (stretch)
        {
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        return root;
    }

    private GameObject CreateObject(string name, RectTransform parent)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        return root;
    }

    private void ClearSpawnedContent()
    {
        for (int i = 0; i < spawnedContent.Count; i++)
        {
            if (spawnedContent[i] != null)
            {
                Destroy(spawnedContent[i]);
            }
        }

        spawnedContent.Clear();
    }

    private void ClearChildren(RectTransform root)
    {
        if (root == null)
        {
            return;
        }

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Destroy(root.GetChild(i).gameObject);
        }
    }

    private void SetStatus(string message)
    {
        if (panelStatusText != null)
        {
            panelStatusText.text = message;
        }
    }

    private void SetPopupStatus(string message)
    {
        if (popupStatusText != null)
        {
            popupStatusText.text = message;
        }
    }

    private void RebuildLayouts()
    {
        if (screenRoot == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(screenRoot);

        if (scrollContent != null)
        {
            RectTransform scrollRoot = scrollContent.parent != null ? scrollContent.parent.parent as RectTransform : null;
            if (scrollRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRoot);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);
        }

        if (createPopupRoot != null && createPopupRoot.activeInHierarchy)
        {
            RectTransform popupRect = createPopupRoot.transform as RectTransform;
            if (popupRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(popupRect);
            }
        }
    }
}
