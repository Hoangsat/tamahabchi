using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillsPanelUI : MonoBehaviour
{
    private static readonly string[] IconOptions =
    {
        "MTH", "DNC", "DEV", "SPT", "ART", "BKS", "GME", "ZEN", "MSC", "WRT"
    };

    private const int RadarChartSkillLimit = 12;

    private static readonly Color[] ChartPalette =
    {
        new Color(0.95f, 0.47f, 0.47f, 1f),
        new Color(0.96f, 0.71f, 0.34f, 1f),
        new Color(0.96f, 0.88f, 0.38f, 1f),
        new Color(0.66f, 0.88f, 0.35f, 1f),
        new Color(0.44f, 0.81f, 0.58f, 1f),
        new Color(0.34f, 0.84f, 0.84f, 1f),
        new Color(0.38f, 0.69f, 0.95f, 1f),
        new Color(0.49f, 0.58f, 0.95f, 1f),
        new Color(0.72f, 0.52f, 0.95f, 1f),
        new Color(0.91f, 0.48f, 0.84f, 1f),
        new Color(0.92f, 0.56f, 0.64f, 1f),
        new Color(0.73f, 0.73f, 0.78f, 1f)
    };

    public GameObject panelRoot;
    public Button openButton;
    public Button closeButton;
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
    private int currentIconIndex;
    private Coroutine skillGainPopupCoroutine;
    private Vector2 skillGainPopupBasePosition;
    private bool hasPopupBasePosition;
    private string pendingFeedbackSkillId = string.Empty;
    private float pendingFeedbackDelta;
    private float pendingFeedbackNewPercent;

    private sealed class ChartSkill
    {
        public SkillEntry Skill;
        public Color Color;
    }

    private void Awake()
    {
        if (openButton != null) openButton.onClick.AddListener(OpenPanel);
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.gameObject.SetActive(false);
        }
        if (previousIconButton != null) previousIconButton.onClick.AddListener(SelectPreviousIcon);
        if (nextIconButton != null) nextIconButton.onClick.AddListener(SelectNextIcon);
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

        CachePopupBasePosition();

        if (gameManager == null)
        {
            Debug.LogWarning("SkillsPanelUI is waiting for GameManager injection.");
        }

        if (skillGainPopupCanvasGroup != null)
        {
            skillGainPopupCanvasGroup.alpha = 0f;
        }

        if (skillGainPopupRoot != null)
        {
            skillGainPopupRoot.SetActive(false);
        }

        UpdateIconPreview();
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

        SetStatus(string.Empty);
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
        currentIconIndex = (currentIconIndex - 1 + IconOptions.Length) % IconOptions.Length;
        UpdateIconPreview();
    }

    private void SelectNextIcon()
    {
        currentIconIndex = (currentIconIndex + 1) % IconOptions.Length;
        UpdateIconPreview();
    }

    private void UpdateIconPreview()
    {
        if (iconPreviewText != null)
        {
            iconPreviewText.text = IconOptions[currentIconIndex];
            iconPreviewText.fontSize = IconOptions[currentIconIndex].Length > 2 ? 18f : 26f;
        }

        RefreshAddButtonState();
    }

    private void AddSkillFromUI()
    {
        if (gameManager == null)
        {
            SetStatus("GameManager missing");
            return;
        }

        SkillEntry addedSkill = gameManager.AddSkill(skillNameInput != null ? skillNameInput.text : string.Empty, IconOptions[currentIconIndex]);
        if (addedSkill == null)
        {
            SetStatus("Enter a valid name");
            RefreshAddButtonState();
            return;
        }

        if (skillNameInput != null)
        {
            skillNameInput.text = string.Empty;
        }

        SetStatus("Skill added");
        RefreshUI();
    }

    private void OnSelectSkill(string skillId)
    {
        if (gameManager == null)
        {
            return;
        }

        if (gameManager.SetSelectedFocusSkill(skillId))
        {
            if (!gameManager.OpenFocusPanel(skillId))
            {
                SetStatus("Focus skill selected");
            }
            RefreshUI();
        }
    }

    private void OnRemoveSkill(string skillId)
    {
        if (gameManager == null)
        {
            return;
        }

        if (gameManager.RemoveSkill(skillId))
        {
            SetStatus("Skill removed");
            RefreshUI();
        }
        else
        {
            SetStatus("Only 0% skills can be removed");
        }
    }

    private void HandleSkillProgressAdded(string skillId, float delta, float newPercent)
    {
        pendingFeedbackSkillId = skillId;
        pendingFeedbackDelta = delta;
        pendingFeedbackNewPercent = newPercent;

        if (gameManager == null)
        {
            return;
        }

        SkillEntry skill = gameManager.GetSkillById(skillId);
        if (skill == null)
        {
            return;
        }

        ShowSkillGainPopup(skill, delta, newPercent >= 99.99f);
    }

    private void RefreshUI()
    {
        if (gameManager == null)
        {
            return;
        }

        List<SkillEntry> skills = gameManager.GetSkills();
        string selectedSkillId = gameManager.GetSelectedFocusSkill();
        UpdateFocusSkillStatus(skills, selectedSkillId);
        RefreshAddButtonState();

        if (panelRoot != null && !panelRoot.activeSelf)
        {
            pendingFeedbackSkillId = string.Empty;
            pendingFeedbackDelta = 0f;
            pendingFeedbackNewPercent = 0f;
            return;
        }

        List<ChartSkill> chartSkills = BuildChartSkills(skills);
        string highlightedSkillId = pendingFeedbackSkillId;
        int highlightedChartIndex = GetChartSkillIndex(chartSkills, highlightedSkillId);

        RebuildChart(chartSkills, highlightedChartIndex >= 0 ? highlightedSkillId : string.Empty);
        RebuildRows(skills, selectedSkillId, chartSkills, highlightedSkillId);

        if (emptyStateText != null)
        {
            emptyStateText.gameObject.SetActive(skills.Count == 0);
            emptyStateText.text = "No skills yet";
        }

        pendingFeedbackSkillId = string.Empty;
        pendingFeedbackDelta = 0f;
        pendingFeedbackNewPercent = 0f;
    }

    private void RebuildRows(List<SkillEntry> skills, string selectedSkillId, List<ChartSkill> chartSkills, string highlightedSkillId)
    {
        ClearRows();

        if (skillsListContainer == null || skillRowTemplate == null)
        {
            return;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            Color markerColor = TryGetChartColor(skills[i].id, chartSkills, out Color chartColor)
                ? chartColor
                : new Color(0.33f, 0.38f, 0.48f, 0.65f);

            SkillRowUI row = Instantiate(skillRowTemplate, skillsListContainer);
            row.gameObject.SetActive(true);
            row.Bind(
                skills[i],
                markerColor,
                skills[i].id == selectedSkillId,
                OnSelectSkill,
                OnRemoveSkill
            );

            if (!string.IsNullOrEmpty(highlightedSkillId) && skills[i].id == highlightedSkillId)
            {
                row.PlayHighlight();
            }

            spawnedRows.Add(row);
        }
    }

    private void ClearRows()
    {
        for (int i = 0; i < spawnedRows.Count; i++)
        {
            if (spawnedRows[i] == null)
            {
                continue;
            }

            spawnedRows[i].gameObject.SetActive(false);

            if (Application.isPlaying)
            {
                Destroy(spawnedRows[i].gameObject);
            }
            else
            {
                DestroyImmediate(spawnedRows[i].gameObject);
            }
        }

        spawnedRows.Clear();
    }

    private void RebuildChart(List<ChartSkill> chartSkills, string highlightedSkillId)
    {
        ClearRadarLabels();

        if (chartEmptyStateText != null)
        {
            chartEmptyStateText.gameObject.SetActive(chartSkills.Count < 3);
            chartEmptyStateText.text = chartSkills.Count == 0 ? "No skills yet" : "Add at least 3 skills";
        }

        if (radarChartGraphic == null)
        {
            return;
        }

        if (chartSkills.Count < 3)
        {
            radarChartGraphic.SetValues(null, null);
            return;
        }

        List<float> values = new List<float>(chartSkills.Count);
        List<Color> colors = new List<Color>(chartSkills.Count);
        int highlightedChartIndex = -1;

        for (int i = 0; i < chartSkills.Count; i++)
        {
            values.Add(Mathf.Clamp01(chartSkills[i].Skill.percent / 100f));
            colors.Add(chartSkills[i].Color);

            if (!string.IsNullOrEmpty(highlightedSkillId) && chartSkills[i].Skill.id == highlightedSkillId)
            {
                highlightedChartIndex = i;
            }
        }

        if (highlightedChartIndex >= 0)
        {
            radarChartGraphic.SetValuesAnimated(values, colors, highlightedChartIndex);
        }
        else
        {
            radarChartGraphic.SetValues(values, colors);
        }

        RebuildRadarLabels(chartSkills);
    }

    private void RebuildRadarLabels(List<ChartSkill> chartSkills)
    {
        if (radarLabelsRoot == null || radarLabelTemplate == null || chartSkills.Count < 3)
        {
            return;
        }

        float radius = Mathf.Min(radarLabelsRoot.rect.width, radarLabelsRoot.rect.height) * 0.5f - 4f;
        float labelRadius = radius + GetRadarLabelOffset(chartSkills.Count);
        float fontSize = GetRadarLabelFontSize(chartSkills.Count);

        for (int i = 0; i < chartSkills.Count; i++)
        {
            TextMeshProUGUI label = Instantiate(radarLabelTemplate, radarLabelsRoot);
            label.gameObject.SetActive(true);
            label.text = GetRadarLabel(chartSkills[i].Skill, chartSkills.Count);
            label.color = chartSkills[i].Color;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = fontSize;

            RectTransform labelTransform = label.rectTransform;
            labelTransform.anchorMin = new Vector2(0.5f, 0.5f);
            labelTransform.anchorMax = new Vector2(0.5f, 0.5f);
            labelTransform.pivot = new Vector2(0.5f, 0.5f);
            labelTransform.anchoredPosition = GetChartPoint(i, chartSkills.Count, labelRadius);

            spawnedRadarLabels.Add(label);
        }
    }

    private void ClearRadarLabels()
    {
        for (int i = 0; i < spawnedRadarLabels.Count; i++)
        {
            if (spawnedRadarLabels[i] == null)
            {
                continue;
            }

            spawnedRadarLabels[i].gameObject.SetActive(false);

            if (Application.isPlaying)
            {
                Destroy(spawnedRadarLabels[i].gameObject);
            }
            else
            {
                DestroyImmediate(spawnedRadarLabels[i].gameObject);
            }
        }

        spawnedRadarLabels.Clear();
    }

    private List<ChartSkill> BuildChartSkills(List<SkillEntry> skills)
    {
        List<ChartSkill> chartSkills = new List<ChartSkill>();
        if (skills == null || skills.Count == 0)
        {
            return chartSkills;
        }

        Dictionary<SkillEntry, int> sourceIndices = new Dictionary<SkillEntry, int>(skills.Count);
        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null)
            {
                sourceIndices[skills[i]] = i;
            }
        }

        List<SkillEntry> orderedSkills = new List<SkillEntry>(skills);
        orderedSkills.Sort((left, right) =>
        {
            int percentCompare = right.percent.CompareTo(left.percent);
            if (percentCompare != 0)
            {
                return percentCompare;
            }

            return sourceIndices[left].CompareTo(sourceIndices[right]);
        });

        int limit = Mathf.Min(RadarChartSkillLimit, orderedSkills.Count);
        for (int i = 0; i < limit; i++)
        {
            SkillEntry skill = orderedSkills[i];
            chartSkills.Add(new ChartSkill
            {
                Skill = skill,
                Color = GetColorForSkill(skill)
            });
        }

        return chartSkills;
    }

    private int GetChartSkillIndex(List<ChartSkill> chartSkills, string skillId)
    {
        for (int i = 0; i < chartSkills.Count; i++)
        {
            if (chartSkills[i].Skill.id == skillId)
            {
                return i;
            }
        }

        return -1;
    }

    private bool TryGetChartColor(string skillId, List<ChartSkill> chartSkills, out Color color)
    {
        for (int i = 0; i < chartSkills.Count; i++)
        {
            if (chartSkills[i].Skill.id == skillId)
            {
                color = chartSkills[i].Color;
                return true;
            }
        }

        color = Color.white;
        return false;
    }

    private string GetRadarLabel(SkillEntry skill, int axisCount)
    {
        if (skill == null)
        {
            return string.Empty;
        }

        if (axisCount >= 11 && !string.IsNullOrEmpty(skill.icon))
        {
            return skill.icon;
        }

        string shortName = skill.name;
        int maxLength = axisCount >= 10 ? 4 : axisCount >= 8 ? 5 : 8;
        if (!string.IsNullOrEmpty(shortName) && shortName.Length > maxLength)
        {
            shortName = shortName.Substring(0, maxLength);
        }

        if (string.IsNullOrEmpty(skill.icon))
        {
            return shortName;
        }

        return string.IsNullOrEmpty(shortName) ? skill.icon : $"{skill.icon}\n{shortName}";
    }

    private float GetRadarLabelOffset(int axisCount)
    {
        if (axisCount >= 10)
        {
            return 24f;
        }

        if (axisCount >= 8)
        {
            return 21f;
        }

        return 18f;
    }

    private float GetRadarLabelFontSize(int axisCount)
    {
        if (radarLabelTemplate == null)
        {
            return 9f;
        }

        float baseSize = radarLabelTemplate.fontSize;
        if (axisCount >= 10)
        {
            return Mathf.Max(7f, baseSize - 1f);
        }

        if (axisCount >= 8)
        {
            return Mathf.Max(8f, baseSize);
        }

        return baseSize;
    }

    private Vector2 GetChartPoint(int index, int count, float radius)
    {
        float angle = Mathf.PI * 0.5f - (Mathf.PI * 2f * index / count);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }

    private void UpdateFocusSkillStatus(List<SkillEntry> skills, string selectedSkillId)
    {
        string selectedName = "None";
        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i].id == selectedSkillId)
            {
                selectedName = skills[i].name;
                break;
            }
        }

        string label = "Focus Skill: " + selectedName;

        if (panelSelectedSkillText != null)
        {
            panelSelectedSkillText.text = label;
        }

        if (focusSkillStatusText != null)
        {
            focusSkillStatusText.text = label;
        }
    }

    private void RefreshAddButtonState()
    {
        if (addSkillButton == null || gameManager == null)
        {
            return;
        }

        bool hasName = skillNameInput != null && !string.IsNullOrWhiteSpace(skillNameInput.text);

        addSkillButton.interactable = hasName;

        if (addSkillButtonText != null)
        {
            addSkillButtonText.text = "Add Skill";
        }
    }

    private void ShowSkillGainPopup(SkillEntry skill, float delta, bool reachedMax)
    {
        if (skill == null || skillGainPopupRoot == null)
        {
            return;
        }

        CachePopupBasePosition();

        if (skillGainPopupIconText != null)
        {
            skillGainPopupIconText.text = string.IsNullOrEmpty(skill.icon) ? "SKL" : skill.icon;
        }

        if (skillGainPopupText != null)
        {
            string suffix = reachedMax ? " MAX" : string.Empty;
            skillGainPopupText.text = $"{skill.name} +{delta:0.#}%{suffix}";
        }

        if (skillGainPopupCoroutine != null)
        {
            StopCoroutine(skillGainPopupCoroutine);
        }

        skillGainPopupCoroutine = StartCoroutine(PlaySkillGainPopupRoutine());
    }

    private IEnumerator PlaySkillGainPopupRoutine()
    {
        const float duration = 1.2f;
        const float riseDistance = 26f;

        if (skillGainPopupRoot != null)
        {
            skillGainPopupRoot.SetActive(true);
        }

        if (skillGainPopupCanvasGroup != null)
        {
            skillGainPopupCanvasGroup.alpha = 1f;
        }

        CachePopupBasePosition();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - normalized, 2f);

            if (skillGainPopupTransform != null)
            {
                skillGainPopupTransform.anchoredPosition = skillGainPopupBasePosition + Vector2.up * (riseDistance * eased);
            }

            if (skillGainPopupCanvasGroup != null)
            {
                float alpha = normalized < 0.2f
                    ? Mathf.Lerp(0f, 1f, normalized / 0.2f)
                    : Mathf.Lerp(1f, 0f, (normalized - 0.2f) / 0.8f);
                skillGainPopupCanvasGroup.alpha = alpha;
            }

            yield return null;
        }

        if (skillGainPopupTransform != null)
        {
            skillGainPopupTransform.anchoredPosition = skillGainPopupBasePosition;
        }

        if (skillGainPopupCanvasGroup != null)
        {
            skillGainPopupCanvasGroup.alpha = 0f;
        }

        if (skillGainPopupRoot != null)
        {
            skillGainPopupRoot.SetActive(false);
        }

        skillGainPopupCoroutine = null;
    }

    private void CachePopupBasePosition()
    {
        if (hasPopupBasePosition || skillGainPopupTransform == null)
        {
            return;
        }

        skillGainPopupBasePosition = skillGainPopupTransform.anchoredPosition;
        hasPopupBasePosition = true;
    }

    private Color GetColorForIndex(int index)
    {
        return ChartPalette[index % ChartPalette.Length];
    }

    private Color GetColorForSkill(SkillEntry skill)
    {
        if (skill == null || string.IsNullOrEmpty(skill.id))
        {
            return GetColorForIndex(0);
        }

        int hash = 17;
        for (int i = 0; i < skill.id.Length; i++)
        {
            hash = (hash * 31) + skill.id[i];
        }

        if (hash == int.MinValue)
        {
            hash = 0;
        }

        return GetColorForIndex(Mathf.Abs(hash));
    }

    private void SetStatus(string message)
    {
        if (panelStatusText != null)
        {
            panelStatusText.text = message;
        }
    }
}
