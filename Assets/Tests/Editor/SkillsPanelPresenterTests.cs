using System.Collections.Generic;
using NUnit.Framework;

public class SkillsPanelPresenterTests
{
    [Test]
    public void BuildChartEntries_PreservesSourceOrderForStableAxes()
    {
        List<SkillEntry> source = new List<SkillEntry>
        {
            new SkillEntry { id = "skill_alpha", name = "Alpha", totalSP = 50 },
            new SkillEntry { id = "skill_beta", name = "Beta", totalSP = 900 },
            new SkillEntry { id = "skill_gamma", name = "Gamma", totalSP = 250 }
        };

        List<SkillsChartEntryViewData> result = SkillsPanelPresenter.BuildChartEntries(source, 12);

        Assert.AreEqual(3, result.Count);
        CollectionAssert.AreEqual(new[] { "skill_alpha", "skill_beta", "skill_gamma" }, new[]
        {
            result[0].Skill.id,
            result[1].Skill.id,
            result[2].Skill.id
        });
    }

    [Test]
    public void BuildChartSummary_WithTwoSkills_ShowsUnlockGuidance()
    {
        List<SkillEntry> source = new List<SkillEntry>
        {
            new SkillEntry { id = "skill_one", name = "One", icon = "DEV", totalSP = 100 },
            new SkillEntry { id = "skill_two", name = "Two", icon = "ART", totalSP = 200 }
        };

        SkillsChartSummaryViewData summary = SkillsPanelPresenter.BuildChartSummary(source, 3);

        StringAssert.Contains("2/3", summary.TitleText);
        StringAssert.Contains("Add 1 more skill", summary.EmptyStateText);
        Assert.False(summary.ShowLabels);
    }

    [Test]
    public void BuildHeroVisual_WithMissingSkill_UsesFallbackIconAndButtonColor()
    {
        SkillsHeroVisualViewData visual = SkillsPanelPresenter.BuildHeroVisual(default);

        Assert.AreEqual("SKL", visual.IconText);
        Assert.AreEqual(28f, visual.IconBaseFontSize);
        Assert.AreEqual(new UnityEngine.Color(0.22f, 0.44f, 0.32f, 0.98f), visual.ButtonColor);
    }

    [Test]
    public void BuildGainPopup_WhenLeveledUp_FormatsLevelTransition()
    {
        SkillEntry skill = new SkillEntry { id = "skill_focus", name = "Focus", icon = "DEV" };
        SkillProgressionViewData view = new SkillProgressionViewData
        {
            isGolden = false
        };
        SkillProgressResult result = new SkillProgressResult
        {
            deltaSP = 25,
            leveledUp = true,
            previousLevel = 1,
            newLevel = 2
        };

        SkillsGainPopupViewData popup = SkillsPanelPresenter.BuildGainPopup(skill, view, result);

        Assert.AreEqual("DEV", popup.IconText);
        StringAssert.Contains("Focus +25 SP", popup.MessageText);
        StringAssert.Contains("Lv.1 -> Lv.2", popup.MessageText);
    }

    [Test]
    public void BuildResponsiveProfile_ForVeryCompactHeight_UsesCompactScales()
    {
        SkillsResponsiveProfile profile = SkillsPanelPresenter.BuildResponsiveProfile(700f);

        Assert.AreEqual(0.88f, profile.FontScale);
        Assert.AreEqual(0.82f, profile.LabelScale);
        Assert.AreEqual(0.84f, profile.HeaderScale);
        Assert.AreEqual(0.84f, profile.ChartHeightScale);
    }

    [Test]
    public void GetRadarLabelFontSize_ForDenseChart_ClampsReadableMinimum()
    {
        SkillsResponsiveProfile profile = SkillsPanelPresenter.BuildResponsiveProfile(700f);

        float fontSize = SkillsPanelPresenter.GetRadarLabelFontSize(10, 8f, profile);

        Assert.AreEqual(7f, fontSize);
    }
}
