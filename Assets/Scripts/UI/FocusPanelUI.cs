using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FocusPanelUI : MonoBehaviour
{
    public event Action OnPanelOpened;
    public event Action<bool> OnPanelClosed;

    private enum FocusUiState
    {
        Setup,
        Active,
        Paused,
        Result
    }

    private readonly List<Button> skillButtons = new List<Button>();
    private readonly List<Button> durationButtons = new List<Button>();
    private readonly int[] durationPresets = { 15, 30, 45, 60 };

    private GameManager gameManager;
    private FocusUiState uiState = FocusUiState.Setup;
    private string selectedSkillId = string.Empty;
    private int selectedDurationMinutes = 15;

    private GameObject panelRoot;
    private Button closeButton;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI statusText;

    private GameObject setupRoot;
    private TextMeshProUGUI setupEmptyText;
    private Button addSkillButton;
    private RectTransform skillListContent;
    private TextMeshProUGUI setupSummaryText;
    private Slider customDurationSlider;
    private TextMeshProUGUI customDurationValueText;
    private Button startButton;

    private GameObject activeRoot;
    private TextMeshProUGUI activeSkillText;
    private TextMeshProUGUI activeTimerText;
    private TextMeshProUGUI activeStatusText;
    private Button pauseResumeButton;
    private TextMeshProUGUI pauseResumeButtonText;
    private Button finishEarlyButton;
    private Button cancelButton;

    private GameObject resultRoot;
    private TextMeshProUGUI resultTitleText;
    private TextMeshProUGUI resultSubtitleText;
    private TextMeshProUGUI resultSkillText;
    private TextMeshProUGUI resultProgressText;
    private TextMeshProUGUI resultRewardText;
    private TextMeshProUGUI resultPetText;

    private GameObject cancelConfirmRoot;

    private void Awake()
    {
        BuildUiIfNeeded();
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (gameManager == null)
        {
            Debug.LogWarning("FocusPanelUI is waiting for GameManager injection.");
        }
    }

    private void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.OnSkillsChanged -= HandleSkillsChanged;
            gameManager.OnFocusSessionChanged -= HandleFocusSessionChanged;
            gameManager.OnFocusResultReady -= HandleFocusResultReady;
            gameManager.OnSkillsChanged += HandleSkillsChanged;
            gameManager.OnFocusSessionChanged += HandleFocusSessionChanged;
            gameManager.OnFocusResultReady += HandleFocusResultReady;
        }
    }

    private void OnDisable()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnSkillsChanged -= HandleSkillsChanged;
        gameManager.OnFocusSessionChanged -= HandleFocusSessionChanged;
        gameManager.OnFocusResultReady -= HandleFocusResultReady;
    }

    public void SetGameManager(GameManager manager)
    {
        if (gameManager == manager)
        {
            return;
        }

        if (gameManager != null)
        {
            gameManager.OnSkillsChanged -= HandleSkillsChanged;
            gameManager.OnFocusSessionChanged -= HandleFocusSessionChanged;
            gameManager.OnFocusResultReady -= HandleFocusResultReady;
        }

        gameManager = manager;

        if (isActiveAndEnabled && gameManager != null)
        {
            gameManager.OnSkillsChanged -= HandleSkillsChanged;
            gameManager.OnFocusSessionChanged -= HandleFocusSessionChanged;
            gameManager.OnFocusResultReady -= HandleFocusResultReady;
            gameManager.OnSkillsChanged += HandleSkillsChanged;
            gameManager.OnFocusSessionChanged += HandleFocusSessionChanged;
            gameManager.OnFocusResultReady += HandleFocusResultReady;
        }
    }

    private void Update()
    {
        if (panelRoot == null || !panelRoot.activeSelf)
        {
            return;
        }

        if (uiState == FocusUiState.Active || uiState == FocusUiState.Paused)
        {
            RefreshActive();
        }
    }

    public void OpenPanel(string preselectedSkillId = null)
    {
        BuildUiIfNeeded();

        if (!string.IsNullOrWhiteSpace(preselectedSkillId) && gameManager != null)
        {
            if (gameManager.SetSelectedFocusSkill(preselectedSkillId))
            {
                selectedSkillId = preselectedSkillId.Trim();
            }
            else
            {
                selectedSkillId = string.Empty;
            }
        }

        panelRoot.SetActive(true);
        OnPanelOpened?.Invoke();
        RefreshFromGameState();
    }

    private void ClosePanel()
    {
        if (uiState == FocusUiState.Active || uiState == FocusUiState.Paused)
        {
            return;
        }

        HidePanel(false);
    }

    public bool IsPanelVisible()
    {
        return panelRoot != null && panelRoot.activeSelf;
    }

    public bool IsBlockingNavigation()
    {
        return IsPanelVisible() && (uiState == FocusUiState.Active || uiState == FocusUiState.Paused || uiState == FocusUiState.Result);
    }

    public void ForceClosePanel(bool returnedFromResult = false)
    {
        HidePanel(returnedFromResult);
    }

    private void RefreshFromGameState()
    {
        if (gameManager == null)
        {
            return;
        }

        FocusSessionSnapshot snapshot = gameManager.GetFocusSessionSnapshot();
        FocusSessionResultData result = gameManager.GetLastFocusSessionResult();

        if (snapshot != null && snapshot.HasActiveSession())
        {
            uiState = snapshot.state == FocusSessionState.Paused ? FocusUiState.Paused : FocusUiState.Active;
            RefreshActive();
            return;
        }

        if (result != null)
        {
            uiState = FocusUiState.Result;
            RefreshResult(result);
            return;
        }

        uiState = FocusUiState.Setup;
        RefreshSetup();
    }

    private void RefreshSetup()
    {
        SetVisibleState(FocusUiState.Setup);
        HideCancelConfirm();

        List<SkillEntry> skills = gameManager != null ? gameManager.GetSkills() : new List<SkillEntry>();
        string gmSelected = gameManager != null ? gameManager.GetSelectedFocusSkill() : string.Empty;
        if (!string.IsNullOrEmpty(gmSelected))
        {
            selectedSkillId = gmSelected;
        }

        if (!HasSkill(skills, selectedSkillId))
        {
            selectedSkillId = skills.Count > 0 ? skills[0].id : string.Empty;
            if (gameManager != null && !string.IsNullOrEmpty(selectedSkillId))
            {
                gameManager.SetSelectedFocusSkill(selectedSkillId);
            }
        }

        RebuildSkillButtons(skills);
        RefreshDurationButtons();

        bool hasSkills = skills.Count > 0;
        if (setupEmptyText != null)
        {
            setupEmptyText.gameObject.SetActive(!hasSkills);
            setupEmptyText.text = "Create at least one skill first";
        }

        if (addSkillButton != null)
        {
            addSkillButton.gameObject.SetActive(!hasSkills);
        }

        SkillEntry skill = FindSkill(skills, selectedSkillId);
        if (setupSummaryText != null)
        {
            setupSummaryText.text = skill == null ? "No valid skill selected." : $"Session: {skill.name} · {selectedDurationMinutes} min";
        }

        if (customDurationSlider != null)
        {
            int clampedDuration = Mathf.Clamp(selectedDurationMinutes, 5, 120);
            if (Mathf.RoundToInt(customDurationSlider.value) != clampedDuration)
            {
                customDurationSlider.SetValueWithoutNotify(clampedDuration);
            }
        }

        if (customDurationValueText != null)
        {
            customDurationValueText.text = $"{selectedDurationMinutes} min";
        }

        if (startButton != null)
        {
            startButton.interactable = hasSkills && !string.IsNullOrEmpty(selectedSkillId) && selectedDurationMinutes >= 5;
        }

        if (titleText != null) titleText.text = "Focus Setup";
        if (statusText != null) statusText.text = hasSkills ? "Choose a skill and a duration." : "No skills available yet.";
        if (closeButton != null) closeButton.gameObject.SetActive(true);
    }

    private void RefreshActive()
    {
        SetVisibleState(uiState == FocusUiState.Paused ? FocusUiState.Paused : FocusUiState.Active);

        FocusSessionSnapshot snapshot = gameManager != null ? gameManager.GetFocusSessionSnapshot() : null;
        if (snapshot == null || !snapshot.HasActiveSession())
        {
            RefreshFromGameState();
            return;
        }

        SkillEntry skill = gameManager != null ? gameManager.GetSkillById(snapshot.skillId) : null;
        string skillLabel = skill == null ? "Unknown Skill" : $"{(string.IsNullOrEmpty(skill.icon) ? string.Empty : skill.icon + " ")}{skill.name}";

        if (activeSkillText != null) activeSkillText.text = skillLabel;
        if (activeTimerText != null) activeTimerText.text = FormatTime(snapshot.remainingSeconds);
        if (activeStatusText != null)
        {
            int plannedMinutes = Mathf.Max(0, Mathf.RoundToInt(snapshot.configuredDurationSeconds / 60f));
            activeStatusText.text = snapshot.state == FocusSessionState.Paused
                ? $"Paused · {plannedMinutes} min planned"
                : $"Focus in progress · {plannedMinutes} min planned";
        }

        if (pauseResumeButtonText != null)
        {
            pauseResumeButtonText.text = snapshot.state == FocusSessionState.Paused ? "Resume" : "Pause";
        }

        if (finishEarlyButton != null)
        {
            finishEarlyButton.interactable = snapshot.elapsedSeconds > 0f;
        }

        if (titleText != null) titleText.text = "Focus Session";
        if (statusText != null) statusText.text = "Stay with your chosen skill.";
        if (closeButton != null) closeButton.gameObject.SetActive(false);
    }

    private void RefreshResult(FocusSessionResultData result)
    {
        SetVisibleState(FocusUiState.Result);
        HideCancelConfirm();

        if (titleText != null) titleText.text = "Focus Result";
        if (statusText != null) statusText.text = "Session rewards were already applied.";
        if (closeButton != null) closeButton.gameObject.SetActive(true);

        int plannedMinutes = Mathf.Max(0, Mathf.RoundToInt(result.plannedDurationSeconds / 60f));
        int actualMinutes = Mathf.Max(0, Mathf.RoundToInt(result.actualDurationSeconds / 60f));
        if (resultTitleText != null) resultTitleText.text = result.outcome == FocusSessionOutcome.CompletedEarly ? "Completed Early" : "Completed";
        if (resultSubtitleText != null)
        {
            resultSubtitleText.text = result.outcome == FocusSessionOutcome.CompletedEarly
                ? $"You finished after {actualMinutes} min of {plannedMinutes} min."
                : $"You completed the full {plannedMinutes} min session.";
        }
        if (resultSkillText != null)
        {
            resultSkillText.text = $"{(string.IsNullOrEmpty(result.skillIcon) ? string.Empty : result.skillIcon + " ")}{result.skillName}";
        }
        if (resultProgressText != null)
        {
            resultProgressText.text = $"Skill: {result.previousPercent:0.##}% -> {result.newPercent:0.##}%  ( +{result.deltaProgress:0.##}% )";
        }
        if (resultRewardText != null)
        {
            resultRewardText.text = $"Coins: +{result.coinsReward}\nXP: +{result.xpReward}\nEnergy: +{result.energyReward:0.#}";
            if (result.lowEnergyPenaltyApplied)
            {
                resultRewardText.text += "\nLow energy penalty applied";
            }
        }
        if (resultPetText != null)
        {
            resultPetText.text = $"Pet: {result.petReaction}\nMood: +{result.moodReward:0.#}\nEnergy: {result.energyBefore:0.#} -> {result.energyAfter:0.#}";
        }
    }

    private void HandleSkillsChanged()
    {
        if (panelRoot != null && panelRoot.activeSelf && uiState == FocusUiState.Setup)
        {
            RefreshSetup();
        }
    }

    private void HandleFocusSessionChanged()
    {
        if (panelRoot != null && panelRoot.activeSelf)
        {
            RefreshFromGameState();
        }
    }

    private void HandleFocusResultReady(FocusSessionResultData _)
    {
        if (panelRoot != null && panelRoot.activeSelf)
        {
            RefreshFromGameState();
        }
    }

    private void SetVisibleState(FocusUiState state)
    {
        uiState = state;
        if (setupRoot != null) setupRoot.SetActive(state == FocusUiState.Setup);
        if (activeRoot != null) activeRoot.SetActive(state == FocusUiState.Active || state == FocusUiState.Paused);
        if (resultRoot != null) resultRoot.SetActive(state == FocusUiState.Result);
    }

    private void RebuildSkillButtons(List<SkillEntry> skills)
    {
        for (int i = 0; i < skillButtons.Count; i++)
        {
            if (skillButtons[i] == null) continue;
            if (Application.isPlaying) Destroy(skillButtons[i].gameObject);
            else DestroyImmediate(skillButtons[i].gameObject);
        }
        skillButtons.Clear();

        if (skillListContent == null)
        {
            return;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            SkillEntry skill = skills[i];
            Button button = CreateButton(skillListContent, $"SkillButton_{i}", GetSkillLabel(skill), () => OnSkillSelected(skill.id));
            skillButtons.Add(button);
            SetButtonSelected(button, skill.id == selectedSkillId);
        }
    }

    private void RefreshDurationButtons()
    {
        for (int i = 0; i < durationButtons.Count; i++)
        {
            if (durationButtons[i] == null) continue;
            SetButtonSelected(durationButtons[i], durationPresets[i] == selectedDurationMinutes);
        }
    }

    private void OnSkillSelected(string skillId)
    {
        selectedSkillId = skillId ?? string.Empty;
        if (gameManager != null)
        {
            gameManager.SetSelectedFocusSkill(selectedSkillId);
        }
        RefreshSetup();
    }

    private void OnDurationSelected(int minutes)
    {
        selectedDurationMinutes = Mathf.Clamp(minutes, 5, 120);
        RefreshSetup();
    }

    private void OnCustomDurationChanged(float rawValue)
    {
        selectedDurationMinutes = Mathf.Clamp(Mathf.RoundToInt(rawValue), 5, 120);
        RefreshSetup();
    }

    private void OnStartClicked()
    {
        if (gameManager != null && gameManager.TryStartFocusSession(selectedSkillId, selectedDurationMinutes))
        {
            RefreshFromGameState();
        }
    }

    private void OnAddSkillClicked()
    {
        HidePanel(false);
        gameManager?.OpenSkillsPanel();
    }

    private void OnPauseResumeClicked()
    {
        if (gameManager == null)
        {
            return;
        }

        FocusSessionSnapshot snapshot = gameManager.GetFocusSessionSnapshot();
        if (snapshot.state == FocusSessionState.Paused)
        {
            gameManager.ResumeFocusSession();
        }
        else
        {
            gameManager.PauseFocusSession();
        }
    }

    private void OnFinishEarlyClicked()
    {
        gameManager?.FinishFocusSessionEarly();
    }

    private void OnCancelClicked()
    {
        if (cancelConfirmRoot != null)
        {
            cancelConfirmRoot.SetActive(true);
        }
    }

    private void OnConfirmCancelClicked()
    {
        HideCancelConfirm();
        gameManager?.CancelFocusSession();
        RefreshFromGameState();
    }

    private void OnKeepSessionClicked()
    {
        HideCancelConfirm();
    }

    private void OnDoneClicked()
    {
        gameManager?.ClearLastFocusSessionResult();
        HidePanel(true);
    }

    private void HideCancelConfirm()
    {
        if (cancelConfirmRoot != null)
        {
            cancelConfirmRoot.SetActive(false);
        }
    }

    private void HidePanel(bool returnedFromResult)
    {
        HideCancelConfirm();
        SetVisibleState(FocusUiState.Setup);

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        OnPanelClosed?.Invoke(returnedFromResult);
    }

    private bool HasSkill(List<SkillEntry> skills, string skillId)
    {
        return FindSkill(skills, skillId) != null;
    }

    private SkillEntry FindSkill(List<SkillEntry> skills, string skillId)
    {
        if (skills == null || string.IsNullOrWhiteSpace(skillId))
        {
            return null;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null && skills[i].id == skillId)
            {
                return skills[i];
            }
        }

        return null;
    }

    private string GetSkillLabel(SkillEntry skill)
    {
        if (skill == null)
        {
            return "Unknown Skill";
        }

        string percent = skill.percent > 0f && skill.percent < 0.01f ? "<0.01%" : $"{skill.percent:0.##}%";
        string icon = string.IsNullOrEmpty(skill.icon) ? string.Empty : skill.icon + " ";
        return $"{icon}{skill.name} · {percent}";
    }

    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
        TimeSpan time = TimeSpan.FromSeconds(totalSeconds);
        return time.TotalHours >= 1d ? time.ToString(@"hh\:mm\:ss") : time.ToString(@"mm\:ss");
    }

    private void BuildUiIfNeeded()
    {
        if (panelRoot != null)
        {
            return;
        }

        RectTransform canvasRect = transform as RectTransform;
        panelRoot = CreatePanel(canvasRect, "FocusPanelRoot", new Color(0.06f, 0.08f, 0.12f, 0.86f), true);
        GameObject window = CreatePanel(panelRoot.transform as RectTransform, "WindowRoot", new Color(0.13f, 0.16f, 0.24f, 0.98f), false);
        RectTransform windowRect = window.GetComponent<RectTransform>();
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.sizeDelta = new Vector2(860f, 1320f);
        windowRect.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup windowLayout = window.AddComponent<VerticalLayoutGroup>();
        windowLayout.padding = new RectOffset(28, 28, 28, 28);
        windowLayout.spacing = 18f;
        windowLayout.childAlignment = TextAnchor.UpperCenter;
        windowLayout.childControlWidth = true;
        windowLayout.childControlHeight = false;
        windowLayout.childForceExpandWidth = true;
        windowLayout.childForceExpandHeight = false;

        GameObject header = CreateObject("HeaderRow", window.transform as RectTransform);
        HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 12f;
        headerLayout.childAlignment = TextAnchor.MiddleCenter;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;
        LayoutElement headerElement = header.AddComponent<LayoutElement>();
        headerElement.preferredHeight = 64f;

        titleText = CreateText(header.transform as RectTransform, "TitleText", "Focus", 34f, FontStyles.Bold, TextAlignmentOptions.Left);
        LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.flexibleWidth = 1f;

        closeButton = CreateButton(header.transform as RectTransform, "CloseButton", "Close", ClosePanel);
        LayoutElement closeLayout = closeButton.gameObject.AddComponent<LayoutElement>();
        closeLayout.preferredWidth = 160f;

        statusText = CreateText(window.transform as RectTransform, "StatusText", string.Empty, 22f, FontStyles.Normal, TextAlignmentOptions.Center);
        statusText.color = new Color(0.82f, 0.86f, 0.96f, 0.86f);

        setupRoot = CreateColumn(window.transform as RectTransform, "SetupRoot");
        activeRoot = CreateColumn(window.transform as RectTransform, "ActiveRoot");
        resultRoot = CreateColumn(window.transform as RectTransform, "ResultRoot");

        BuildSetupSection(setupRoot.transform as RectTransform);
        BuildActiveSection(activeRoot.transform as RectTransform);
        BuildResultSection(resultRoot.transform as RectTransform);
        cancelConfirmRoot = BuildCancelConfirm(window.transform as RectTransform);
        HideCancelConfirm();
    }

    private void BuildSetupSection(RectTransform parent)
    {
        setupEmptyText = CreateText(parent, "SetupEmptyText", string.Empty, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
        setupEmptyText.color = new Color(0.96f, 0.77f, 0.49f, 1f);
        addSkillButton = CreateButton(parent, "AddSkillButton", "Add Skill", OnAddSkillClicked);
        addSkillButton.gameObject.SetActive(false);
        CreateText(parent, "SkillLabel", "Choose a skill", 24f, FontStyles.Bold, TextAlignmentOptions.Left);
        skillListContent = CreateScrollContent(parent, "SkillList");
        CreateText(parent, "DurationLabel", "Choose a duration", 24f, FontStyles.Bold, TextAlignmentOptions.Left);

        GameObject durationRow = CreateObject("DurationRow", parent);
        HorizontalLayoutGroup rowLayout = durationRow.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;
        LayoutElement rowElement = durationRow.AddComponent<LayoutElement>();
        rowElement.preferredHeight = 76f;

        for (int i = 0; i < durationPresets.Length; i++)
        {
            int minutes = durationPresets[i];
            durationButtons.Add(CreateButton(durationRow.transform as RectTransform, $"Duration_{minutes}", $"{minutes} min", () => OnDurationSelected(minutes)));
        }

        CreateText(parent, "CustomDurationLabel", "Custom duration", 22f, FontStyles.Bold, TextAlignmentOptions.Left);
        GameObject customDurationRoot = CreatePanel(parent, "CustomDurationRoot", new Color(0.1f, 0.13f, 0.2f, 0.82f), false);
        VerticalLayoutGroup customLayout = customDurationRoot.AddComponent<VerticalLayoutGroup>();
        customLayout.padding = new RectOffset(18, 18, 18, 18);
        customLayout.spacing = 8f;
        customLayout.childAlignment = TextAnchor.MiddleCenter;
        customLayout.childControlWidth = true;
        customLayout.childControlHeight = true;
        customLayout.childForceExpandWidth = true;
        customLayout.childForceExpandHeight = false;

        customDurationValueText = CreateText(customDurationRoot.transform as RectTransform, "CustomDurationValueText", "15 min", 22f, FontStyles.Normal, TextAlignmentOptions.Center);
        GameObject sliderRoot = CreateObject("CustomDurationSliderRoot", customDurationRoot.transform as RectTransform);
        LayoutElement sliderLayout = sliderRoot.AddComponent<LayoutElement>();
        sliderLayout.preferredHeight = 42f;
        Image sliderBackground = sliderRoot.AddComponent<Image>();
        sliderBackground.color = new Color(1f, 1f, 1f, 0.08f);
        customDurationSlider = sliderRoot.AddComponent<Slider>();
        customDurationSlider.minValue = 5f;
        customDurationSlider.maxValue = 120f;
        customDurationSlider.wholeNumbers = true;
        customDurationSlider.value = selectedDurationMinutes;
        customDurationSlider.onValueChanged.AddListener(OnCustomDurationChanged);

        setupSummaryText = CreateText(parent, "SetupSummaryText", string.Empty, 24f, FontStyles.Normal, TextAlignmentOptions.Center);
        startButton = CreateButton(parent, "StartButton", "Start Focus", OnStartClicked);
    }

    private void BuildActiveSection(RectTransform parent)
    {
        activeSkillText = CreateText(parent, "ActiveSkillText", string.Empty, 30f, FontStyles.Bold, TextAlignmentOptions.Center);
        activeTimerText = CreateText(parent, "ActiveTimerText", "00:00", 56f, FontStyles.Bold, TextAlignmentOptions.Center);
        activeStatusText = CreateText(parent, "ActiveStatusText", string.Empty, 24f, FontStyles.Normal, TextAlignmentOptions.Center);
        pauseResumeButton = CreateButton(parent, "PauseResumeButton", "Pause", OnPauseResumeClicked);
        pauseResumeButtonText = pauseResumeButton.GetComponentInChildren<TextMeshProUGUI>();
        finishEarlyButton = CreateButton(parent, "FinishEarlyButton", "Finish Early", OnFinishEarlyClicked);
        cancelButton = CreateButton(parent, "CancelButton", "Cancel", OnCancelClicked);
    }

    private void BuildResultSection(RectTransform parent)
    {
        resultTitleText = CreateText(parent, "ResultTitleText", string.Empty, 32f, FontStyles.Bold, TextAlignmentOptions.Center);
        resultSubtitleText = CreateText(parent, "ResultSubtitleText", string.Empty, 22f, FontStyles.Normal, TextAlignmentOptions.Center);
        resultSkillText = CreateText(parent, "ResultSkillText", string.Empty, 28f, FontStyles.Bold, TextAlignmentOptions.Center);
        resultProgressText = CreateText(parent, "ResultProgressText", string.Empty, 24f, FontStyles.Normal, TextAlignmentOptions.Center);
        resultRewardText = CreateText(parent, "ResultRewardText", string.Empty, 24f, FontStyles.Normal, TextAlignmentOptions.Center);
        resultPetText = CreateText(parent, "ResultPetText", string.Empty, 22f, FontStyles.Normal, TextAlignmentOptions.Center);
        CreateButton(parent, "DoneButton", "Done", OnDoneClicked);
    }

    private GameObject BuildCancelConfirm(RectTransform parent)
    {
        GameObject overlay = CreatePanel(parent, "CancelConfirmRoot", new Color(0.02f, 0.03f, 0.06f, 0.78f), true);
        GameObject box = CreatePanel(overlay.transform as RectTransform, "CancelConfirmBox", new Color(0.17f, 0.2f, 0.3f, 1f), false);
        RectTransform boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(620f, 280f);
        boxRect.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup boxLayout = box.AddComponent<VerticalLayoutGroup>();
        boxLayout.padding = new RectOffset(24, 24, 24, 24);
        boxLayout.spacing = 18f;
        boxLayout.childAlignment = TextAnchor.MiddleCenter;
        boxLayout.childControlWidth = true;
        boxLayout.childControlHeight = false;
        boxLayout.childForceExpandWidth = true;
        boxLayout.childForceExpandHeight = false;

        CreateText(box.transform as RectTransform, "CancelTitle", "Cancel this focus session?", 28f, FontStyles.Bold, TextAlignmentOptions.Center);
        CreateText(box.transform as RectTransform, "CancelBody", "Cancelling stops the session with no reward.", 22f, FontStyles.Normal, TextAlignmentOptions.Center);

        GameObject buttonsRow = CreateObject("CancelButtonsRow", box.transform as RectTransform);
        HorizontalLayoutGroup rowLayout = buttonsRow.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;
        CreateButton(buttonsRow.transform as RectTransform, "KeepButton", "Keep Session", OnKeepSessionClicked);
        CreateButton(buttonsRow.transform as RectTransform, "ConfirmButton", "Confirm Cancel", OnConfirmCancelClicked);
        return overlay;
    }

    private RectTransform CreateScrollContent(RectTransform parent, string name)
    {
        GameObject root = CreatePanel(parent, name, new Color(0.1f, 0.13f, 0.2f, 0.82f), false);
        LayoutElement rootLayout = root.AddComponent<LayoutElement>();
        rootLayout.preferredHeight = 420f;
        ScrollRect scrollRect = root.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        GameObject viewport = CreateObject("Viewport", root.transform as RectTransform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(10f, 10f);
        viewportRect.offsetMax = new Vector2(-10f, -10f);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject content = CreateObject("Content", viewportRect);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        return contentRect;
    }

    private GameObject CreateColumn(RectTransform parent, string name)
    {
        GameObject column = CreateObject(name, parent);
        VerticalLayoutGroup layout = column.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        LayoutElement element = column.AddComponent<LayoutElement>();
        element.flexibleHeight = 1f;
        return column;
    }

    private Button CreateButton(RectTransform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject root = CreatePanel(parent, name, new Color(0.2f, 0.28f, 0.42f, 1f), false);
        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.preferredHeight = 64f;
        Button button = root.AddComponent<Button>();
        button.onClick.AddListener(onClick);
        TextMeshProUGUI text = CreateText(root.transform as RectTransform, "Label", label, 22f, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return button;
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
        return root;
    }

    private void SetButtonSelected(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
        if (image != null)
        {
            image.color = selected ? new Color(0.28f, 0.55f, 0.86f, 1f) : new Color(0.2f, 0.28f, 0.42f, 1f);
        }
        if (text != null)
        {
            text.color = selected ? Color.white : new Color(0.93f, 0.96f, 1f, 1f);
        }
    }
}
