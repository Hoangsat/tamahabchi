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

    private readonly List<MissionPanelSectionHeaderRefs> pooledSectionHeaders = new List<MissionPanelSectionHeaderRefs>();
    private readonly List<MissionRowUI> pooledMissionRows = new List<MissionRowUI>();
    private readonly List<Button> popupSkillButtons = new List<Button>();
    private readonly List<Button> popupRoutineSkillButtons = new List<Button>();

    private GameManager gameManager;
    private MissionPanelCoordinator panelCoordinator;

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
    private Slider popupRoutineMoodSlider;
    private Slider popupRoutineEnergySlider;
    private Slider popupRoutineSkillSlider;
    private TextMeshProUGUI popupRoutineCostText;
    private bool popupSkillModeActive = true;
    private string popupSelectedSkillId = string.Empty;
    private string popupRoutineSkillRewardId = string.Empty;
    private bool popupInputEventsBound;
    private MissionPanelBonusCardRefs bonusCard;

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
        panelCoordinator = gameManager != null ? new MissionPanelCoordinator(gameManager) : null;
        AttachEvents();
        RefreshUI();
    }

    public void ShowPanel()
    {
        BuildUiIfNeeded();
        SetStatus(string.Empty);
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(false);
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
        if (panelRoot != null && !panelRoot.activeSelf)
        {
            return;
        }

        BuildUiIfNeeded();
        ApplyScreenVisuals();

        if (gameManager == null)
        {
            SetStatus("GameManager missing");
            return;
        }

        MissionPanelViewState viewState = panelCoordinator != null
            ? panelCoordinator.GetViewState()
            : new MissionPanelViewState("Missions", string.Empty, string.Empty, new List<MissionEntryData>(), new List<MissionEntryData>(), new MissionBonusStatus());

        titleText.text = viewState.Title;
        resetInfoText.text = viewState.ResetInfo;

        if (headerStatsText != null)
        {
            headerStatsText.text = viewState.HeaderStats;
        }

        RebuildContent(viewState.SkillMissions, viewState.Routines, viewState.Bonus);
        RefreshPopupLists();
        RebuildLayouts();
    }

    private void RebuildContent(List<MissionEntryData> skillMissions, List<MissionEntryData> routines, MissionBonusStatus bonus)
    {
        int sectionIndex = 0;
        int rowIndex = 0;
        int siblingIndex = 0;

        AddSectionHeader(sectionIndex++, siblingIndex++, "Skill Missions", "Choose up to five missions to track through focus sessions.");
        for (int i = 0; i < skillMissions.Count; i++)
        {
            MissionRowUI row = MissionPanelViewUtility.EnsureCached(pooledMissionRows, rowIndex++, CreateMissionRow);
            MissionPanelViewUtility.PrepareContentRoot(row.gameObject, siblingIndex++);
            row.BindSkillMission(skillMissions[i], HandleSkillToggle, HandleSkillClaim);
        }

        AddBonusCard(bonus, siblingIndex++);
        AddSectionHeader(sectionIndex++, siblingIndex++, "Routines", "Quick repeatable actions with instant rewards.");
        for (int i = 0; i < routines.Count; i++)
        {
            MissionRowUI row = MissionPanelViewUtility.EnsureCached(pooledMissionRows, rowIndex++, CreateMissionRow);
            MissionPanelViewUtility.PrepareContentRoot(row.gameObject, siblingIndex++);
            row.BindRoutine(routines[i], HandleRoutineComplete);
        }

        MissionPanelViewUtility.HideUnused(pooledMissionRows, rowIndex);
        HideUnusedSectionHeaders(sectionIndex);

        if (emptyStateText != null)
        {
            bool hasContent = skillMissions.Count > 0 || routines.Count > 0;
            emptyStateText.gameObject.SetActive(!hasContent);
            emptyStateText.text = MissionPanelPresenter.GetEmptyStateText();
        }
    }

    private void HandleSkillToggle(string missionId, bool shouldSelect)
    {
        if (panelCoordinator == null)
        {
            return;
        }

        MissionPanelActionResult result = panelCoordinator.ToggleSkillTracking(missionId, shouldSelect);
        SetStatus(result.Message);
        if (result.Success)
        {
            RefreshUI();
        }
    }

    private void HandleSkillClaim(string missionId)
    {
        if (panelCoordinator == null)
        {
            return;
        }

        MissionPanelActionResult result = panelCoordinator.ClaimSkillMission(missionId);
        SetStatus(result.Message);
        if (result.Success)
        {
            RefreshUI();
        }
    }

    private void HandleRoutineComplete(string missionId)
    {
        if (panelCoordinator == null)
        {
            return;
        }

        MissionPanelActionResult result = panelCoordinator.CompleteRoutine(missionId);
        SetStatus(result.Message);
        if (result.Success)
        {
            RefreshUI();
        }
    }

    private void HandleClaimBonus()
    {
        if (panelCoordinator == null)
        {
            return;
        }

        MissionPanelActionResult result = panelCoordinator.ClaimBonus();
        SetStatus(result.Message);
        if (result.Success)
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
        if (popupRoutineMoodSlider != null) popupRoutineMoodSlider.value = 3f;
        if (popupRoutineEnergySlider != null) popupRoutineEnergySlider.value = 3f;
        if (popupRoutineSkillSlider != null) popupRoutineSkillSlider.value = 0f;

        SwitchPopupMode(true);
        RefreshPopupLists();
        RefreshPopupPresentation();
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
        if (panelCoordinator == null)
        {
            SetPopupStatus("Mission system unavailable");
            return;
        }

        MissionPanelActionResult result;

        if (popupSkillModeActive)
        {
            int minutes = popupSkillMinutesSlider != null ? Mathf.RoundToInt(popupSkillMinutesSlider.value) : 30;
            result = panelCoordinator.CreateSkillMission(popupSelectedSkillId, minutes);
        }
        else
        {
            string title = popupRoutineTitleInput != null ? popupRoutineTitleInput.text : string.Empty;
            int coins = popupRoutineCoinsSlider != null ? Mathf.RoundToInt(popupRoutineCoinsSlider.value) : 0;
            int mood = popupRoutineMoodSlider != null ? Mathf.RoundToInt(popupRoutineMoodSlider.value) : 0;
            int energy = popupRoutineEnergySlider != null ? Mathf.RoundToInt(popupRoutineEnergySlider.value) : 0;
            int skillSP = popupRoutineSkillSlider != null ? Mathf.RoundToInt(popupRoutineSkillSlider.value) : 0;
            result = panelCoordinator.CreateRoutineMission(title, coins, mood, energy, skillSP, popupRoutineSkillRewardId);
        }

        if (!result.Success)
        {
            SetPopupStatus(result.Message);
            return;
        }

        HandleCloseCreate();
        SetStatus(result.Message);
        RefreshUI();
    }

    private void HandleReroll()
    {
        SetStatus(MissionPanelPresenter.GetRerollStatusText());
    }

    private void SwitchPopupMode(bool skillMode)
    {
        popupSkillModeActive = skillMode;
        if (popupSkillRoot != null) popupSkillRoot.SetActive(skillMode);
        if (popupRoutineRoot != null) popupRoutineRoot.SetActive(!skillMode);
        RefreshPopupPresentation();
    }

    private void RefreshPopupLists()
    {
        if (panelCoordinator == null || popupSkillListContent == null || popupRoutineSkillListContent == null)
        {
            return;
        }

        List<SkillEntry> skills = panelCoordinator.GetSkills();
        if (string.IsNullOrEmpty(popupSelectedSkillId) && skills.Count > 0)
        {
            popupSelectedSkillId = skills[0].id;
        }

        RebuildSkillChoiceList(
            popupSkillListContent,
            popupSkillButtons,
            skills,
            popupSelectedSkillId,
            skill => panelCoordinator.GetSkillChoiceLabel(skill),
            id =>
            {
                popupSelectedSkillId = id;
                RefreshPopupLists();
            });
        Button noneButton = MissionPanelViewUtility.EnsureCached(
            popupRoutineSkillButtons,
            0,
            index => MissionPanelViewUtility.CreateActionButton(popupRoutineSkillListContent, $"NoSkillRewardButton_{index}", string.Empty, null));
        MissionPanelViewUtility.ConfigureChoiceButton(
            noneButton,
            "No skill reward",
            () =>
            {
                popupRoutineSkillRewardId = string.Empty;
                RefreshPopupLists();
            },
            string.IsNullOrEmpty(popupRoutineSkillRewardId),
            0);

        int usedRoutineButtons = 1;
        for (int i = 0; i < skills.Count; i++)
        {
            SkillEntry skill = skills[i];
            Button skillButton = MissionPanelViewUtility.EnsureCached(
                popupRoutineSkillButtons,
                usedRoutineButtons,
                index => MissionPanelViewUtility.CreateActionButton(popupRoutineSkillListContent, $"RoutineSkill_{index}", string.Empty, null));
            MissionPanelViewUtility.ConfigureChoiceButton(
                skillButton,
                skill.name,
                () =>
                {
                    popupRoutineSkillRewardId = skill.id;
                    RefreshPopupLists();
                },
                popupRoutineSkillRewardId == skill.id,
                usedRoutineButtons);
            usedRoutineButtons++;
        }

        MissionPanelViewUtility.HideUnused(popupRoutineSkillButtons, usedRoutineButtons);

        if (popupRoutineCostText != null)
        {
            int cost = panelCoordinator.GetRoutineCreationCost();
            popupRoutineCostText.text = MissionPanelPresenter.BuildRoutineCostText(cost);
        }

        RefreshPopupPresentation();
    }

    private void RebuildSkillChoiceList(RectTransform root, List<Button> cache, List<SkillEntry> skills, string selectedId, Func<SkillEntry, string> labelFactory, Action<string> onSelect)
    {
        for (int i = 0; i < skills.Count; i++)
        {
            SkillEntry skill = skills[i];
            Button button = MissionPanelViewUtility.EnsureCached(
                cache,
                i,
                index => MissionPanelViewUtility.CreateActionButton(root, $"SkillChoice_{index}", string.Empty, null));
            MissionPanelViewUtility.ConfigureChoiceButton(button, labelFactory(skill), () => onSelect?.Invoke(skill.id), selectedId == skill.id, i);
        }

        MissionPanelViewUtility.HideUnused(cache, skills.Count);
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
        MissionPanelLayoutRefs layoutRefs = MissionPanelLayoutBuilder.Build(panelRoot.transform as RectTransform, ClosePanel, HandleOpenCreate, HandleReroll);
        screenRoot = layoutRefs.ScreenRoot;
        titleText = layoutRefs.TitleText;
        closeButton = layoutRefs.CloseButton;
        resetInfoText = layoutRefs.ResetInfoText;
        headerStatsText = layoutRefs.HeaderStatsText;
        panelStatusText = layoutRefs.PanelStatusText;
        scrollContent = layoutRefs.ScrollContent;
        missionListContainer = scrollContent;
        emptyStateText = layoutRefs.EmptyStateText;
        footerCreateButton = layoutRefs.FooterCreateButton;
        footerRerollButton = layoutRefs.FooterRerollButton;

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

        MissionPanelPopupRefs popupRefs = MissionPanelPopupBuilder.Build(
            panelRoot.transform as RectTransform,
            HandleCloseCreate,
            HandleConfirmCreate,
            () => SwitchPopupMode(true),
            () => SwitchPopupMode(false));

        createPopupRoot = popupRefs.Root;
        popupTitleText = popupRefs.TitleText;
        popupStatusText = popupRefs.StatusText;
        popupSkillRoot = popupRefs.SkillRoot;
        popupRoutineRoot = popupRefs.RoutineRoot;
        popupSkillListContent = popupRefs.SkillListContent;
        popupRoutineSkillListContent = popupRefs.RoutineSkillListContent;
        popupSkillMinutesSlider = popupRefs.SkillMinutesSlider;
        popupSkillMinutesValueText = popupRefs.SkillMinutesValueText;
        popupRoutineTitleInput = popupRefs.RoutineTitleInput;
        popupRoutineCoinsSlider = popupRefs.RoutineCoinsSlider;
        popupRoutineMoodSlider = popupRefs.RoutineMoodSlider;
        popupRoutineEnergySlider = popupRefs.RoutineEnergySlider;
        popupRoutineSkillSlider = popupRefs.RoutineSkillSlider;
        popupRoutineCostText = popupRefs.RoutineCostText;
        BindPopupInputEvents();
    }

    private MissionPanelSectionHeaderRefs GetOrCreateSectionHeader(int index)
    {
        while (pooledSectionHeaders.Count <= index)
        {
            pooledSectionHeaders.Add(MissionPanelViewUtility.CreateSectionHeader(scrollContent, $"Section_{pooledSectionHeaders.Count}"));
        }

        return pooledSectionHeaders[index];
    }

    private MissionPanelBonusCardRefs GetOrCreateBonusCard()
    {
        if (bonusCard != null && bonusCard.root != null)
        {
            return bonusCard;
        }

        bonusCard = MissionPanelViewUtility.CreateBonusCard(scrollContent, HandleClaimBonus);
        return bonusCard;
    }

    private void HideUnusedSectionHeaders(int usedCount)
    {
        for (int i = usedCount; i < pooledSectionHeaders.Count; i++)
        {
            if (pooledSectionHeaders[i] != null && pooledSectionHeaders[i].root != null)
            {
                pooledSectionHeaders[i].root.SetActive(false);
            }
        }
    }

    private void AddSectionHeader(int sectionIndex, int siblingIndex, string title, string subtitle)
    {
        MissionPanelSectionHeaderRefs header = GetOrCreateSectionHeader(sectionIndex);
        MissionPanelViewUtility.PrepareContentRoot(header.root, siblingIndex);
        header.titleText.text = title;
        header.subtitleText.text = subtitle;
    }

    private void AddBonusCard(MissionBonusStatus bonus, int siblingIndex)
    {
        MissionPanelBonusCardRefs bonusRefs = GetOrCreateBonusCard();
        MissionPanelBonusCardViewData bonusView = MissionPanelPresenter.BuildBonusCard(bonus);
        MissionPanelViewUtility.PrepareContentRoot(bonusRefs.root, siblingIndex);
        bonusRefs.titleText.text = bonusView.TitleText;
        bonusRefs.progressText.text = bonusView.ProgressText;
        bonusRefs.progressText.color = bonusView.ProgressColor;
        bonusRefs.claimButtonText.text = bonusView.ClaimButtonText;
        bonusRefs.claimButton.interactable = bonusView.CanClaim;
        bonusRefs.claimButton.onClick.RemoveAllListeners();
        bonusRefs.claimButton.onClick.AddListener(HandleClaimBonus);
    }

    private MissionRowUI CreateMissionRow(int index)
    {
        GameObject root = new GameObject($"MissionRow_{index}", typeof(RectTransform), typeof(Image), typeof(MissionRowUI), typeof(LayoutElement));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(scrollContent, false);
        rect.localScale = Vector3.one;
        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.minHeight = 250f;
        layout.preferredHeight = -1f;
        layout.flexibleHeight = 0f;
        root.SetActive(false);
        return root.GetComponent<MissionRowUI>();
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

    private void RefreshPopupPresentation()
    {
        if (popupTitleText == null || popupStatusText == null)
        {
            return;
        }

        MissionPanelPopupDraftViewData view;
        if (popupSkillModeActive)
        {
            string selectedSkillLabel = GetPopupSelectedSkillName();
            int minutes = popupSkillMinutesSlider != null ? Mathf.RoundToInt(popupSkillMinutesSlider.value) : 30;
            view = MissionPanelPresenter.BuildSkillMissionDraft(selectedSkillLabel, minutes);
        }
        else
        {
            string routineTitle = popupRoutineTitleInput != null ? popupRoutineTitleInput.text : string.Empty;
            int coins = popupRoutineCoinsSlider != null ? Mathf.RoundToInt(popupRoutineCoinsSlider.value) : 0;
            int mood = popupRoutineMoodSlider != null ? Mathf.RoundToInt(popupRoutineMoodSlider.value) : 0;
            int energy = popupRoutineEnergySlider != null ? Mathf.RoundToInt(popupRoutineEnergySlider.value) : 0;
            int skillSp = popupRoutineSkillSlider != null ? Mathf.RoundToInt(popupRoutineSkillSlider.value) : 0;
            int cost = panelCoordinator != null ? panelCoordinator.GetRoutineCreationCost() : 0;
            view = MissionPanelPresenter.BuildRoutineDraft(
                routineTitle,
                coins,
                mood,
                energy,
                skillSp,
                GetPopupRoutineRewardSkillName(),
                cost);
        }

        popupTitleText.text = view.TitleText;
        popupStatusText.text = view.StatusText;
        popupStatusText.color = view.StatusColor;
    }

    private string GetPopupSelectedSkillName()
    {
        if (panelCoordinator == null || string.IsNullOrEmpty(popupSelectedSkillId))
        {
            return string.Empty;
        }

        List<SkillEntry> skills = panelCoordinator.GetSkills();
        for (int i = 0; i < skills.Count; i++)
        {
            SkillEntry skill = skills[i];
            if (skill != null && skill.id == popupSelectedSkillId)
            {
                return skill.name ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private string GetPopupRoutineRewardSkillName()
    {
        if (panelCoordinator == null || string.IsNullOrEmpty(popupRoutineSkillRewardId))
        {
            return string.Empty;
        }

        List<SkillEntry> skills = panelCoordinator.GetSkills();
        for (int i = 0; i < skills.Count; i++)
        {
            SkillEntry skill = skills[i];
            if (skill != null && skill.id == popupRoutineSkillRewardId)
            {
                return skill.name ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private void BindPopupInputEvents()
    {
        if (popupInputEventsBound)
        {
            return;
        }

        if (popupSkillMinutesSlider != null)
        {
            popupSkillMinutesSlider.onValueChanged.AddListener(_ => RefreshPopupPresentation());
        }

        if (popupRoutineTitleInput != null)
        {
            popupRoutineTitleInput.onValueChanged.AddListener(_ => RefreshPopupPresentation());
        }

        if (popupRoutineCoinsSlider != null)
        {
            popupRoutineCoinsSlider.onValueChanged.AddListener(_ => RefreshPopupPresentation());
        }

        if (popupRoutineMoodSlider != null)
        {
            popupRoutineMoodSlider.onValueChanged.AddListener(_ => RefreshPopupPresentation());
        }

        if (popupRoutineEnergySlider != null)
        {
            popupRoutineEnergySlider.onValueChanged.AddListener(_ => RefreshPopupPresentation());
        }

        if (popupRoutineSkillSlider != null)
        {
            popupRoutineSkillSlider.onValueChanged.AddListener(_ => RefreshPopupPresentation());
        }

        popupInputEventsBound = true;
    }

    private void RebuildLayouts()
    {
        if (screenRoot == null)
        {
            return;
        }

        LayoutRebuilder.MarkLayoutForRebuild(screenRoot);

        if (createPopupRoot != null && createPopupRoot.activeInHierarchy)
        {
            RectTransform popupRect = createPopupRoot.transform as RectTransform;
            if (popupRect != null)
            {
                LayoutRebuilder.MarkLayoutForRebuild(popupRect);
            }
        }
    }
}
