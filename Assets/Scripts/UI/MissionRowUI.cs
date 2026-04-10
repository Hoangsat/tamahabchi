using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionRowUI : MonoBehaviour
{
    public Image backgroundImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI rewardText;
    public Image progressTrackImage;
    public Image progressFillImage;
    public Button primaryButton;
    public TextMeshProUGUI primaryButtonText;

    private VerticalLayoutGroup rootLayout;
    private RectTransform titleZone;
    private RectTransform statusZone;
    private RectTransform progressZone;
    private RectTransform rewardActionZone;
    private LayoutElement rootLayoutElement;
    private LayoutElement progressZoneLayout;

    private void Awake()
    {
        EnsureUiBuilt();
    }

    public void BindSkillMission(MissionEntryData mission, Action<string, bool> onToggleSelected, Action<string> onClaim)
    {
        EnsureUiBuilt();
        if (mission == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        titleText.text = mission.title;
        statusText.text = BuildSkillStatusLabel(mission);
        progressText.text = BuildProgressLabel(mission);
        rewardText.text = BuildRewardLabel(mission);

        float progress01 = GetProgress01(mission);
        SetProgress(progress01, true);

        primaryButton.onClick.RemoveAllListeners();
        primaryButton.interactable = true;

        if (mission.isClaimed)
        {
            primaryButtonText.text = "Claimed";
            primaryButton.interactable = false;
        }
        else if (mission.isCompleted)
        {
            primaryButtonText.text = "Claim";
            primaryButton.onClick.AddListener(() => onClaim?.Invoke(mission.missionId));
        }
        else if (mission.isSelected)
        {
            primaryButtonText.text = progress01 > 0f ? "Tracking" : "Tracked";
            primaryButton.onClick.AddListener(() => onToggleSelected?.Invoke(mission.missionId, false));
        }
        else
        {
            primaryButtonText.text = "Track";
            primaryButton.onClick.AddListener(() => onToggleSelected?.Invoke(mission.missionId, true));
        }

        ApplyVisuals(mission.isSelected, mission.isCompleted, mission.isClaimed, false);
    }

    public void BindRoutine(MissionEntryData mission, Action<string> onCompleteRoutine)
    {
        EnsureUiBuilt();
        if (mission == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        titleText.text = mission.title;
        statusText.text = BuildRoutineStatusLabel(mission);
        progressText.text = string.Empty;
        rewardText.text = BuildRewardLabel(mission);

        bool showProgress = mission.isCompleted || mission.isClaimed;
        SetProgress(showProgress ? 1f : 0f, showProgress);

        primaryButton.onClick.RemoveAllListeners();
        primaryButton.interactable = true;

        if (mission.isClaimed)
        {
            primaryButtonText.text = "Done";
            primaryButton.interactable = false;
        }
        else
        {
            primaryButtonText.text = "Complete";
            primaryButton.onClick.AddListener(() => onCompleteRoutine?.Invoke(mission.missionId));
        }

        ApplyVisuals(false, mission.isCompleted, mission.isClaimed, true);
    }

    private void EnsureUiBuilt()
    {
        backgroundImage = EnsureImageComponent(gameObject, new Color(0.11f, 0.17f, 0.26f, 0.98f));
        rootLayout = GetComponent<VerticalLayoutGroup>();
        if (rootLayout == null)
        {
            rootLayout = gameObject.AddComponent<VerticalLayoutGroup>();
        }

        rootLayout.padding = new RectOffset(28, 28, 24, 24);
        rootLayout.spacing = 12f;
        rootLayout.childAlignment = TextAnchor.UpperLeft;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        rootLayoutElement = GetComponent<LayoutElement>();
        if (rootLayoutElement == null)
        {
            rootLayoutElement = gameObject.AddComponent<LayoutElement>();
        }

        rootLayoutElement.minHeight = 250f;
        rootLayoutElement.preferredHeight = -1f;
        rootLayoutElement.flexibleHeight = 0f;

        titleZone = EnsureZone("TitleZone", 0f);
        statusZone = EnsureZone("StatusZone", 0f);
        progressZone = EnsureZone("ProgressZone", 0f);
        rewardActionZone = EnsureZone("RewardActionZone", 0f);

        titleText = EnsureText(titleZone, "TitleText", 30f, FontStyles.Bold, new Color(0.97f, 0.99f, 1f, 1f));
        titleText.textWrappingMode = TextWrappingModes.Normal;
        titleText.overflowMode = TextOverflowModes.Overflow;
        titleText.alignment = TextAlignmentOptions.Left;

        statusText = EnsureText(statusZone, "StatusText", 19f, FontStyles.Bold, new Color(0.79f, 0.88f, 0.98f, 0.95f));
        statusText.textWrappingMode = TextWrappingModes.NoWrap;
        statusText.overflowMode = TextOverflowModes.Overflow;

        progressText = EnsureText(progressZone, "ProgressText", 20f, FontStyles.Normal, new Color(0.9f, 0.95f, 1f, 0.95f));
        progressText.textWrappingMode = TextWrappingModes.NoWrap;
        progressText.overflowMode = TextOverflowModes.Overflow;

        progressTrackImage = EnsureProgressTrack(progressZone);
        progressFillImage = EnsureProgressFill(progressTrackImage.rectTransform);
        progressZoneLayout = progressZone.GetComponent<LayoutElement>();

        rewardText = EnsureText(rewardActionZone, "RewardText", 18f, FontStyles.Normal, new Color(0.84f, 0.9f, 0.97f, 0.88f));
        rewardText.textWrappingMode = TextWrappingModes.Normal;
        rewardText.overflowMode = TextOverflowModes.Overflow;

        primaryButton = EnsurePrimaryButton(rewardActionZone);
        primaryButtonText = primaryButton.GetComponentInChildren<TextMeshProUGUI>(true);
        ConfigureButtonText(primaryButtonText);
    }

    private RectTransform EnsureZone(string name, float minHeight)
    {
        GameObject zone = FindOrCreateChild(name);
        RectTransform rect = zone.GetComponent<RectTransform>();
        StretchRect(rect);

        VerticalLayoutGroup layout = zone.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = zone.AddComponent<VerticalLayoutGroup>();
        }

        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = zone.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = zone.AddComponent<ContentSizeFitter>();
        }

        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutElement layoutElement = zone.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = zone.AddComponent<LayoutElement>();
        }

        layoutElement.minHeight = minHeight;
        layoutElement.preferredHeight = -1f;
        layoutElement.flexibleHeight = 0f;
        return rect;
    }

    private TextMeshProUGUI EnsureText(RectTransform parent, string name, float fontSize, FontStyles fontStyle, Color color)
    {
        GameObject root = FindOrCreateChild(name, parent);
        RectTransform rect = root.GetComponent<RectTransform>();
        StretchRect(rect);

        TextMeshProUGUI text = root.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = root.AddComponent<TextMeshProUGUI>();
        }

        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAlignmentOptions.Left;
        text.enableAutoSizing = false;
        text.margin = Vector4.zero;

        LayoutElement layout = root.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = root.AddComponent<LayoutElement>();
        }

        layout.minHeight = fontSize + 6f;
        layout.preferredHeight = -1f;
        layout.flexibleHeight = 0f;
        return text;
    }

    private Image EnsureProgressTrack(RectTransform parent)
    {
        GameObject trackRoot = FindOrCreateChild("ProgressTrack", parent);
        RectTransform rect = trackRoot.GetComponent<RectTransform>();
        StretchRect(rect);

        Image track = EnsureImageComponent(trackRoot, new Color(1f, 1f, 1f, 0.08f));
        LayoutElement layout = trackRoot.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = trackRoot.AddComponent<LayoutElement>();
        }

        layout.minHeight = 14f;
        layout.preferredHeight = 14f;
        return track;
    }

    private Image EnsureProgressFill(RectTransform trackRect)
    {
        GameObject fillRoot = FindOrCreateChild("Fill", trackRect);
        RectTransform fillRect = fillRoot.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        return EnsureImageComponent(fillRoot, new Color(0.42f, 0.72f, 1f, 1f));
    }

    private Button EnsurePrimaryButton(RectTransform parent)
    {
        GameObject buttonRoot = FindOrCreateChild("PrimaryButton", parent);
        RectTransform rect = buttonRoot.GetComponent<RectTransform>();
        StretchRect(rect);

        LayoutElement layout = buttonRoot.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = buttonRoot.AddComponent<LayoutElement>();
        }

        layout.minHeight = 62f;
        layout.preferredHeight = 62f;

        Image image = EnsureImageComponent(buttonRoot, new Color(0.24f, 0.32f, 0.48f, 1f));
        image.type = Image.Type.Sliced;

        Button button = buttonRoot.GetComponent<Button>();
        if (button == null)
        {
            button = buttonRoot.AddComponent<Button>();
        }

        GameObject labelRoot = FindOrCreateChild("Label", rect);
        RectTransform labelRect = labelRoot.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelRoot.GetComponent<TextMeshProUGUI>();
        if (label == null)
        {
            label = labelRoot.AddComponent<TextMeshProUGUI>();
        }

        ConfigureButtonText(label);
        return button;
    }

    private void ConfigureButtonText(TextMeshProUGUI text)
    {
        if (text == null)
        {
            return;
        }

        text.fontSize = 21f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.enableAutoSizing = false;
        text.color = Color.white;
    }

    private void SetProgress(float progress01, bool showProgress)
    {
        float safe = Mathf.Clamp01(progress01);
        if (progressZoneLayout != null)
        {
            progressZoneLayout.ignoreLayout = !showProgress;
        }

        if (progressZone != null)
        {
            progressZone.gameObject.SetActive(showProgress);
        }

        if (!showProgress || progressFillImage == null)
        {
            return;
        }

        RectTransform fillRect = progressFillImage.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(safe, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
    }

    private void ApplyVisuals(bool isSelected, bool isCompleted, bool isClaimed, bool isRoutine)
    {
        backgroundImage.color = isClaimed
            ? new Color(0.14f, 0.17f, 0.22f, 0.98f)
            : isCompleted
                ? new Color(0.13f, 0.31f, 0.22f, 0.98f)
                : isSelected
                    ? new Color(0.12f, 0.25f, 0.42f, 0.98f)
                    : isRoutine
                        ? new Color(0.12f, 0.19f, 0.28f, 0.98f)
                        : new Color(0.11f, 0.17f, 0.26f, 0.98f);

        statusText.color = isClaimed
            ? new Color(0.72f, 0.77f, 0.84f, 1f)
            : isCompleted
                ? new Color(0.6f, 0.97f, 0.71f, 1f)
                : isSelected
                    ? new Color(0.54f, 0.86f, 1f, 1f)
                    : new Color(0.88f, 0.92f, 0.98f, 0.9f);

        if (progressFillImage != null)
        {
            progressFillImage.color = isClaimed
                ? new Color(0.62f, 0.67f, 0.76f, 1f)
                : isCompleted
                    ? new Color(0.5f, 0.93f, 0.61f, 1f)
                    : isSelected
                        ? new Color(0.44f, 0.76f, 1f, 1f)
                        : new Color(0.38f, 0.53f, 0.8f, 1f);
        }

        if (primaryButton != null)
        {
            Image buttonImage = primaryButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = isClaimed
                    ? new Color(0.24f, 0.28f, 0.34f, 1f)
                    : isCompleted
                        ? new Color(0.27f, 0.59f, 0.35f, 1f)
                        : isSelected
                            ? new Color(0.22f, 0.54f, 0.9f, 1f)
                            : new Color(0.24f, 0.32f, 0.48f, 1f);
            }
        }
    }

    private float GetProgress01(MissionEntryData mission)
    {
        if (mission == null)
        {
            return 0f;
        }

        if (mission.requiredMinutes > 0f)
        {
            return Mathf.Clamp01(mission.progressMinutes / Mathf.Max(1f, mission.requiredMinutes));
        }

        return Mathf.Clamp01(mission.currentProgress / Mathf.Max(1f, mission.targetProgress));
    }

    private string BuildSkillStatusLabel(MissionEntryData mission)
    {
        if (mission.isClaimed)
        {
            return "Claimed";
        }

        if (mission.isCompleted)
        {
            return "Completed";
        }

        if (!mission.isSelected)
        {
            return "Ready";
        }

        return GetProgress01(mission) > 0f ? "In Progress" : "Tracked";
    }

    private string BuildRoutineStatusLabel(MissionEntryData mission)
    {
        if (mission.isClaimed)
        {
            return "Claimed";
        }

        if (mission.isCompleted)
        {
            return "Completed";
        }

        return "Ready";
    }

    private string BuildProgressLabel(MissionEntryData mission)
    {
        if (mission.isClaimed)
        {
            return "Completed";
        }

        if (string.Equals(mission.skillMissionMode, "sessions", StringComparison.Ordinal))
        {
            return $"{mission.currentProgress}/{mission.targetProgress} sessions";
        }

        if (mission.requiredMinutes > 0f)
        {
            return $"{Mathf.Min(mission.progressMinutes, mission.requiredMinutes):0.#}/{mission.requiredMinutes:0.#} min";
        }

        return $"{mission.currentProgress}/{mission.targetProgress}";
    }

    private string BuildRewardLabel(MissionEntryData mission)
    {
        string label = $"+{mission.rewardCoins} Coins  +{mission.rewardXp} XP";
        if (mission.rewardSkillPercent > 0f)
        {
            label += $"  +{mission.rewardSkillPercent:0.#}% Skill";
        }

        if (mission.rewardMood > 0)
        {
            label += $"  +{mission.rewardMood} Mood";
        }

        if (mission.rewardEnergy > 0)
        {
            label += $"  +{mission.rewardEnergy} Energy";
        }

        return label;
    }

    private static void StretchRect(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static Image EnsureImageComponent(GameObject target, Color color)
    {
        Image image = target.GetComponent<Image>();
        if (image == null)
        {
            image = target.AddComponent<Image>();
        }

        image.color = color;
        return image;
    }

    private GameObject FindOrCreateChild(string objectName)
    {
        return FindOrCreateChild(objectName, transform as RectTransform);
    }

    private GameObject FindOrCreateChild(string objectName, RectTransform parent)
    {
        Transform child = parent != null ? parent.Find(objectName) : null;
        if (child != null)
        {
            return child.gameObject;
        }

        GameObject root = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return root;
    }
}
