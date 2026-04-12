using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillsPanelUI : MonoBehaviour
{
    private const int MinimumSkillsForRadar = 3;
    private const int RadarChartSkillLimit = 12;

    public GameObject panelRoot;
    public Button openButton;
    public Button closeButton;
    public Image heroCardBackgroundImage;
    public Image heroIconBadgeImage;
    public TextMeshProUGUI heroSkillIconText;
    public TextMeshProUGUI heroSkillNameText;
    public TextMeshProUGUI heroSkillMetaText;
    public Image heroProgressFillImage;
    public TextMeshProUGUI heroProgressText;
    public TextMeshProUGUI heroHintText;
    public Button heroActionButton;
    public TextMeshProUGUI heroActionButtonText;
    public TextMeshProUGUI chartTitleText;
    public RadarChartGraphic radarChartGraphic;
    public RectTransform radarLabelsRoot;
    public TextMeshProUGUI radarLabelTemplate;
    public TextMeshProUGUI chartEmptyStateText;
    public TMP_InputField skillNameInput;
    public Button previousIconButton;
    public Button nextIconButton;
    public TextMeshProUGUI iconPreviewText;
    public Button addSkillButton;
    public TextMeshProUGUI addSkillButtonText;
    public TextMeshProUGUI panelStatusText;
    public TextMeshProUGUI emptyStateText;
    public TextMeshProUGUI panelSelectedSkillText;
    public TextMeshProUGUI focusSkillStatusText;
    public RectTransform skillsListContainer;
    public SkillRowUI skillRowTemplate;
    public GameObject skillGainPopupRoot;
    public CanvasGroup skillGainPopupCanvasGroup;
    public RectTransform skillGainPopupTransform;
    public TextMeshProUGUI skillGainPopupIconText;
    public TextMeshProUGUI skillGainPopupText;

    private readonly List<SkillRowUI> spawnedRows = new List<SkillRowUI>();
    private readonly List<TextMeshProUGUI> spawnedRadarLabels = new List<TextMeshProUGUI>();
    private GameManager gameManager;
    private SkillsPanelCoordinator panelCoordinator;
    private SkillsPanelPopupController popupController;
    private RectTransform archetypePickerRoot;
    private GridLayoutGroup archetypePickerGrid;
    private readonly List<SkillArchetypeCardUI> spawnedArchetypeCards = new List<SkillArchetypeCardUI>();
    private GameObject archetypeEditPopupRoot;
    private RectTransform archetypeEditPopupCardsRoot;
    private GridLayoutGroup archetypeEditPopupGrid;
    private TextMeshProUGUI archetypeEditPopupTitleText;
    private Button archetypeEditPopupCloseButton;
    private Button archetypeEditPopupConfirmButton;
    private TextMeshProUGUI archetypeEditPopupConfirmButtonText;
    private readonly List<SkillArchetypeCardUI> spawnedArchetypePopupCards = new List<SkillArchetypeCardUI>();
    private string selectedCreateArchetypeId = SkillArchetypeCatalog.Logic;
    private string editingSkillId = string.Empty;
    private string pendingEditArchetypeId = string.Empty;
    private string pendingFeedbackSkillId = string.Empty;
    private SkillProgressResult pendingFeedbackResult;
    private bool responsiveReferencesCached;
    private bool responsiveDefaultsCached;
    private VerticalLayoutGroup skillsCardLayoutGroup;
    private LayoutElement heroCardLayoutElement;
    private VerticalLayoutGroup heroCardLayoutGroup;
    private LayoutElement chartContainerLayoutElement;
    private VerticalLayoutGroup chartContainerLayoutGroup;
    private LayoutElement skillNameInputLayoutElement;
    private LayoutElement iconRowLayoutElement;
    private HorizontalLayoutGroup iconRowLayoutGroup;
    private VerticalLayoutGroup skillsListLayoutGroup;
    private LayoutElement closeButtonLayoutElement;
    private LayoutElement heroActionButtonLayoutElement;
    private LayoutElement addSkillButtonLayoutElement;
    private readonly Dictionary<TextMeshProUGUI, float> defaultFontSizes = new Dictionary<TextMeshProUGUI, float>();
    private readonly Dictionary<LayoutElement, LayoutElementDefaults> defaultLayoutElements = new Dictionary<LayoutElement, LayoutElementDefaults>();
    private readonly Dictionary<HorizontalOrVerticalLayoutGroup, LayoutGroupDefaults> defaultLayoutGroups = new Dictionary<HorizontalOrVerticalLayoutGroup, LayoutGroupDefaults>();

    private sealed class LayoutElementDefaults
    {
        public float PreferredWidth;
        public float PreferredHeight;
    }

    private sealed class LayoutGroupDefaults
    {
        public float Spacing;
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;
    }

    private void Awake()
    {
        if (openButton != null) openButton.onClick.AddListener(OpenPanel);
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
            closeButton.gameObject.SetActive(false);
        }
        if (heroActionButton != null)
        {
            heroActionButton.onClick.RemoveListener(HandleHeroAction);
            heroActionButton.onClick.AddListener(HandleHeroAction);
        }
        if (previousIconButton != null)
        {
            previousIconButton.onClick.RemoveListener(SelectPreviousIcon);
            previousIconButton.onClick.AddListener(SelectPreviousIcon);
            previousIconButton.gameObject.SetActive(false);
        }
        if (nextIconButton != null)
        {
            nextIconButton.onClick.RemoveListener(SelectNextIcon);
            nextIconButton.onClick.AddListener(SelectNextIcon);
            nextIconButton.gameObject.SetActive(false);
        }
        if (addSkillButton != null) addSkillButton.onClick.AddListener(AddSkillFromUI);
        if (skillNameInput != null) skillNameInput.onValueChanged.AddListener(_ => RefreshAddButtonState());

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (skillGainPopupTransform == null && skillGainPopupRoot != null)
        {
            skillGainPopupTransform = skillGainPopupRoot.GetComponent<RectTransform>();
        }

        if (skillGainPopupCanvasGroup == null && skillGainPopupRoot != null)
        {
            skillGainPopupCanvasGroup = skillGainPopupRoot.GetComponent<CanvasGroup>();
        }

        popupController = new SkillsPanelPopupController(
            skillGainPopupRoot,
            skillGainPopupCanvasGroup,
            skillGainPopupTransform,
            skillGainPopupIconText,
            skillGainPopupText);
        popupController.HideImmediate(this);

        EnsureArchetypePickerUi();
        EnsureArchetypeEditPopupUi();
        CacheResponsiveReferences();
        CacheResponsiveDefaults();
        UpdateIconPreview();
        ApplyResponsiveLayout();
    }

    private void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.OnSkillsChanged += RefreshUI;
            gameManager.OnSkillProgressAdded += HandleSkillProgressAdded;
        }
    }

    private void Start()
    {
        RefreshUI();
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnSkillsChanged -= RefreshUI;
            gameManager.OnSkillProgressAdded -= HandleSkillProgressAdded;
        }

        HideSkillGainPopupImmediate();
        CloseArchetypeEditPopup();
        ClearPendingSkillFeedback();
    }

    public void SetGameManager(GameManager manager)
    {
        if (gameManager == manager)
        {
            return;
        }

        if (gameManager != null)
        {
            gameManager.OnSkillsChanged -= RefreshUI;
            gameManager.OnSkillProgressAdded -= HandleSkillProgressAdded;
        }

        gameManager = manager;
        panelCoordinator = gameManager != null ? new SkillsPanelCoordinator(gameManager) : null;

        if (isActiveAndEnabled && gameManager != null)
        {
            gameManager.OnSkillsChanged += RefreshUI;
            gameManager.OnSkillProgressAdded += HandleSkillProgressAdded;
        }
    }

    public void ShowPanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        ApplyResponsiveLayout();

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(false);
        }

        HideSkillGainPopupImmediate();
        CloseArchetypeEditPopup();
        ClearPendingSkillFeedback();
        SetStatus(string.Empty);
        RefreshUI();
    }

    public void HidePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(false);
        }

        HideSkillGainPopupImmediate();
        CloseArchetypeEditPopup();
        ClearPendingSkillFeedback();
        SetStatus(string.Empty);
    }

    public bool IsPanelVisible()
    {
        return panelRoot != null && panelRoot.activeSelf;
    }

    private void OpenPanel()
    {
        ShowPanel();
    }

    private void ClosePanel()
    {
        HidePanel();
    }

    private void SelectPreviousIcon()
    {
        List<SkillArchetypeDefinition> archetypes = GetSelectableArchetypes();
        if (archetypes.Count == 0)
        {
            return;
        }

        int currentIndex = GetSelectedArchetypeIndex(archetypes, selectedCreateArchetypeId);
        currentIndex = (currentIndex - 1 + archetypes.Count) % archetypes.Count;
        selectedCreateArchetypeId = archetypes[currentIndex].Id;
        UpdateIconPreview();
    }

    private void SelectNextIcon()
    {
        List<SkillArchetypeDefinition> archetypes = GetSelectableArchetypes();
        if (archetypes.Count == 0)
        {
            return;
        }

        int currentIndex = GetSelectedArchetypeIndex(archetypes, selectedCreateArchetypeId);
        currentIndex = (currentIndex + 1) % archetypes.Count;
        selectedCreateArchetypeId = archetypes[currentIndex].Id;
        UpdateIconPreview();
    }

    private void UpdateIconPreview()
    {
        if (iconPreviewText != null)
        {
            SkillArchetypeDefinition selectedArchetype = SkillArchetypeCatalog.GetDefinition(selectedCreateArchetypeId);
            iconPreviewText.text = selectedArchetype.DisplayName;
            float baseFontSize = 18f;
            iconPreviewText.fontSize = baseFontSize * GetResponsiveProfile().FontScale;
            iconPreviewText.enabled = true;
            DisableInlineIconOverlay(iconPreviewText);
        }

        RefreshArchetypePicker();
        RefreshAddButtonState();
    }

    private void AddSkillFromUI()
    {
        if (panelCoordinator == null)
        {
            SetStatus("GameManager missing");
            return;
        }

        SkillsPanelActionResult result = panelCoordinator.AddSkill(
            skillNameInput != null ? skillNameInput.text : string.Empty,
            selectedCreateArchetypeId);
        if (!result.Success)
        {
            SetStatus(result.Message);
            RefreshAddButtonState();
            return;
        }

        if (skillNameInput != null)
        {
            skillNameInput.text = string.Empty;
        }

        SetStatus(result.Message);
        RefreshUI();
    }

    private void OnSelectSkill(string skillId)
    {
        if (panelCoordinator == null)
        {
            return;
        }

        SkillsPanelActionResult result = panelCoordinator.SelectSkillForFocus(skillId);
        if (result.Success)
        {
            if (!string.IsNullOrEmpty(result.Message))
            {
                SetStatus(result.Message);
            }

            RefreshUI();
        }
    }

    private void OnRemoveSkill(string skillId)
    {
        if (panelCoordinator == null)
        {
            return;
        }

        SkillsPanelActionResult result = panelCoordinator.RemoveSkill(skillId);
        SetStatus(result.Message);
        if (result.Success)
        {
            RefreshUI();
        }
    }

    private void OnChangeSkillType(string skillId)
    {
        if (panelCoordinator == null)
        {
            return;
        }

        SkillEntry skill = panelCoordinator.GetSkillById(skillId);
        if (skill == null)
        {
            return;
        }

        editingSkillId = skill.id;
        pendingEditArchetypeId = SkillArchetypeCatalog.IsSelectable(skill.archetypeId)
            ? SkillArchetypeCatalog.NormalizeArchetypeId(skill.archetypeId)
            : GetFirstSelectableArchetypeId();
        OpenArchetypeEditPopup(skill);
    }

    private void HandleSkillProgressAdded(SkillProgressResult result)
    {
        if (!IsPanelVisible())
        {
            HideSkillGainPopupImmediate();
            ClearPendingSkillFeedback();
            return;
        }

        if (result == null || string.IsNullOrEmpty(result.skillId))
        {
            return;
        }

        pendingFeedbackSkillId = result.skillId;
        pendingFeedbackResult = result;

        if (panelCoordinator == null)
        {
            return;
        }

        SkillEntry skill = panelCoordinator.GetSkillById(result.skillId);
        if (skill == null)
        {
            return;
        }

        SkillProgressionViewData view = panelCoordinator.GetSkillProgressionView(result.skillId);
        ShowSkillGainPopup(skill, view, result);
    }

    private void RefreshUI()
    {
        ApplyResponsiveLayout();
        RefreshArchetypePicker();
        RefreshArchetypeEditPopup();

        if (panelCoordinator == null)
        {
            return;
        }

        SkillsPanelSnapshot snapshot = panelCoordinator.GetSnapshot();
        List<SkillEntry> skills = snapshot.Skills;
        List<SkillProgressionViewData> skillViews = snapshot.SkillViews;
        string selectedSkillId = snapshot.SelectedSkillId;
        SkillsHeroState heroState = panelCoordinator.GetHeroState(snapshot);
        UpdateFocusSkillStatus(heroState);
        RefreshHeroBlock(heroState);
        RefreshAddButtonState();

        if (panelRoot != null && !panelRoot.activeSelf)
        {
            ClearPendingSkillFeedback();
            return;
        }

        List<SkillsChartEntryViewData> chartSkills = SkillsPanelPresenter.BuildChartEntries(skills, RadarChartSkillLimit);
        string highlightedSkillId = pendingFeedbackSkillId;
        int highlightedChartIndex = SkillsPanelPresenter.GetChartSkillIndex(chartSkills, highlightedSkillId);

        SkillsPanelChartUtility.UpdateChartPresentation(
            chartTitleText,
            chartEmptyStateText,
            radarLabelsRoot,
            skills,
            MinimumSkillsForRadar);
        SkillsPanelChartUtility.RebuildChart(
            radarChartGraphic,
            radarLabelsRoot,
            radarLabelTemplate,
            chartEmptyStateText,
            spawnedRadarLabels,
            chartSkills,
            skillViews,
            highlightedChartIndex >= 0 ? highlightedSkillId : string.Empty,
            GetResponsiveProfile(),
            MinimumSkillsForRadar);
        SkillsPanelChartUtility.RebuildRows(
            skillsListContainer,
            skillRowTemplate,
            spawnedRows,
            skills,
            skillViews,
            selectedSkillId,
            chartSkills,
            highlightedSkillId,
            OnSelectSkill,
            OnChangeSkillType,
            OnRemoveSkill);

        if (emptyStateText != null)
        {
            emptyStateText.gameObject.SetActive(skills.Count == 0);
            emptyStateText.text = "No skills yet";
        }

        ClearPendingSkillFeedback();
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyResponsiveLayout();
    }

    private void UpdateFocusSkillStatus(SkillsHeroState heroState)
    {
        if (panelSelectedSkillText != null)
        {
            panelSelectedSkillText.text = heroState.HeroLabel;
        }

        if (focusSkillStatusText != null)
        {
            focusSkillStatusText.text = heroState.HudLabel;
        }
    }

    private void RefreshHeroBlock(SkillsHeroState heroState)
    {
        SkillEntry heroSkill = heroState.HeroSkill;
        SkillProgressionViewData heroView = heroState.HeroView;
        SkillsHeroVisualViewData heroVisual = SkillsPanelPresenter.BuildHeroVisual(heroState);

        if (heroCardBackgroundImage != null)
        {
            heroCardBackgroundImage.color = heroVisual.BackgroundColor;
        }

        if (heroIconBadgeImage != null)
        {
            heroIconBadgeImage.color = heroVisual.BadgeColor;
        }

        if (heroSkillIconText != null)
        {
            heroSkillIconText.text = heroVisual.IconText;
            heroSkillIconText.fontSize = heroVisual.IconBaseFontSize * GetResponsiveProfile().FontScale;
            UiIconViewUtility.ApplyIconToTextSlot(heroSkillIconText, heroVisual.IconText);
        }

        if (heroSkillNameText != null)
        {
            heroSkillNameText.text = heroState.HeroNameText;
        }

        if (heroSkillMetaText != null)
        {
            heroSkillMetaText.text = heroState.HeroMetaText;
        }

        if (heroProgressFillImage != null)
        {
            heroProgressFillImage.fillAmount = heroView == null ? 0f : (heroView.isMaxed ? 1f : Mathf.Clamp01(heroView.progressInLevel01));
            heroProgressFillImage.color = heroVisual.AccentColor;
        }

        if (heroProgressText != null)
        {
            heroProgressText.text = heroState.ProgressLabel;
        }

        if (heroHintText != null)
        {
            heroHintText.text = heroState.HintText;
        }

        if (heroActionButtonText != null)
        {
            heroActionButtonText.text = heroState.ActionText;
        }

        if (heroActionButton != null)
        {
            heroActionButton.interactable = true;
            Image buttonImage = heroActionButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = heroVisual.ButtonColor;
            }
        }
    }

    private void HandleHeroAction()
    {
        if (panelCoordinator == null)
        {
            return;
        }

        SkillsPanelSnapshot snapshot = panelCoordinator.GetSnapshot();
        SkillsHeroState heroState = panelCoordinator.GetHeroState(snapshot);
        SkillEntry heroSkill = heroState.HeroSkill;
        if (heroSkill == null)
        {
            if (skillNameInput != null)
            {
                skillNameInput.Select();
                skillNameInput.ActivateInputField();
            }

            SetStatus("Name a skill below to unlock focus");
            return;
        }

        SkillsPanelActionResult result = panelCoordinator.StartHeroFocus(heroSkill.id);
        if (!string.IsNullOrEmpty(result.Message))
        {
            SetStatus(result.Message);
        }

        RefreshUI();
    }

    private void RefreshAddButtonState()
    {
        if (addSkillButton == null || panelCoordinator == null)
        {
            return;
        }

        bool hasName = skillNameInput != null && !string.IsNullOrWhiteSpace(skillNameInput.text);
        bool hasDuplicate = hasName && panelCoordinator.HasDuplicateSkillName(skillNameInput.text);
        bool hasArchetype = SkillArchetypeCatalog.IsSelectable(selectedCreateArchetypeId);

        addSkillButton.interactable = hasName && !hasDuplicate && hasArchetype;

        if (addSkillButtonText != null)
        {
            addSkillButtonText.text = hasDuplicate ? "Exists" : "Create Skill";
        }
    }

    private void ShowSkillGainPopup(SkillEntry skill, SkillProgressionViewData view, SkillProgressResult result)
    {
        if (skill == null || view == null || result == null || skillGainPopupRoot == null || !IsPanelVisible())
        {
            return;
        }

        SkillsGainPopupViewData popupView = SkillsPanelPresenter.BuildGainPopup(skill, view, result);
        if (popupController == null)
        {
            popupController = new SkillsPanelPopupController(
                skillGainPopupRoot,
                skillGainPopupCanvasGroup,
                skillGainPopupTransform,
                skillGainPopupIconText,
                skillGainPopupText);
        }

        popupController.Show(this, popupView);
    }

    private void HideSkillGainPopupImmediate()
    {
        if (popupController == null)
        {
            popupController = new SkillsPanelPopupController(
                skillGainPopupRoot,
                skillGainPopupCanvasGroup,
                skillGainPopupTransform,
                skillGainPopupIconText,
                skillGainPopupText);
        }

        popupController.HideImmediate(this);
    }

    private void ClearPendingSkillFeedback()
    {
        pendingFeedbackSkillId = string.Empty;
        pendingFeedbackResult = null;
    }

    private List<SkillArchetypeDefinition> GetSelectableArchetypes()
    {
        List<SkillArchetypeDefinition> archetypes = panelCoordinator != null
            ? panelCoordinator.GetSelectableArchetypes()
            : new List<SkillArchetypeDefinition>(SkillArchetypeCatalog.GetPlayerSelectableDefinitions());

        if (archetypes.Count > 0 && !SkillArchetypeCatalog.IsSelectable(selectedCreateArchetypeId))
        {
            selectedCreateArchetypeId = archetypes[0].Id;
        }

        return archetypes;
    }

    private int GetSelectedArchetypeIndex(List<SkillArchetypeDefinition> archetypes, string archetypeId)
    {
        if (archetypes == null || archetypes.Count == 0)
        {
            return 0;
        }

        for (int i = 0; i < archetypes.Count; i++)
        {
            if (archetypes[i] != null && archetypes[i].Id == SkillArchetypeCatalog.NormalizeArchetypeId(archetypeId))
            {
                return i;
            }
        }

        return 0;
    }

    private string GetFirstSelectableArchetypeId()
    {
        List<SkillArchetypeDefinition> archetypes = GetSelectableArchetypes();
        return archetypes.Count > 0 ? archetypes[0].Id : SkillArchetypeCatalog.Logic;
    }

    private void EnsureArchetypePickerUi()
    {
        if (archetypePickerRoot != null || panelRoot == null)
        {
            return;
        }

        Transform skillsCard = panelRoot.transform.Find("SkillsCard");
        if (skillsCard == null)
        {
            return;
        }

        GameObject pickerObject = new GameObject("ArchetypePicker", typeof(RectTransform), typeof(LayoutElement), typeof(GridLayoutGroup));
        pickerObject.transform.SetParent(skillsCard, false);

        LayoutElement pickerLayout = pickerObject.GetComponent<LayoutElement>();
        pickerLayout.preferredHeight = 248f;

        archetypePickerRoot = pickerObject.GetComponent<RectTransform>();
        archetypePickerGrid = pickerObject.GetComponent<GridLayoutGroup>();
        archetypePickerGrid.cellSize = new Vector2(150f, 74f);
        archetypePickerGrid.spacing = new Vector2(10f, 10f);
        archetypePickerGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        archetypePickerGrid.constraintCount = 2;
        archetypePickerGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        archetypePickerGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
        archetypePickerGrid.childAlignment = TextAnchor.UpperCenter;

        Transform inputTransform = skillsCard.Find("SkillNameInput");
        int siblingIndex = inputTransform != null ? inputTransform.GetSiblingIndex() + 1 : 0;
        pickerObject.transform.SetSiblingIndex(siblingIndex);
    }

    private void RefreshArchetypePicker()
    {
        EnsureArchetypePickerUi();
        List<SkillArchetypeDefinition> archetypes = GetSelectableArchetypes();
        if (archetypePickerRoot == null)
        {
            return;
        }

        if (archetypes.Count > 0 && !SkillArchetypeCatalog.IsSelectable(selectedCreateArchetypeId))
        {
            selectedCreateArchetypeId = archetypes[0].Id;
        }

        for (int i = 0; i < archetypes.Count; i++)
        {
            SkillArchetypeCardUI card = EnsureArchetypeCard(spawnedArchetypeCards, i, archetypePickerRoot, "CreateArchetypeCard");
            if (card == null)
            {
                continue;
            }

            card.Bind(archetypes[i], archetypes[i].Id == selectedCreateArchetypeId, OnSelectCreateArchetype);
        }

        HideUnusedArchetypeCards(spawnedArchetypeCards, archetypes.Count);
    }

    private void OnSelectCreateArchetype(string archetypeId)
    {
        if (!SkillArchetypeCatalog.IsSelectable(archetypeId))
        {
            return;
        }

        selectedCreateArchetypeId = SkillArchetypeCatalog.NormalizeArchetypeId(archetypeId);
        UpdateIconPreview();
    }

    private void EnsureArchetypeEditPopupUi()
    {
        if (archetypeEditPopupRoot != null || panelRoot == null)
        {
            return;
        }

        GameObject overlayObject = new GameObject("ArchetypeEditPopup", typeof(RectTransform), typeof(Image));
        overlayObject.transform.SetParent(panelRoot.transform, false);
        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Image overlayImage = overlayObject.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.72f);
        overlayObject.SetActive(false);
        archetypeEditPopupRoot = overlayObject;

        GameObject cardObject = new GameObject("PopupCard", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        cardObject.transform.SetParent(overlayObject.transform, false);
        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(640f, 560f);
        Image cardImage = cardObject.GetComponent<Image>();
        cardImage.color = new Color(0.10f, 0.14f, 0.20f, 0.98f);
        VerticalLayoutGroup cardLayout = cardObject.GetComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(22, 22, 22, 22);
        cardLayout.spacing = 14f;
        cardLayout.childControlHeight = true;
        cardLayout.childControlWidth = true;
        cardLayout.childForceExpandHeight = false;
        cardLayout.childForceExpandWidth = true;

        archetypeEditPopupTitleText = CreatePanelText("Title", cardObject.transform, 26f, TextAlignmentOptions.Center);

        GameObject gridObject = new GameObject("Cards", typeof(RectTransform), typeof(LayoutElement), typeof(GridLayoutGroup));
        gridObject.transform.SetParent(cardObject.transform, false);
        LayoutElement gridLayoutElement = gridObject.GetComponent<LayoutElement>();
        gridLayoutElement.preferredHeight = 348f;
        archetypeEditPopupCardsRoot = gridObject.GetComponent<RectTransform>();
        archetypeEditPopupGrid = gridObject.GetComponent<GridLayoutGroup>();
        archetypeEditPopupGrid.cellSize = new Vector2(164f, 82f);
        archetypeEditPopupGrid.spacing = new Vector2(10f, 10f);
        archetypeEditPopupGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        archetypeEditPopupGrid.constraintCount = 2;

        GameObject footerObject = new GameObject("Footer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        footerObject.transform.SetParent(cardObject.transform, false);
        HorizontalLayoutGroup footerLayout = footerObject.GetComponent<HorizontalLayoutGroup>();
        footerLayout.spacing = 12f;
        footerLayout.childControlHeight = true;
        footerLayout.childControlWidth = true;
        footerLayout.childForceExpandWidth = true;

        archetypeEditPopupCloseButton = CreatePanelButton("CancelButton", footerObject.transform, "Отмена", out _);
        archetypeEditPopupConfirmButton = CreatePanelButton("ConfirmButton", footerObject.transform, "Применить", out archetypeEditPopupConfirmButtonText);
        if (archetypeEditPopupCloseButton != null)
        {
            archetypeEditPopupCloseButton.onClick.RemoveAllListeners();
            archetypeEditPopupCloseButton.onClick.AddListener(CloseArchetypeEditPopup);
        }

        if (archetypeEditPopupConfirmButton != null)
        {
            archetypeEditPopupConfirmButton.onClick.RemoveAllListeners();
            archetypeEditPopupConfirmButton.onClick.AddListener(ConfirmArchetypeEdit);
        }
    }

    private void OpenArchetypeEditPopup(SkillEntry skill)
    {
        EnsureArchetypeEditPopupUi();
        if (skill == null || archetypeEditPopupRoot == null)
        {
            return;
        }

        editingSkillId = skill.id;
        pendingEditArchetypeId = SkillArchetypeCatalog.IsSelectable(skill.archetypeId)
            ? SkillArchetypeCatalog.NormalizeArchetypeId(skill.archetypeId)
            : GetFirstSelectableArchetypeId();

        if (archetypeEditPopupTitleText != null)
        {
            archetypeEditPopupTitleText.text = $"Тип навыка: {skill.name}";
        }

        archetypeEditPopupRoot.SetActive(true);
        RefreshArchetypeEditPopup();
    }

    private void RefreshArchetypeEditPopup()
    {
        if (archetypeEditPopupRoot == null || !archetypeEditPopupRoot.activeSelf)
        {
            return;
        }

        List<SkillArchetypeDefinition> archetypes = GetSelectableArchetypes();
        for (int i = 0; i < archetypes.Count; i++)
        {
            SkillArchetypeCardUI card = EnsureArchetypeCard(spawnedArchetypePopupCards, i, archetypeEditPopupCardsRoot, "EditArchetypeCard");
            if (card == null)
            {
                continue;
            }

            card.Bind(archetypes[i], archetypes[i].Id == pendingEditArchetypeId, OnSelectEditArchetype);
        }

        HideUnusedArchetypeCards(spawnedArchetypePopupCards, archetypes.Count);
        if (archetypeEditPopupConfirmButton != null)
        {
            archetypeEditPopupConfirmButton.interactable =
                !string.IsNullOrEmpty(editingSkillId) &&
                SkillArchetypeCatalog.IsSelectable(pendingEditArchetypeId);
        }
    }

    private void OnSelectEditArchetype(string archetypeId)
    {
        if (!SkillArchetypeCatalog.IsSelectable(archetypeId))
        {
            return;
        }

        pendingEditArchetypeId = SkillArchetypeCatalog.NormalizeArchetypeId(archetypeId);
        RefreshArchetypeEditPopup();
    }

    private void ConfirmArchetypeEdit()
    {
        if (panelCoordinator == null || string.IsNullOrEmpty(editingSkillId))
        {
            return;
        }

        SkillsPanelActionResult result = panelCoordinator.ChangeSkillArchetype(editingSkillId, pendingEditArchetypeId);
        SetStatus(result.Message);
        if (!result.Success)
        {
            return;
        }

        CloseArchetypeEditPopup();
        RefreshUI();
    }

    private void CloseArchetypeEditPopup()
    {
        if (archetypeEditPopupRoot != null)
        {
            archetypeEditPopupRoot.SetActive(false);
        }

        editingSkillId = string.Empty;
        pendingEditArchetypeId = string.Empty;
    }

    private SkillArchetypeCardUI EnsureArchetypeCard(List<SkillArchetypeCardUI> cards, int index, Transform parent, string objectNamePrefix)
    {
        if (cards == null || parent == null || index < 0)
        {
            return null;
        }

        while (cards.Count <= index)
        {
            SkillArchetypeCardUI createdCard = CreateArchetypeCard(parent, objectNamePrefix + "_" + cards.Count);
            cards.Add(createdCard);
        }

        return cards[index];
    }

    private SkillArchetypeCardUI CreateArchetypeCard(Transform parent, string objectName)
    {
        if (parent == null)
        {
            return null;
        }

        GameObject cardObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(SkillArchetypeCardUI));
        cardObject.transform.SetParent(parent, false);
        LayoutElement cardLayout = cardObject.GetComponent<LayoutElement>();
        cardLayout.preferredWidth = 150f;
        cardLayout.preferredHeight = 74f;
        cardObject.GetComponent<Image>().color = new Color(0.20f, 0.24f, 0.34f, 0.92f);

        GameObject outlineObject = new GameObject("Outline", typeof(RectTransform), typeof(Image));
        outlineObject.transform.SetParent(cardObject.transform, false);
        RectTransform outlineRect = outlineObject.GetComponent<RectTransform>();
        outlineRect.anchorMin = Vector2.zero;
        outlineRect.anchorMax = Vector2.one;
        outlineRect.offsetMin = new Vector2(2f, 2f);
        outlineRect.offsetMax = new Vector2(-2f, -2f);
        Image outlineImage = outlineObject.GetComponent<Image>();
        outlineImage.color = new Color(0.48f, 0.55f, 0.70f, 0.75f);
        outlineImage.raycastTarget = false;

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform));
        iconObject.transform.SetParent(cardObject.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(12f, 0f);
        iconRect.sizeDelta = new Vector2(42f, 42f);
        TextMeshProUGUI iconText = iconObject.AddComponent<TextMeshProUGUI>();
        iconText.fontSize = 20f;
        iconText.alignment = TextAlignmentOptions.Center;
        iconText.textWrappingMode = TextWrappingModes.NoWrap;

        GameObject nameObject = new GameObject("Name", typeof(RectTransform));
        nameObject.transform.SetParent(cardObject.transform, false);
        RectTransform nameRect = nameObject.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.offsetMin = new Vector2(58f, 8f);
        nameRect.offsetMax = new Vector2(-10f, -8f);
        TextMeshProUGUI nameText = nameObject.AddComponent<TextMeshProUGUI>();
        nameText.fontSize = 15f;
        nameText.alignment = TextAlignmentOptions.MidlineLeft;
        nameText.textWrappingMode = TextWrappingModes.Normal;

        SkillArchetypeCardUI card = cardObject.GetComponent<SkillArchetypeCardUI>();
        card.backgroundImage = cardObject.GetComponent<Image>();
        card.outlineImage = outlineImage;
        card.iconText = iconText;
        card.nameText = nameText;
        card.button = cardObject.GetComponent<Button>();
        return card;
    }

    private void HideUnusedArchetypeCards(List<SkillArchetypeCardUI> cards, int usedCount)
    {
        for (int i = usedCount; i < cards.Count; i++)
        {
            if (cards[i] != null)
            {
                cards[i].gameObject.SetActive(false);
            }
        }
    }

    private TextMeshProUGUI CreatePanelText(string name, Transform parent, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private Button CreatePanelButton(string name, Transform parent, string labelText, out TextMeshProUGUI label)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);
        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 46f;
        layoutElement.preferredWidth = 0f;
        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.22f, 0.31f, 0.44f, 0.98f);

        label = CreatePanelText("Label", buttonObject.transform, 18f, TextAlignmentOptions.Center);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        label.text = labelText;
        label.textWrappingMode = TextWrappingModes.NoWrap;

        return buttonObject.GetComponent<Button>();
    }

    private void CacheResponsiveReferences()
    {
        if (responsiveReferencesCached || panelRoot == null)
        {
            return;
        }

        Transform panelTransform = panelRoot.transform;
        skillsCardLayoutGroup = panelTransform.Find("SkillsCard")?.GetComponent<VerticalLayoutGroup>();
        heroCardLayoutElement = panelTransform.Find("SkillsCard/HeroCard")?.GetComponent<LayoutElement>();
        heroCardLayoutGroup = panelTransform.Find("SkillsCard/HeroCard")?.GetComponent<VerticalLayoutGroup>();
        chartContainerLayoutElement = panelTransform.Find("SkillsCard/ChartContainer")?.GetComponent<LayoutElement>();
        chartContainerLayoutGroup = panelTransform.Find("SkillsCard/ChartContainer")?.GetComponent<VerticalLayoutGroup>();
        skillNameInputLayoutElement = panelTransform.Find("SkillsCard/SkillNameInput")?.GetComponent<LayoutElement>();
        iconRowLayoutElement = panelTransform.Find("SkillsCard/IconRow")?.GetComponent<LayoutElement>();
        iconRowLayoutGroup = panelTransform.Find("SkillsCard/IconRow")?.GetComponent<HorizontalLayoutGroup>();
        skillsListLayoutGroup = panelTransform.Find("SkillsCard/SkillsList")?.GetComponent<VerticalLayoutGroup>();
        closeButtonLayoutElement = closeButton != null ? closeButton.GetComponent<LayoutElement>() : panelTransform.Find("SkillsCard/HeaderRow/CloseButton")?.GetComponent<LayoutElement>();
        heroActionButtonLayoutElement = heroActionButton != null ? heroActionButton.GetComponent<LayoutElement>() : panelTransform.Find("SkillsCard/HeroCard/HeroProgressBlock/HeroBottomRow/HeroActionButton")?.GetComponent<LayoutElement>();
        addSkillButtonLayoutElement = addSkillButton != null ? addSkillButton.GetComponent<LayoutElement>() : panelTransform.Find("SkillsCard/IconRow/AddSkillButton")?.GetComponent<LayoutElement>();

        responsiveReferencesCached = true;
    }

    private void CacheResponsiveDefaults()
    {
        if (responsiveDefaultsCached)
        {
            return;
        }

        CacheResponsiveReferences();

        RegisterFontDefault(heroSkillNameText);
        RegisterFontDefault(heroSkillMetaText);
        RegisterFontDefault(heroProgressText);
        RegisterFontDefault(heroHintText);
        RegisterFontDefault(heroActionButtonText);
        RegisterFontDefault(chartTitleText);
        RegisterFontDefault(chartEmptyStateText);
        RegisterFontDefault(addSkillButtonText);
        RegisterFontDefault(panelStatusText);
        RegisterFontDefault(emptyStateText);
        RegisterFontDefault(panelSelectedSkillText);
        RegisterFontDefault(focusSkillStatusText);

        RegisterLayoutElementDefault(heroCardLayoutElement);
        RegisterLayoutElementDefault(chartContainerLayoutElement);
        RegisterLayoutElementDefault(skillNameInputLayoutElement);
        RegisterLayoutElementDefault(iconRowLayoutElement);
        RegisterLayoutElementDefault(closeButtonLayoutElement);
        RegisterLayoutElementDefault(heroActionButtonLayoutElement);
        RegisterLayoutElementDefault(addSkillButtonLayoutElement);

        RegisterLayoutGroupDefault(skillsCardLayoutGroup);
        RegisterLayoutGroupDefault(heroCardLayoutGroup);
        RegisterLayoutGroupDefault(chartContainerLayoutGroup);
        RegisterLayoutGroupDefault(iconRowLayoutGroup);
        RegisterLayoutGroupDefault(skillsListLayoutGroup);

        responsiveDefaultsCached = true;
    }

    private void RegisterFontDefault(TextMeshProUGUI text)
    {
        if (text == null || defaultFontSizes.ContainsKey(text))
        {
            return;
        }

        defaultFontSizes[text] = text.fontSize;
    }

    private void RegisterLayoutElementDefault(LayoutElement layoutElement)
    {
        if (layoutElement == null || defaultLayoutElements.ContainsKey(layoutElement))
        {
            return;
        }

        defaultLayoutElements[layoutElement] = new LayoutElementDefaults
        {
            PreferredWidth = layoutElement.preferredWidth,
            PreferredHeight = layoutElement.preferredHeight
        };
    }

    private void RegisterLayoutGroupDefault(HorizontalOrVerticalLayoutGroup layoutGroup)
    {
        if (layoutGroup == null || defaultLayoutGroups.ContainsKey(layoutGroup))
        {
            return;
        }

        RectOffset padding = layoutGroup.padding;
        defaultLayoutGroups[layoutGroup] = new LayoutGroupDefaults
        {
            Spacing = layoutGroup.spacing,
            Left = padding != null ? padding.left : 0,
            Right = padding != null ? padding.right : 0,
            Top = padding != null ? padding.top : 0,
            Bottom = padding != null ? padding.bottom : 0
        };
    }

    private void ApplyResponsiveLayout()
    {
        if (panelRoot == null)
        {
            return;
        }

        CacheResponsiveDefaults();
        SkillsResponsiveProfile responsiveProfile = GetResponsiveProfile();

        ApplyLayoutGroupScale(skillsCardLayoutGroup, responsiveProfile.SpacingScale, responsiveProfile.PaddingScale);
        ApplyLayoutElementScale(heroCardLayoutElement, 1f, responsiveProfile.HeightScale);
        ApplyLayoutGroupScale(heroCardLayoutGroup, responsiveProfile.SpacingScale, responsiveProfile.PaddingScale);
        ApplyLayoutElementScale(chartContainerLayoutElement, 1f, responsiveProfile.ChartHeightScale);
        ApplyLayoutGroupScale(chartContainerLayoutGroup, responsiveProfile.SpacingScale, responsiveProfile.PaddingScale);
        ApplyLayoutElementScale(skillNameInputLayoutElement, 1f, responsiveProfile.InputHeightScale);
        ApplyLayoutElementScale(iconRowLayoutElement, 1f, responsiveProfile.IconRowHeightScale);
        ApplyLayoutGroupScale(iconRowLayoutGroup, responsiveProfile.SpacingScale, 1f);
        ApplyLayoutGroupScale(skillsListLayoutGroup, responsiveProfile.SpacingScale, responsiveProfile.PaddingScale);
        ApplyLayoutElementScale(closeButtonLayoutElement, responsiveProfile.HeaderScale, responsiveProfile.ButtonHeightScale);
        ApplyLayoutElementScale(heroActionButtonLayoutElement, responsiveProfile.ButtonWidthScale, responsiveProfile.ButtonHeightScale);
        ApplyLayoutElementScale(addSkillButtonLayoutElement, responsiveProfile.ButtonWidthScale, responsiveProfile.ButtonHeightScale);

        ApplyFontScale(heroSkillNameText, responsiveProfile.FontScale);
        ApplyFontScale(heroSkillMetaText, responsiveProfile.FontScale);
        ApplyFontScale(heroProgressText, responsiveProfile.FontScale);
        ApplyFontScale(heroHintText, responsiveProfile.FontScale);
        ApplyFontScale(heroActionButtonText, responsiveProfile.FontScale);
        ApplyFontScale(chartTitleText, responsiveProfile.FontScale);
        ApplyFontScale(chartEmptyStateText, responsiveProfile.FontScale);
        ApplyFontScale(addSkillButtonText, responsiveProfile.FontScale);
        ApplyFontScale(panelStatusText, responsiveProfile.FontScale);
        ApplyFontScale(emptyStateText, responsiveProfile.FontScale);
        ApplyFontScale(panelSelectedSkillText, responsiveProfile.FontScale);
        ApplyFontScale(focusSkillStatusText, responsiveProfile.FontScale);

        if (iconPreviewText != null)
        {
            iconPreviewText.fontSize = 18f * responsiveProfile.FontScale;
            iconPreviewText.enabled = true;
            DisableInlineIconOverlay(iconPreviewText);
        }

        if (heroSkillIconText != null)
        {
            string currentIcon = heroSkillIconText.text;
            float baseFontSize = !string.IsNullOrEmpty(currentIcon) && currentIcon.Length > 2 ? 28f : 38f;
            heroSkillIconText.fontSize = baseFontSize * responsiveProfile.FontScale;
            UiIconViewUtility.ApplyIconToTextSlot(heroSkillIconText, currentIcon);
        }

        if (skillRowTemplate != null)
        {
            skillRowTemplate.RefreshResponsiveLayout();
        }

        ApplyArchetypeCardLayout(responsiveProfile);

        for (int i = 0; i < spawnedRows.Count; i++)
        {
            if (spawnedRows[i] != null)
            {
                spawnedRows[i].RefreshResponsiveLayout();
            }
        }
    }

    private void ApplyArchetypeCardLayout(SkillsResponsiveProfile responsiveProfile)
    {
        Vector2 pickerCellSize = new Vector2(150f, 74f);
        Vector2 popupCellSize = new Vector2(164f, 82f);
        if (responsiveProfile.FontScale < 0.9f)
        {
            pickerCellSize = new Vector2(138f, 68f);
            popupCellSize = new Vector2(148f, 76f);
        }
        else if (responsiveProfile.FontScale < 1f)
        {
            pickerCellSize = new Vector2(144f, 72f);
            popupCellSize = new Vector2(156f, 80f);
        }

        if (archetypePickerGrid != null)
        {
            archetypePickerGrid.cellSize = pickerCellSize;
        }

        if (archetypeEditPopupGrid != null)
        {
            archetypeEditPopupGrid.cellSize = popupCellSize;
        }
    }

    private void ApplyFontScale(TextMeshProUGUI text, float scale)
    {
        if (text == null || !defaultFontSizes.TryGetValue(text, out float defaultSize))
        {
            return;
        }

        text.fontSize = defaultSize * scale;
    }

    private void ApplyLayoutElementScale(LayoutElement layoutElement, float widthScale, float heightScale)
    {
        if (layoutElement == null || !defaultLayoutElements.TryGetValue(layoutElement, out LayoutElementDefaults defaults))
        {
            return;
        }

        if (defaults.PreferredWidth >= 0f)
        {
            layoutElement.preferredWidth = defaults.PreferredWidth * widthScale;
        }

        if (defaults.PreferredHeight >= 0f)
        {
            layoutElement.preferredHeight = defaults.PreferredHeight * heightScale;
        }
    }

    private void ApplyLayoutGroupScale(HorizontalOrVerticalLayoutGroup layoutGroup, float spacingScale, float paddingScale)
    {
        if (layoutGroup == null || !defaultLayoutGroups.TryGetValue(layoutGroup, out LayoutGroupDefaults defaults))
        {
            return;
        }

        layoutGroup.spacing = defaults.Spacing * spacingScale;

        RectOffset padding = layoutGroup.padding ?? new RectOffset();
        padding.left = Mathf.RoundToInt(defaults.Left * paddingScale);
        padding.right = Mathf.RoundToInt(defaults.Right * paddingScale);
        padding.top = Mathf.RoundToInt(defaults.Top * paddingScale);
        padding.bottom = Mathf.RoundToInt(defaults.Bottom * paddingScale);
        layoutGroup.padding = padding;
    }

    private void DisableInlineIconOverlay(TextMeshProUGUI textSlot)
    {
        if (textSlot == null)
        {
            return;
        }

        Transform overlayTransform = textSlot.transform.Find("IconSprite");
        if (overlayTransform == null)
        {
            return;
        }

        overlayTransform.gameObject.SetActive(false);
        Image overlayImage = overlayTransform.GetComponent<Image>();
        if (overlayImage != null)
        {
            overlayImage.enabled = false;
            overlayImage.sprite = null;
        }
    }

    private float GetReferenceCanvasHeight()
    {
        RectTransform canvasRect = transform as RectTransform;
        if (canvasRect != null && canvasRect.rect.height > 0.01f)
        {
            return canvasRect.rect.height;
        }

        if (panelRoot != null)
        {
            RectTransform panelRect = panelRoot.transform as RectTransform;
            if (panelRect != null && panelRect.rect.height > 0.01f)
            {
                return panelRect.rect.height;
            }
        }

        return Screen.height;
    }

    private SkillsResponsiveProfile GetResponsiveProfile()
    {
        return SkillsPanelPresenter.BuildResponsiveProfile(GetReferenceCanvasHeight());
    }

    private void SetStatus(string message)
    {
        if (panelStatusText != null)
        {
            panelStatusText.text = message;
        }
    }
}
