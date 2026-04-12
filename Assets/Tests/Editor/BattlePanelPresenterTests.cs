using NUnit.Framework;
using UnityEngine;

public class BattlePanelPresenterTests
{
    [Test]
    public void BuildPlayerPreview_NoSkills_ReturnsGuidanceCopy()
    {
        BattlePlayerPreviewViewData view = BattlePanelPresenter.BuildPlayerPreview(new BattlePlayerPreviewData(), null);

        Assert.AreEqual("Player Battle Power: 0", view.PowerText);
        StringAssert.Contains("No combat skills yet.", view.SkillsText);
    }

    [Test]
    public void BuildPlayerPreview_FormatsSkillRowsWithFallbackIcon()
    {
        BattlePlayerPreviewData preview = new BattlePlayerPreviewData
        {
            playerBattlePower = 240.5f,
            combatSkills =
            {
                new BattleCombatSkillSnapshotData
                {
                    name = "Coding",
                    icon = string.Empty,
                    level = 4,
                    effectiveSP = 300,
                    axisPercent = 73.2f
                }
            }
        };

        BattlePlayerPreviewViewData view = BattlePanelPresenter.BuildPlayerPreview(preview, null);

        Assert.AreEqual("Player Battle Power: 240.5", view.PowerText);
        StringAssert.Contains("* Coding", view.SkillsText);
        StringAssert.Contains("Axis 73.2%", view.SkillsText);
    }

    [Test]
    public void BuildBossDetail_WithBoss_FormatsMetaAndRadarState()
    {
        BossDefinitionData boss = new BossDefinitionData
        {
            id = "boss_01",
            name = "Sprout",
            targetLevel = 2,
            bossPower = 120,
            rewardCoins = 36,
            difficultyTier = "Easy",
            visualWebValues01 = new System.Collections.Generic.List<float> { 0.2f, 0.5f, 0.8f }
        };

        BattleBossDetailViewData view = BattlePanelPresenter.BuildBossDetail(
            boss,
            new BattleAvailabilityData
            {
                trackedSkillCount = 3,
                requiredSkillCount = 3,
                currentEnergy = 100f,
                requiredEnergy = 10f
            });

        Assert.AreEqual("Sprout", view.NameText);
        StringAssert.Contains("Target Lv.2", view.MetaText);
        StringAssert.Contains("+36 Coins", view.MetaText);
        Assert.AreEqual("Boss Power: 120", view.PowerText);
        Assert.True(view.CanFight);
        Assert.AreEqual("Fight (-10 EN)", view.FightButtonText);
        Assert.NotNull(view.RadarValues);
        Assert.NotNull(view.RadarColors);
        Assert.AreEqual(3, view.RadarColors.Count);
    }

    [Test]
    public void BuildResult_WinResult_UsesWinSummary()
    {
        BattleResultViewData view = BattlePanelPresenter.BuildResult(
            new BattleResultData
            {
                bossId = "boss_02",
                bossName = "Boss 2",
                bossPower = 180,
                playerBattlePower = 240.5f,
                result = BattleOutcome.Win,
                rewardCoins = 44,
                energyCost = 10f,
                adviceMessage = "Next target: Boss 3 at 516 power."
            },
            new BossDefinitionData { id = "boss_02", name = "Boss 2" },
            null);

        StringAssert.Contains("VICTORY vs Boss 2", view.Text);
        StringAssert.Contains("Player 240.5 vs Boss 180", view.Text);
        StringAssert.Contains("Rewards +44 Coins", view.Text);
        Assert.AreEqual(new Color(0.48f, 0.92f, 0.62f, 1f), view.Color);
    }

    [Test]
    public void BuildResult_WhenBlocked_UsesBlockedCopy()
    {
        BattleResultViewData view = BattlePanelPresenter.BuildResult(
            null,
            new BossDefinitionData { id = "boss_01", name = "Boss 1" },
            new BattleAvailabilityData
            {
                trackedSkillCount = 2,
                requiredSkillCount = 3,
                currentEnergy = 100f,
                requiredEnergy = 10f,
                blockedReason = "Track at least 3 skills before fighting (2/3).",
                guidance = "Add and grow more tracked skills."
            });

        StringAssert.Contains("Cannot challenge Boss 1 yet.", view.Text);
        StringAssert.Contains("Track at least 3 skills", view.Text);
        Assert.AreEqual(new Color(1f, 0.84f, 0.45f, 1f), view.Color);
    }

    [Test]
    public void BuildResult_LossResult_UsesLossSummaryAndNoRewardCopy()
    {
        BattleResultViewData view = BattlePanelPresenter.BuildResult(
            new BattleResultData
            {
                bossId = "boss_03",
                bossName = "Boss 3",
                bossPower = 516,
                playerBattlePower = 240.5f,
                result = BattleOutcome.Loss,
                rewardCoins = 0,
                energyCost = 10f,
                adviceMessage = "Grow another tracked skill before retrying."
            },
            new BossDefinitionData { id = "boss_03", name = "Boss 3" },
            null);

        StringAssert.Contains("LOSS vs Boss 3", view.Text);
        StringAssert.Contains("No reward this round.", view.Text);
        StringAssert.Contains("Grow another tracked skill", view.Text);
        Assert.AreEqual(new Color(1f, 0.58f, 0.48f, 1f), view.Color);
    }
}
