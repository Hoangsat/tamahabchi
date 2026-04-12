using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public readonly struct MissionPanelPopupRefs
{
    public MissionPanelPopupRefs(
        GameObject root,
        TextMeshProUGUI titleText,
        TextMeshProUGUI statusText,
        GameObject skillRoot,
        GameObject routineRoot,
        RectTransform skillListContent,
        RectTransform routineSkillListContent,
        Slider skillMinutesSlider,
        TextMeshProUGUI skillMinutesValueText,
        TMP_InputField routineTitleInput,
        Slider routineCoinsSlider,
        Slider routineMoodSlider,
        Slider routineEnergySlider,
        Slider routineSkillSlider,
        TextMeshProUGUI routineCostText)
    {
        Root = root;
        TitleText = titleText;
        StatusText = statusText;
        SkillRoot = skillRoot;
        RoutineRoot = routineRoot;
        SkillListContent = skillListContent;
        RoutineSkillListContent = routineSkillListContent;
        SkillMinutesSlider = skillMinutesSlider;
        SkillMinutesValueText = skillMinutesValueText;
        RoutineTitleInput = routineTitleInput;
        RoutineCoinsSlider = routineCoinsSlider;
        RoutineMoodSlider = routineMoodSlider;
        RoutineEnergySlider = routineEnergySlider;
        RoutineSkillSlider = routineSkillSlider;
        RoutineCostText = routineCostText;
    }

    public GameObject Root { get; }
    public TextMeshProUGUI TitleText { get; }
    public TextMeshProUGUI StatusText { get; }
    public GameObject SkillRoot { get; }
    public GameObject RoutineRoot { get; }
    public RectTransform SkillListContent { get; }
    public RectTransform RoutineSkillListContent { get; }
    public Slider SkillMinutesSlider { get; }
    public TextMeshProUGUI SkillMinutesValueText { get; }
    public TMP_InputField RoutineTitleInput { get; }
    public Slider RoutineCoinsSlider { get; }
    public Slider RoutineMoodSlider { get; }
    public Slider RoutineEnergySlider { get; }
    public Slider RoutineSkillSlider { get; }
    public TextMeshProUGUI RoutineCostText { get; }
}

