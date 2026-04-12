using System.Collections.Generic;
using UnityEngine;

public readonly struct SkillsChartEntryViewData
{
    public SkillsChartEntryViewData(SkillEntry skill, Color color)
    {
        Skill = skill;
        Color = color;
    }

    public SkillEntry Skill { get; }
    public Color Color { get; }
}

public readonly struct SkillsChartSummaryViewData
{
    public SkillsChartSummaryViewData(string titleText, string emptyStateText, bool showLabels)
    {
        TitleText = titleText ?? string.Empty;
        EmptyStateText = emptyStateText ?? string.Empty;
        ShowLabels = showLabels;
    }

    public string TitleText { get; }
    public string EmptyStateText { get; }
    public bool ShowLabels { get; }
}

public readonly struct SkillsResponsiveProfile
{
    public SkillsResponsiveProfile(
        float fontScale,
        float labelScale,
        float headerScale,
        float heightScale,
        float buttonWidthScale,
        float spacingScale,
        float paddingScale,
        float chartHeightScale,
        float inputHeightScale,
        float iconRowHeightScale,
        float buttonHeightScale)
    {
        FontScale = fontScale;
        LabelScale = labelScale;
        HeaderScale = headerScale;
        HeightScale = heightScale;
        ButtonWidthScale = buttonWidthScale;
        SpacingScale = spacingScale;
        PaddingScale = paddingScale;
        ChartHeightScale = chartHeightScale;
        InputHeightScale = inputHeightScale;
        IconRowHeightScale = iconRowHeightScale;
        ButtonHeightScale = buttonHeightScale;
    }

    public float FontScale { get; }
    public float LabelScale { get; }
    public float HeaderScale { get; }
    public float HeightScale { get; }
    public float ButtonWidthScale { get; }
    public float SpacingScale { get; }
    public float PaddingScale { get; }
    public float ChartHeightScale { get; }
    public float InputHeightScale { get; }
    public float IconRowHeightScale { get; }
    public float ButtonHeightScale { get; }
}

public readonly struct SkillsHeroVisualViewData
{
    public SkillsHeroVisualViewData(Color accentColor, Color backgroundColor, Color badgeColor, Color buttonColor, string iconText, float iconBaseFontSize)
    {
        AccentColor = accentColor;
        BackgroundColor = backgroundColor;
        BadgeColor = badgeColor;
        ButtonColor = buttonColor;
        IconText = iconText ?? string.Empty;
        IconBaseFontSize = iconBaseFontSize;
    }

    public Color AccentColor { get; }
    public Color BackgroundColor { get; }
    public Color BadgeColor { get; }
    public Color ButtonColor { get; }
    public string IconText { get; }
    public float IconBaseFontSize { get; }
}

public readonly struct SkillsGainPopupViewData
{
    public SkillsGainPopupViewData(string iconText, string messageText)
    {
        IconText = iconText ?? string.Empty;
        MessageText = messageText ?? string.Empty;
    }

    public string IconText { get; }
    public string MessageText { get; }
}

public static class SkillsPanelPresenter
{
    private static readonly Color EmptyHeroAccentColor = new Color(0.32f, 0.39f, 0.54f, 1f);
    private static readonly Color HeroBackgroundBaseColor = new Color(0.14f, 0.18f, 0.27f, 0.98f);
    private static readonly Color HeroBadgeBaseColor = new Color(0.18f, 0.23f, 0.34f, 1f);
    private static readonly Color HeroButtonBaseColor = new Color(0.20f, 0.46f, 0.32f, 0.98f);
    private static readonly Color EmptyHeroButtonColor = new Color(0.22f, 0.44f, 0.32f, 0.98f);
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

    public static List<SkillsChartEntryViewData> BuildChartEntries(List<SkillEntry> skills, int limit)
    {
        List<SkillsChartEntryViewData> chartEntries = new List<SkillsChartEntryViewData>();
        if (skills == null || skills.Count == 0 || limit <= 0)
        {
            return chartEntries;
        }

        int clampedLimit = Mathf.Min(limit, skills.Count);
        for (int i = 0; i < clampedLimit; i++)
        {
            SkillEntry skill = skills[i];
            if (skill == null)
            {
                continue;
            }

            chartEntries.Add(new SkillsChartEntryViewData(skill, GetColorForSkill(skill)));
        }

        return chartEntries;
    }

    public static SkillsChartSummaryViewData BuildChartSummary(List<SkillEntry> skills, int minimumSkillsForRadar)
    {
        int totalSkills = skills != null ? skills.Count : 0;
        int skillsNeeded = Mathf.Max(0, minimumSkillsForRadar - totalSkills);

        string titleText = totalSkills >= minimumSkillsForRadar
            ? string.Empty
            : $"{Mathf.Min(totalSkills, minimumSkillsForRadar)}/{minimumSkillsForRadar} ready";

        string emptyStateText;
        if (totalSkills <= 0)
        {
            emptyStateText = "Create your first skill to start the radar.\nThe chart unlocks once you track 3 skills.";
        }
        else if (skillsNeeded > 0)
        {
            string suffix = skillsNeeded == 1 ? string.Empty : "s";
            emptyStateText =
                $"Add {skillsNeeded} more skill{suffix} to unlock the radar.\n" +
                "Your tracked skills are still active below.";
        }
        else
        {
            emptyStateText = string.Empty;
        }

        return new SkillsChartSummaryViewData(titleText, emptyStateText, totalSkills >= minimumSkillsForRadar);
    }

