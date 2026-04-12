using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class FocusPanelLayoutRefs
{
    public GameObject PanelRoot;
    public Button CloseButton;
    public TextMeshProUGUI TitleText;
    public TextMeshProUGUI StatusText;

    public GameObject SetupRoot;
    public TextMeshProUGUI SetupEmptyText;
    public Button AddSkillButton;
    public RectTransform SkillListContent;
    public TextMeshProUGUI SetupSummaryText;
    public Slider CustomDurationSlider;
    public TextMeshProUGUI CustomDurationValueText;
    public Button StartButton;
    public TextMeshProUGUI StartButtonText;

    public GameObject ActiveRoot;
    public TextMeshProUGUI ActiveSkillText;
    public TextMeshProUGUI ActiveTimerText;
    public TextMeshProUGUI ActiveStatusText;
    public Button PauseResumeButton;
    public TextMeshProUGUI PauseResumeButtonText;
    public Button FinishEarlyButton;
    public Button CancelButton;

    public GameObject ResultRoot;
    public TextMeshProUGUI ResultTitleText;
    public TextMeshProUGUI ResultSubtitleText;
    public TextMeshProUGUI ResultSkillText;
    public TextMeshProUGUI ResultProgressText;
    public TextMeshProUGUI ResultRewardText;
    public TextMeshProUGUI ResultPetText;

    public GameObject CancelConfirmRoot;
}

public static class FocusPanelLayoutBuilder
{
    public static FocusPanelLayoutRefs BuildLayout(
        RectTransform canvasRect,
        Action onClose,
        Action onAddSkill,
        Action onStart,
        Action onPauseResume,
        Action onFinishEarly,
        Action onCancel,
        Action onDone,
        Action onKeepSession,
        Action onConfirmCancel,
        Action<float> onCustomDurationChanged,
        int[] durationPresets,
        Action<int> onDurationSelected,
        int selectedDurationMinutes)
    {
        FocusPanelLayoutRefs refs = new FocusPanelLayoutRefs();
        if (canvasRect == null)
        {
            return refs;
        }

        refs.PanelRoot = FocusPanelViewUtility.CreatePanel(canvasRect, "FocusPanelRoot", new Color(0.06f, 0.08f, 0.12f, 0.86f), true);
        GameObject window = FocusPanelViewUtility.CreatePanel(refs.PanelRoot.transform as RectTransform, "WindowRoot", new Color(0.13f, 0.16f, 0.24f, 0.98f), false);
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

        GameObject header = FocusPanelViewUtility.CreateObject("HeaderRow", window.transform as RectTransform);
        HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 12f;
        headerLayout.childAlignment = TextAnchor.MiddleCenter;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;
        LayoutElement headerElement = header.AddComponent<LayoutElement>();
        headerElement.preferredHeight = 64f;

        refs.TitleText = FocusPanelViewUtility.CreateText(header.transform as RectTransform, "TitleText", "Focus", 34f, FontStyles.Bold, TextAlignmentOptions.Left);
        LayoutElement titleLayout = refs.TitleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.flexibleWidth = 1f;

        refs.CloseButton = FocusPanelViewUtility.CreateButton(header.transform as RectTransform, "CloseButton", "Close", () => onClose?.Invoke());
        LayoutElement closeLayout = refs.CloseButton.gameObject.AddComponent<LayoutElement>();
        closeLayout.preferredWidth = 160f;

        refs.StatusText = FocusPanelViewUtility.CreateText(window.transform as RectTransform, "StatusText", string.Empty, 22f, FontStyles.Normal, TextAlignmentOptions.Center);
        refs.StatusText.color = new Color(0.82f, 0.86f, 0.96f, 0.86f);

        refs.SetupRoot = FocusPanelViewUtility.CreateColumn(window.transform as RectTransform, "SetupRoot");
        refs.ActiveRoot = FocusPanelViewUtility.CreateColumn(window.transform as RectTransform, "ActiveRoot");
        refs.ResultRoot = FocusPanelViewUtility.CreateColumn(window.transform as RectTransform, "ResultRoot");

        BuildSetupSection(refs, refs.SetupRoot.transform as RectTransform, onAddSkill, onStart, onCustomDurationChanged, durationPresets, onDurationSelected, selectedDurationMinutes);
        BuildActiveSection(refs, refs.ActiveRoot.transform as RectTransform, onPauseResume, onFinishEarly, onCancel);
        BuildResultSection(refs, refs.ResultRoot.transform as RectTransform, onDone);
        refs.CancelConfirmRoot = BuildCancelConfirm(refs.SetupRoot.transform.parent as RectTransform, onKeepSession, onConfirmCancel);
        refs.CancelConfirmRoot.SetActive(false);

        return refs;
    }