public static class MissionPanelPopupBuilder
{
    public static MissionPanelPopupRefs Build(
        RectTransform panelRoot,
        UnityAction onClose,
        UnityAction onConfirmCreate,
        UnityAction onSwitchToSkillMode,
        UnityAction onSwitchToRoutineMode)
    {
        GameObject overlayRoot = CreatePanel(panelRoot, "CreatePopupRoot", new Color(0.01f, 0.02f, 0.04f, 0.82f), true);
        RectTransform popupOverlayRect = overlayRoot.transform as RectTransform;
        popupOverlayRect.SetAsLastSibling();

        GameObject popupBox = CreatePanel(overlayRoot.transform as RectTransform, "CreatePopupBox", new Color(0.12f, 0.16f, 0.24f, 1f), false);
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

        TextMeshProUGUI popupTitleText = CreateText(popupTitleRow.transform as RectTransform, "PopupTitleText", "Create Skill Mission", 34f, FontStyles.Bold, TextAlignmentOptions.Left);
        LayoutElement popupTitleLayout = popupTitleText.gameObject.AddComponent<LayoutElement>();
        popupTitleLayout.flexibleWidth = 1f;
        popupTitleText.textWrappingMode = TextWrappingModes.NoWrap;
        popupTitleText.overflowMode = TextOverflowModes.Overflow;

        Button popupCloseButton = CreateActionButton(popupTitleRow.transform as RectTransform, "PopupCloseButton", "Close", onClose);
        ApplyButtonSizing(popupCloseButton, 170f, 56f, false);

        GameObject popupModeRow = CreateObject("PopupModeRow", popupHeader.transform as RectTransform);
        HorizontalLayoutGroup popupModeLayout = popupModeRow.AddComponent<HorizontalLayoutGroup>();
        popupModeLayout.spacing = 12f;
        popupModeLayout.childControlWidth = true;
        popupModeLayout.childControlHeight = true;
        popupModeLayout.childForceExpandWidth = true;
        popupModeLayout.childForceExpandHeight = false;

        Button popupSkillModeButton = CreateActionButton(popupModeRow.transform as RectTransform, "SkillModeButton", "Skill Mission", onSwitchToSkillMode);
        Button popupRoutineModeButton = CreateActionButton(popupModeRow.transform as RectTransform, "RoutineModeButton", "Routine", onSwitchToRoutineMode);
        ApplyButtonSizing(popupSkillModeButton, 0f, 60f, true);
        ApplyButtonSizing(popupRoutineModeButton, 0f, 60f, true);

        TextMeshProUGUI popupStatusText = CreateText(popupBox.transform as RectTransform, "PopupStatusText", string.Empty, 18f, FontStyles.Normal, TextAlignmentOptions.Left);
        GameObject popupSkillRoot = CreateColumn(popupBox.transform as RectTransform, "PopupSkillRoot");
        GameObject popupRoutineRoot = CreateColumn(popupBox.transform as RectTransform, "PopupRoutineRoot");

        RectTransform popupSkillListContent;
        Slider popupSkillMinutesSlider;
        TextMeshProUGUI popupSkillMinutesValueText;
        BuildSkillPopup(
            popupSkillRoot.transform as RectTransform,
            out popupSkillListContent,
            out popupSkillMinutesSlider,
            out popupSkillMinutesValueText);

        TMP_InputField popupRoutineTitleInput;
        Slider popupRoutineCoinsSlider;
        Slider popupRoutineMoodSlider;
        Slider popupRoutineEnergySlider;
        Slider popupRoutineSkillSlider;
        TextMeshProUGUI popupRoutineCostText;
        RectTransform popupRoutineSkillListContent;
        BuildRoutinePopup(
            popupRoutineRoot.transform as RectTransform,
            out popupRoutineTitleInput,
            out popupRoutineCoinsSlider,
            out popupRoutineMoodSlider,
            out popupRoutineEnergySlider,
            out popupRoutineSkillSlider,
            out popupRoutineCostText,
            out popupRoutineSkillListContent);

        Button popupCreateButton = CreateActionButton(popupBox.transform as RectTransform, "PopupCreateButton", "Create", onConfirmCreate);
        ApplyButtonSizing(popupCreateButton, 0f, 64f, true);
        overlayRoot.SetActive(false);

        return new MissionPanelPopupRefs(
            overlayRoot,
            popupTitleText,
            popupStatusText,
            popupSkillRoot,
            popupRoutineRoot,
            popupSkillListContent,
            popupRoutineSkillListContent,
            popupSkillMinutesSlider,
            popupSkillMinutesValueText,
            popupRoutineTitleInput,
            popupRoutineCoinsSlider,
            popupRoutineMoodSlider,
            popupRoutineEnergySlider,
            popupRoutineSkillSlider,
            popupRoutineCostText);
    }

    private static void BuildSkillPopup(
        RectTransform parent,
        out RectTransform popupSkillListContent,
        out Slider popupSkillMinutesSlider,
        out TextMeshProUGUI popupSkillMinutesValueText)
    {
        CreateText(parent, "SkillHint", "Choose the skill and duration for this mission.", 22f, FontStyles.Normal, TextAlignmentOptions.Left);
        popupSkillListContent = CreateScrollContent(parent, "PopupSkillList");
        ApplyPreferredHeight(popupSkillListContent.parent as RectTransform, 260f);
        TextMeshProUGUI minutesValueText = CreateText(parent, "PopupSkillMinutesValue", "30 min", 24f, FontStyles.Bold, TextAlignmentOptions.Left);
        popupSkillMinutesValueText = minutesValueText;
        popupSkillMinutesSlider = CreateSlider(parent, "PopupSkillMinutesSlider", 15f, 120f, 30f, true, value =>
        {
            minutesValueText.text = $"{Mathf.RoundToInt(value)} min";
        });
    }

