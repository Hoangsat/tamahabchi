using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class SkillsPanelChartUtility
{
    public static void RebuildRows(
        RectTransform skillsListContainer,
        SkillRowUI skillRowTemplate,
        List<SkillRowUI> spawnedRows,
        List<SkillEntry> skills,
        List<SkillProgressionViewData> skillViews,
        string selectedSkillId,
        List<SkillsChartEntryViewData> chartSkills,
        string highlightedSkillId,
        Action<string> onSelectSkill,
        Action<string> onChangeSkillType,
        Action<string> onRemoveSkill)
    {
        if (skillsListContainer == null || skillRowTemplate == null)
        {
            return;
        }

        if (skillRowTemplate.gameObject.activeSelf)
        {
            skillRowTemplate.gameObject.SetActive(false);
        }

        for (int i = 0; i < skills.Count; i++)
        {
            Color markerColor = SkillsPanelPresenter.TryGetChartColor(skills[i].id, chartSkills, out Color chartColor)
                ? chartColor
                : new Color(0.33f, 0.38f, 0.48f, 0.65f);

            SkillRowUI row = SkillsPanelViewUtility.EnsureInstance(spawnedRows, i, skillRowTemplate, skillsListContainer);
            if (row == null)
            {
                continue;
            }

            SkillProgressionViewData view = SkillsPanelViewUtility.FindProgressionView(skillViews, skills[i].id);
            if (view == null)
            {
                continue;
            }

            row.gameObject.SetActive(true);
            row.transform.SetSiblingIndex(i + 1);
            row.Bind(
                skills[i],
                view,
                markerColor,
                skills[i].id == selectedSkillId,
                SkillArchetypeCatalog.GetDisplayName(skills[i].archetypeId),
                onSelectSkill,
                onChangeSkillType,
                onRemoveSkill);

            if (!string.IsNullOrEmpty(highlightedSkillId) && skills[i].id == highlightedSkillId)
            {
                row.PlayHighlight();
            }
        }

        for (int i = skills.Count; i < spawnedRows.Count; i++)
        {
            if (spawnedRows[i] == null)
            {
                continue;
            }

            spawnedRows[i].ResetRowState();
            spawnedRows[i].gameObject.SetActive(false);
        }
    }

    public static void RebuildChart(
        RadarChartGraphic radarChartGraphic,
        RectTransform radarLabelsRoot,
        TextMeshProUGUI radarLabelTemplate,
        TextMeshProUGUI chartEmptyStateText,
        List<TextMeshProUGUI> spawnedRadarLabels,
        List<SkillsChartEntryViewData> chartSkills,
        List<SkillProgressionViewData> skillViews,
        string highlightedSkillId,
        SkillsResponsiveProfile responsiveProfile,
        int minimumSkillsForRadar)
    {
        if (chartEmptyStateText != null)
        {
            chartEmptyStateText.gameObject.SetActive(chartSkills.Count < minimumSkillsForRadar);
        }

        if (radarChartGraphic == null)
        {
            return;
        }

        if (chartSkills.Count < minimumSkillsForRadar)
        {
            radarChartGraphic.SetValues(null, null);
            SkillsPanelViewUtility.HideUnused(spawnedRadarLabels, 0);
            return;
        }

        List<float> values = new List<float>(chartSkills.Count);
        List<Color> colors = new List<Color>(chartSkills.Count);
        int highlightedChartIndex = -1;

        for (int i = 0; i < chartSkills.Count; i++)
        {
            SkillProgressionViewData view = SkillsPanelViewUtility.FindProgressionView(skillViews, chartSkills[i].Skill.id);
            values.Add(view != null ? Mathf.Clamp01(view.axisFill01) : 0f);
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

        RebuildRadarLabels(radarLabelsRoot, radarLabelTemplate, spawnedRadarLabels, chartSkills, responsiveProfile, minimumSkillsForRadar);
    }

    public static void UpdateChartPresentation(
        TextMeshProUGUI chartTitleText,
        TextMeshProUGUI chartEmptyStateText,
        RectTransform radarLabelsRoot,
        List<SkillEntry> skills,
        int minimumSkillsForRadar)
    {
        SkillsChartSummaryViewData chartSummary = SkillsPanelPresenter.BuildChartSummary(skills, minimumSkillsForRadar);

        if (chartTitleText != null)
        {
            chartTitleText.text = chartSummary.TitleText;
        }

        if (chartEmptyStateText != null)
        {
            chartEmptyStateText.text = chartSummary.EmptyStateText;
        }

        if (radarLabelsRoot != null)
        {
            radarLabelsRoot.gameObject.SetActive(chartSummary.ShowLabels);
        }
    }

    private static void RebuildRadarLabels(
        RectTransform radarLabelsRoot,
        TextMeshProUGUI radarLabelTemplate,
        List<TextMeshProUGUI> spawnedRadarLabels,
        List<SkillsChartEntryViewData> chartSkills,
        SkillsResponsiveProfile responsiveProfile,
        int minimumSkillsForRadar)
    {
        if (radarLabelsRoot == null || radarLabelTemplate == null || chartSkills.Count < minimumSkillsForRadar)
        {
            SkillsPanelViewUtility.HideUnused(spawnedRadarLabels, 0);
            return;
        }

        float radius = Mathf.Min(radarLabelsRoot.rect.width, radarLabelsRoot.rect.height) * 0.5f - 4f;
        float labelRadius = radius + SkillsPanelPresenter.GetRadarLabelOffset(chartSkills.Count, responsiveProfile);
        float fontSize = SkillsPanelPresenter.GetRadarLabelFontSize(chartSkills.Count, radarLabelTemplate.fontSize, responsiveProfile);

        for (int i = 0; i < chartSkills.Count; i++)
        {
            TextMeshProUGUI label = SkillsPanelViewUtility.EnsureInstance(spawnedRadarLabels, i, radarLabelTemplate, radarLabelsRoot);
            if (label == null)
            {
                continue;
            }

            label.gameObject.SetActive(true);
            label.text = SkillsPanelPresenter.GetRadarLabel(chartSkills[i].Skill, chartSkills.Count);
            label.color = chartSkills[i].Color;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = fontSize;

            RectTransform labelTransform = label.rectTransform;
            labelTransform.anchorMin = new Vector2(0.5f, 0.5f);
            labelTransform.anchorMax = new Vector2(0.5f, 0.5f);
            labelTransform.pivot = new Vector2(0.5f, 0.5f);
            labelTransform.anchoredPosition = SkillsPanelViewUtility.GetChartPoint(i, chartSkills.Count, labelRadius);
        }

        SkillsPanelViewUtility.HideUnused(spawnedRadarLabels, chartSkills.Count);
    }
}