    private static void BuildSetupSection(
        FocusPanelLayoutRefs refs,
        RectTransform parent,
        Action onAddSkill,
        Action onStart,
        Action<float> onCustomDurationChanged,
        int[] durationPresets,
        Action<int> onDurationSelected,
        int selectedDurationMinutes)
    {
        refs.SetupEmptyText = FocusPanelViewUtility.CreateText(parent, "SetupEmptyText", string.Empty, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
        refs.SetupEmptyText.color = new Color(0.96f, 0.77f, 0.49f, 1f);
        refs.AddSkillButton = FocusPanelViewUtility.CreateButton(parent, "AddSkillButton", "Add Skill", () => onAddSkill?.Invoke());
        refs.AddSkillButton.gameObject.SetActive(false);
        FocusPanelViewUtility.CreateText(parent, "SkillLabel", "Choose a skill", 24f, FontStyles.Bold, TextAlignmentOptions.Left);
        refs.SkillListContent = FocusPanelViewUtility.CreateScrollContent(parent, "SkillList");
        FocusPanelViewUtility.CreateText(parent, "DurationLabel", "Choose a duration", 24f, FontStyles.Bold, TextAlignmentOptions.Left);

        GameObject durationRow = FocusPanelViewUtility.CreateObject("DurationRow", parent);
        HorizontalLayoutGroup rowLayout = durationRow.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;
        LayoutElement rowElement = durationRow.AddComponent<LayoutElement>();
        rowElement.preferredHeight = 76f;

        if (durationPresets != null)
        {
            for (int i = 0; i < durationPresets.Length; i++)
            {
                int minutes = durationPresets[i];
                FocusPanelViewUtility.CreateButton(durationRow.transform as RectTransform, $"Duration_{minutes}", $"{minutes} min", () => onDurationSelected?.Invoke(minutes));
            }
        }

        FocusPanelViewUtility.CreateText(parent, "CustomDurationLabel", "Custom duration", 22f, FontStyles.Bold, TextAlignmentOptions.Left);
        GameObject customDurationRoot = FocusPanelViewUtility.CreatePanel(parent, "CustomDurationRoot", new Color(0.1f, 0.13f, 0.2f, 0.82f), false);
        VerticalLayoutGroup customLayout = customDurationRoot.AddComponent<VerticalLayoutGroup>();
        customLayout.padding = new RectOffset(18, 18, 18, 18);
        customLayout.spacing = 8f;
        customLayout.childAlignment = TextAnchor.MiddleCenter;
        customLayout.childControlWidth = true;
        customLayout.childControlHeight = true;
        customLayout.childForceExpandWidth = true;
        customLayout.childForceExpandHeight = false;

        refs.CustomDurationValueText = FocusPanelViewUtility.CreateText(customDurationRoot.transform as RectTransform, "CustomDurationValueText", "15 min", 22f, FontStyles.Normal, TextAlignmentOptions.Center);
        GameObject sliderRoot = FocusPanelViewUtility.CreateObject("CustomDurationSliderRoot", customDurationRoot.transform as RectTransform);
        LayoutElement sliderLayout = sliderRoot.AddComponent<LayoutElement>();
        sliderLayout.preferredHeight = 42f;
        Image sliderBackground = sliderRoot.AddComponent<Image>();
        sliderBackground.color = new Color(1f, 1f, 1f, 0.08f);
        refs.CustomDurationSlider = sliderRoot.AddComponent<Slider>();
        refs.CustomDurationSlider.minValue = 5f;
        refs.CustomDurationSlider.maxValue = 120f;
        refs.CustomDurationSlider.wholeNumbers = true;
        refs.CustomDurationSlider.value = selectedDurationMinutes;
        refs.CustomDurationSlider.onValueChanged.AddListener(value => onCustomDurationChanged?.Invoke(value));

        refs.SetupSummaryText = FocusPanelViewUtility.CreateText(parent, "SetupSummaryText", string.Empty, 24f, FontStyles.Normal, TextAlignmentOptions.Center);
        refs.StartButton = FocusPanelViewUtility.CreateButton(parent, "StartButton", "Start Focus", () => onStart?.Invoke());
        refs.StartButtonText = refs.StartButton != null ? refs.StartButton.GetComponentInChildren<TextMeshProUGUI>() : null;
    }

    private static void BuildActiveSection(FocusPanelLayoutRefs refs, RectTransform parent, Action onPauseResume, Action onFinishEarly, Action onCancel)
    {
        refs.ActiveSkillText = FocusPanelViewUtility.CreateText(parent, "ActiveSkillText", string.Empty, 30f, FontStyles.Bold, TextAlignmentOptions.Center);
        refs.ActiveTimerText = FocusPanelViewUtility.CreateText(parent, "ActiveTimerText", "00:00", 56f, FontStyles.Bold, TextAlignmentOptions.Center);
        refs.ActiveStatusText = FocusPanelViewUtility.CreateText(parent, "ActiveStatusText", string.Empty, 24f, FontStyles.Normal, TextAlignmentOptions.Center);
        refs.PauseResumeButton = FocusPanelViewUtility.CreateButton(parent, "PauseResumeButton", "Pause", () => onPauseResume?.Invoke());
        refs.PauseResumeButtonText = refs.PauseResumeButton.GetComponentInChildren<TextMeshProUGUI>();
        refs.FinishEarlyButton = FocusPanelViewUtility.CreateButton(parent, "FinishEarlyButton", "Finish Early", () => onFinishEarly?.Invoke());
        refs.CancelButton = FocusPanelViewUtility.CreateButton(parent, "CancelButton", "Cancel", () => onCancel?.Invoke());
    }

    private static void BuildResultSection(FocusPanelLayoutRefs refs, RectTransform parent, Action onDone)
    {
        refs.ResultTitleText = FocusPanelViewUtility.CreateText(parent, "ResultTitleText", string.Empty, 32f, FontStyles.Bold, TextAlignmentOptions.Center);
        refs.ResultSubtitleText = FocusPanelViewUtility.CreateText(parent, "ResultSubtitleText", string.Empty, 22f, FontStyles.Normal, TextAlignmentOptions.Center);
        refs.ResultSkillText = FocusPanelViewUtility.CreateText(parent, "ResultSkillText", string.Empty, 28f, FontStyles.Bold, TextAlignmentOptions.Center);
        refs.ResultProgressText = FocusPanelViewUtility.CreateText(parent, "ResultProgressText", string.Empty, 24f, FontStyles.Normal, TextAlignmentOptions.Center);
        refs.ResultRewardText = FocusPanelViewUtility.CreateText(parent, "ResultRewardText", string.Empty, 24f, FontStyles.Normal, TextAlignmentOptions.Center);
        refs.ResultPetText = FocusPanelViewUtility.CreateText(parent, "ResultPetText", string.Empty, 22f, FontStyles.Normal, TextAlignmentOptions.Center);
        FocusPanelViewUtility.CreateButton(parent, "DoneButton", "Done", () => onDone?.Invoke());
    }

    private static GameObject BuildCancelConfirm(RectTransform parent, Action onKeepSession, Action onConfirmCancel)
    {
        GameObject overlay = FocusPanelViewUtility.CreatePanel(parent, "CancelConfirmRoot", new Color(0.02f, 0.03f, 0.06f, 0.78f), true);
        GameObject box = FocusPanelViewUtility.CreatePanel(overlay.transform as RectTransform, "CancelConfirmBox", new Color(0.17f, 0.2f, 0.3f, 1f), false);
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

        FocusPanelViewUtility.CreateText(box.transform as RectTransform, "CancelTitle", "Cancel this focus session?", 28f, FontStyles.Bold, TextAlignmentOptions.Center);
        FocusPanelViewUtility.CreateText(box.transform as RectTransform, "CancelBody", "Cancelling stops the session with no reward.", 22f, FontStyles.Normal, TextAlignmentOptions.Center);

        GameObject buttonsRow = FocusPanelViewUtility.CreateObject("CancelButtonsRow", box.transform as RectTransform);
        HorizontalLayoutGroup rowLayout = buttonsRow.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;
        FocusPanelViewUtility.CreateButton(buttonsRow.transform as RectTransform, "KeepButton", "Keep Session", () => onKeepSession?.Invoke());
        FocusPanelViewUtility.CreateButton(buttonsRow.transform as RectTransform, "ConfirmButton", "Confirm Cancel", () => onConfirmCancel?.Invoke());
        return overlay;
    }
}