    private static void BuildRoutinePopup(
        RectTransform parent,
        out TMP_InputField popupRoutineTitleInput,
        out Slider popupRoutineCoinsSlider,
        out Slider popupRoutineMoodSlider,
        out Slider popupRoutineEnergySlider,
        out Slider popupRoutineSkillSlider,
        out TextMeshProUGUI popupRoutineCostText,
        out RectTransform popupRoutineSkillListContent)
    {
        CreateText(parent, "RoutineHint", "Create a routine with an instant reward bundle.", 22f, FontStyles.Normal, TextAlignmentOptions.Left);
        popupRoutineTitleInput = CreateInput(parent, "PopupRoutineTitleInput", "Routine name (max 30)");
        popupRoutineTitleInput.characterLimit = 30;
        popupRoutineCoinsSlider = CreateSliderWithLabel(parent, "PopupRoutineCoinsSlider", 0f, 100f, 20f, true, "Coins");
        popupRoutineMoodSlider = CreateSliderWithLabel(parent, "PopupRoutineMoodSlider", 0f, 20f, 3f, true, "Mood");
        popupRoutineEnergySlider = CreateSliderWithLabel(parent, "PopupRoutineEnergySlider", 0f, 20f, 3f, true, "Care");
        popupRoutineSkillSlider = CreateSliderWithLabel(parent, "PopupRoutineSkillSlider", 0f, 300f, 0f, true, "Skill SP");
        popupRoutineCostText = CreateText(parent, "PopupRoutineCostText", string.Empty, 22f, FontStyles.Bold, TextAlignmentOptions.Left);
        CreateText(parent, "PopupRoutineSkillTargetLabel", "Skill reward target", 22f, FontStyles.Bold, TextAlignmentOptions.Left);
        popupRoutineSkillListContent = CreateScrollContent(parent, "PopupRoutineSkillList");
        ApplyPreferredHeight(popupRoutineSkillListContent.parent as RectTransform, 220f);
    }

    private static RectTransform CreateScrollContent(RectTransform parent, string name)
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
        ScrollRectPerformanceHelper.Optimize(root, scrollRect);
        return contentRect;
    }

    private static GameObject CreateColumn(RectTransform parent, string name)
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

    private static Button CreateActionButton(RectTransform parent, string name, string label, UnityAction action)
    {
        GameObject root = CreatePanel(parent, name, new Color(0.24f, 0.33f, 0.5f, 1f), false);
        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.preferredHeight = 60f;
        Button button = root.AddComponent<Button>();
        if (action != null)
        {
            button.onClick.AddListener(action);
        }

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

    private static Slider CreateSliderWithLabel(RectTransform parent, string name, float min, float max, float value, bool wholeNumbers, string label)
    {
        TextMeshProUGUI valueText = CreateText(parent, $"{name}_Label", string.Empty, 20f, FontStyles.Bold, TextAlignmentOptions.Left);
        return CreateSlider(parent, name, min, max, value, wholeNumbers, sliderValue =>
        {
            string formatted = wholeNumbers ? Mathf.RoundToInt(sliderValue).ToString() : sliderValue.ToString("0.#");
            valueText.text = $"{label}: {formatted}";
        });
    }

    private static Slider CreateSlider(RectTransform parent, string name, float min, float max, float value, bool wholeNumbers, UnityAction<float> onChanged)
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

    private static TMP_InputField CreateInput(RectTransform parent, string name, string placeholderText)
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

    private static void ApplyButtonSizing(Button button, float preferredWidth, float preferredHeight, bool flexibleWidth)
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

    private static void ApplyPreferredHeight(RectTransform rect, float height)
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

    private static TextMeshProUGUI CreateText(RectTransform parent, string name, string value, float size, FontStyles style, TextAlignmentOptions alignment)
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

    private static GameObject CreatePanel(RectTransform parent, string name, Color color, bool stretch)
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

    private static GameObject CreateObject(string name, RectTransform parent)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        return root;
    }
}
