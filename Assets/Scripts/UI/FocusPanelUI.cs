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
    private readonly List<TextMeshProUGUI> skillButtonLabels = new List<TextMeshProUGUI>();
    private readonly List<string> skillButtonSkillIds = new List<string>();
    private readonly List<Button> durationButtons = new List<Button>();
    private readonly int[] durationPresets = { 15, 30, 45, 60 };

    private GameManager gameManager;
    private FocusUiState uiState = FocusUiState.Setup;
    private string selectedSkillId = string.Empty;
    private int selectedDurationMinutes = 15;
    private int visibleSkillButtonCount;

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
    private TextMeshProUGUI startButtonText;

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
        PetStatusSummary petSummary = gameManager != null ? gameManager.GetPetStatusSummary() : null;
        bool isNeglected = petSummary != null && petSummary.flowState == PetFlowState.Neglected;
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

        SkillEntry skill = FindSkill(skills, selectedSkillId);
        RefreshSkillButtons(skills);
        RefreshDurationButtons();
        ApplySetupState(skills.Count, skill, isNeglected);
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
                ? $"Paused - {plannedMinutes} min planned"
                : $"Focus in progress - {plannedMinutes} min planned";
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

        FocusResultViewData viewData = FocusResultPresenter.Build(result);
        if (resultTitleText != null)
        {
            resultTitleText.text = viewData.Title;
            resultTitleText.color = viewData.TitleColor;
        }
        if (resultSubtitleText != null) resultSubtitleText.text = viewData.Subtitle;
        if (resultSkillText != null) resultSkillText.text = viewData.Skill;
        if (resultProgressText != null) resultProgressText.text = viewData.Progress;
        if (resultRewardText != null)
        {
            resultRewardText.text = viewData.Reward;
            resultRewardText.color = viewData.RewardColor;
        }
        if (resultPetText != null)
        {
            resultPetText.text = viewData.Pet;
            resultPetText.color = viewData.PetColor;
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

    private void RefreshSkillButtons(List<SkillEntry> skills)
    {
        visibleSkillButtonCount = skills != null ? skills.Count : 0;
        EnsureSkillButtonPool(visibleSkillButtonCount);

        for (int i = 0; i < visibleSkillButtonCount; i++)
        {
            SkillEntry skill = skills[i];
            Button button = skillButtons[i];
            if (button == null)
            {
                continue;
            }

            button.gameObject.SetActive(true);
            button.name = $"SkillButton_{i}";

            TextMeshProUGUI label = skillButtonLabels[i];
            if (label != null)
            {
                label.text = GetSkillLabel(skill);
            }

            skillButtonSkillIds[i] = skill.id ?? string.Empty;
            button.onClick.RemoveAllListeners();
            string skillId = skill.id;
            button.onClick.AddListener(() => OnSkillSelected(skillId));
            FocusPanelViewUtility.SetButtonSelected(button, skill.id == selectedSkillId);
        }

        for (int i = visibleSkillButtonCount; i < skillButtons.Count; i++)
        {
            if (skillButtons[i] != null)
            {
                skillButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void EnsureSkillButtonPool(int count)
    {
        if (skillListContent == null)
        {
            return;
        }

        while (skillButtons.Count < count)
        {
            int index = skillButtons.Count;
            Button button = FocusPanelViewUtility.CreateButton(skillListContent, $"SkillButton_{index}", string.Empty, null);
            skillButtons.Add(button);
            skillButtonLabels.Add(button != null ? button.GetComponentInChildren<TextMeshProUGUI>(true) : null);
            skillButtonSkillIds.Add(string.Empty);
        }
    }

    private void RefreshSkillButtonSelection()
    {
        for (int i = 0; i < visibleSkillButtonCount && i < skillButtons.Count; i++)
        {
            if (skillButtons[i] != null)
            {
                FocusPanelViewUtility.SetButtonSelected(skillButtons[i], skillButtonSkillIds[i] == selectedSkillId);
            }
        }
    }

    private void ApplySetupState(int skillCount, SkillEntry selectedSkill, bool isNeglected)
    {
        bool hasSkills = skillCount > 0;
        if (setupEmptyText != null)
        {
            setupEmptyText.gameObject.SetActive(!hasSkills);
            setupEmptyText.text = "Create at least one skill first";
        }

        if (addSkillButton != null)
        {
            addSkillButton.gameObject.SetActive(!hasSkills);
        }

        if (setupSummaryText != null)
        {
            if (selectedSkill == null)
            {
                setupSummaryText.text = "No valid skill selected.";
            }
            else if (isNeglected)
            {
                setupSummaryText.text = $"Care for your pet to unlock focus.\nSelected: {selectedSkill.name} - {selectedDurationMinutes} min";
            }
            else
            {
                setupSummaryText.text = $"Session: {selectedSkill.name} - {selectedDurationMinutes} min";
            }
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
            startButton.interactable = hasSkills && !string.IsNullOrEmpty(selectedSkillId) && selectedDurationMinutes >= 5 && !isNeglected;
        }

        if (startButtonText != null)
        {
            startButtonText.text = isNeglected ? "Care First" : "Start Focus";
        }

        if (titleText != null)
        {
            titleText.text = "Focus Setup";
        }

        if (statusText != null)
        {
            if (!hasSkills)
            {
                statusText.text = "No skills available yet.";
            }
            else if (isNeglected)
            {
                statusText.text = "Pet neglected. Care first.";
            }
            else
            {
                statusText.text = "Choose a skill and a duration.";
            }
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(true);
        }
    }

    private bool IsPetNeglected()
    {
        PetStatusSummary petSummary = gameManager != null ? gameManager.GetPetStatusSummary() : null;
        return petSummary != null && petSummary.flowState == PetFlowState.Neglected;
    }

    private void RefreshDurationButtons()
    {
        for (int i = 0; i < durationButtons.Count; i++)
        {
            if (durationButtons[i] == null) continue;
            FocusPanelViewUtility.SetButtonSelected(durationButtons[i], durationPresets[i] == selectedDurationMinutes);
        }
    }

    private void OnSkillSelected(string skillId)
    {
        selectedSkillId = skillId ?? string.Empty;
        if (gameManager != null)
        {
            gameManager.SetSelectedFocusSkill(selectedSkillId);
        }
        RefreshSkillButtonSelection();
        ApplySetupState(visibleSkillButtonCount, gameManager != null ? gameManager.GetSkillById(selectedSkillId) : null, IsPetNeglected());
    }

    private void OnDurationSelected(int minutes)
    {
        selectedDurationMinutes = Mathf.Clamp(minutes, 5, 120);
        RefreshDurationButtons();
        ApplySetupState(visibleSkillButtonCount, gameManager != null ? gameManager.GetSkillById(selectedSkillId) : null, IsPetNeglected());
    }

    private void OnCustomDurationChanged(float rawValue)
    {
        selectedDurationMinutes = Mathf.Clamp(Mathf.RoundToInt(rawValue), 5, 120);
        RefreshDurationButtons();
        ApplySetupState(visibleSkillButtonCount, gameManager != null ? gameManager.GetSkillById(selectedSkillId) : null, IsPetNeglected());
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

        SkillProgressionViewData view = gameManager != null ? gameManager.GetSkillProgressionView(skill.id) : null;
        string progressLabel = "Lv.0 - 0% to Lv.1";
        if (view != null)
        {
            progressLabel = view.isMaxed
                ? $"Lv.{view.level} - Maxed"
                : $"Lv.{view.level} - {view.progressToNextLevelPercent:0.#}% to Lv.{Mathf.Min(view.level + 1, SkillProgressionModel.MaxLevel)}";
        }
        string icon = string.IsNullOrEmpty(skill.icon) ? string.Empty : skill.icon + " ";
        return $"{icon}{skill.name} - {progressLabel}";
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
        FocusPanelLayoutRefs refs = FocusPanelLayoutBuilder.BuildLayout(
            canvasRect,
            ClosePanel,
            OnAddSkillClicked,
            OnStartClicked,
            OnPauseResumeClicked,
            OnFinishEarlyClicked,
            OnCancelClicked,
            OnDoneClicked,
            OnKeepSessionClicked,
            OnConfirmCancelClicked,
            OnCustomDurationChanged,
            durationPresets,
            OnDurationSelected,
            selectedDurationMinutes);

        panelRoot = refs.PanelRoot;
        closeButton = refs.CloseButton;
        titleText = refs.TitleText;
        statusText = refs.StatusText;
        setupRoot = refs.SetupRoot;
        setupEmptyText = refs.SetupEmptyText;
        addSkillButton = refs.AddSkillButton;
        skillListContent = refs.SkillListContent;
        setupSummaryText = refs.SetupSummaryText;
        customDurationSlider = refs.CustomDurationSlider;
        customDurationValueText = refs.CustomDurationValueText;
        startButton = refs.StartButton;
        startButtonText = refs.StartButtonText;
        activeRoot = refs.ActiveRoot;
        activeSkillText = refs.ActiveSkillText;
        activeTimerText = refs.ActiveTimerText;
        activeStatusText = refs.ActiveStatusText;
        pauseResumeButton = refs.PauseResumeButton;
        pauseResumeButtonText = refs.PauseResumeButtonText;
        finishEarlyButton = refs.FinishEarlyButton;
        cancelButton = refs.CancelButton;
        resultRoot = refs.ResultRoot;
        resultTitleText = refs.ResultTitleText;
        resultSubtitleText = refs.ResultSubtitleText;
        resultSkillText = refs.ResultSkillText;
        resultProgressText = refs.ResultProgressText;
        resultRewardText = refs.ResultRewardText;
        resultPetText = refs.ResultPetText;
        cancelConfirmRoot = refs.CancelConfirmRoot;

        HideCancelConfirm();
    }
}