    public static SkillsResponsiveProfile BuildResponsiveProfile(float canvasHeight)
    {
        bool compact = canvasHeight > 0f && canvasHeight < 900f;
        bool veryCompact = canvasHeight > 0f && canvasHeight < 760f;

        return new SkillsResponsiveProfile(
            veryCompact ? 0.88f : compact ? 0.94f : 1f,
            veryCompact ? 0.82f : compact ? 0.90f : 1f,
            veryCompact ? 0.84f : compact ? 0.92f : 1f,
            veryCompact ? 0.86f : compact ? 0.93f : 1f,
            veryCompact ? 0.90f : compact ? 0.95f : 1f,
            veryCompact ? 0.80f : compact ? 0.90f : 1f,
            veryCompact ? 0.78f : compact ? 0.88f : 1f,
            veryCompact ? 0.84f : compact ? 0.92f : 1f,
            veryCompact ? 0.90f : compact ? 0.95f : 1f,
            veryCompact ? 0.90f : compact ? 0.94f : 1f,
            veryCompact ? 0.90f : compact ? 0.95f : 1f);
    }

    public static SkillsHeroVisualViewData BuildHeroVisual(SkillsHeroState heroState)
    {
        SkillEntry heroSkill = heroState.HeroSkill;
        Color accentColor = heroSkill != null ? GetColorForSkill(heroSkill) : EmptyHeroAccentColor;
        string iconText = heroSkill == null || string.IsNullOrWhiteSpace(heroSkill.icon) ? "SKL" : heroSkill.icon.Trim();

        return new SkillsHeroVisualViewData(
            accentColor,
            Color.Lerp(HeroBackgroundBaseColor, accentColor, 0.22f),
            Color.Lerp(HeroBadgeBaseColor, accentColor, 0.42f),
            heroSkill == null ? EmptyHeroButtonColor : Color.Lerp(HeroButtonBaseColor, accentColor, 0.18f),
            iconText,
            iconText.Length > 2 ? 28f : 38f);
    }

    public static SkillsGainPopupViewData BuildGainPopup(SkillEntry skill, SkillProgressionViewData view, SkillProgressResult result)
    {
        if (skill == null || view == null || result == null)
        {
            return new SkillsGainPopupViewData("SKL", string.Empty);
        }

        string messageText = $"{skill.name} +{result.deltaSP} SP";
        if (result.leveledUp)
        {
            messageText += $" | Lv.{result.previousLevel} -> Lv.{result.newLevel}";
        }
        else if (view.isGolden)
        {
            messageText += " | MAX";
        }

        return new SkillsGainPopupViewData(
            string.IsNullOrEmpty(skill.icon) ? "SKL" : skill.icon,
            messageText);
    }

    public static float GetRadarLabelOffset(int axisCount, SkillsResponsiveProfile profile)
    {
        if (axisCount <= 4)
        {
            return 28f * profile.LabelScale;
        }

        if (axisCount <= 6)
        {
            return 22f * profile.LabelScale;
        }

        if (axisCount >= 10)
        {
            return 24f * profile.LabelScale;
        }

        if (axisCount >= 8)
        {
            return 21f * profile.LabelScale;
        }

        return 18f * profile.LabelScale;
    }

    public static float GetRadarLabelFontSize(int axisCount, float templateFontSize, SkillsResponsiveProfile profile)
    {
        float baseSize = templateFontSize * profile.FontScale;
        if (axisCount <= 4)
        {
            return Mathf.Max(11f, baseSize + 2f);
        }

        if (axisCount <= 6)
        {
            return Mathf.Max(10f, baseSize + 1f);
        }

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

    public static int GetChartSkillIndex(List<SkillsChartEntryViewData> chartEntries, string skillId)
    {
        for (int i = 0; i < chartEntries.Count; i++)
        {
            if (chartEntries[i].Skill != null && chartEntries[i].Skill.id == skillId)
            {
                return i;
            }
        }

        return -1;
    }

    public static bool TryGetChartColor(string skillId, List<SkillsChartEntryViewData> chartEntries, out Color color)
    {
        for (int i = 0; i < chartEntries.Count; i++)
        {
            if (chartEntries[i].Skill != null && chartEntries[i].Skill.id == skillId)
            {
                color = chartEntries[i].Color;
                return true;
            }
        }

        color = Color.white;
        return false;
    }

    public static string GetRadarLabel(SkillEntry skill, int axisCount)
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
        if (axisCount <= 4)
        {
            if (string.IsNullOrEmpty(skill.icon))
            {
                return shortName;
            }

            return string.IsNullOrEmpty(shortName) ? skill.icon : $"{skill.icon} {shortName}";
        }

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

    public static Color GetColorForSkill(SkillEntry skill)
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

    private static Color GetColorForIndex(int index)
    {
        return ChartPalette[index % ChartPalette.Length];
    }
}
