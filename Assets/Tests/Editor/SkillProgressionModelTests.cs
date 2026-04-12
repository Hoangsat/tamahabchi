using NUnit.Framework;
using UnityEngine;

public class SkillProgressionModelTests
{
    [Test]
    public void ZeroSp_StartsAtLevelZeroAxisZero()
    {
        Assert.AreEqual(0, SkillProgressionModel.GetLevel(0));
        Assert.AreEqual(0f, SkillProgressionModel.GetAxisPercent(0), 0.001f);
        Assert.AreEqual(0, SkillProgressionModel.GetProgressInLevel(0));
        Assert.AreEqual(100, SkillProgressionModel.GetRequiredSPForNextLevel(0));
    }

    [Test]
    public void AxisPercent_UsesLevelBands()
    {
        int totalSp = SkillProgressionModel.GetTotalSPForLevelStart(3) + Mathf.RoundToInt(410f * 0.7f);

        Assert.AreEqual(3, SkillProgressionModel.GetLevel(totalSp));
        Assert.AreEqual(37f, SkillProgressionModel.GetAxisPercent(totalSp), 0.6f);
    }

    [Test]
    public void ApplySkillPoints_CanJumpMultipleLevels()
    {
        SkillsSystem skillsSystem = new SkillsSystem();
        skillsSystem.Init(new SkillsData
        {
            skills = { new SkillEntry { id = "skill_math", name = "Math", totalSP = 0 } }
        });

        SkillProgressResult result = skillsSystem.ApplySkillPoints("skill_math", 300, 1800f, "2026-04-11T10:00:00.0000000Z", 0.05f);

        Assert.True(result.success);
        Assert.True(result.leveledUp);
        Assert.GreaterOrEqual(result.levelsGained, 2);
        Assert.Greater(result.newAxisPercent, 20f);
    }

    [TestCase(0f)]
    [TestCase(9.9f)]
    [TestCase(10f)]
    [TestCase(37f)]
    [TestCase(99.9f)]
    [TestCase(100f)]
    public void SaveNormalizer_MigratesLegacyPercentToBandAlignedAxis(float legacyPercent)
    {
        SaveData normalized = SaveNormalizer.Normalize(new SaveData
        {
            skillsData = new SkillsData
            {
                skills =
                {
                    new SkillEntry
                    {
                        id = "skill_legacy",
                        name = "Legacy",
                        percent = legacyPercent
                    }
                }
            }
        });

        SkillEntry migrated = normalized.skillsData.skills[0];
        float migratedAxis = SkillProgressionModel.GetAxisPercent(migrated.totalSP);

        float expectedAxis = legacyPercent >= 100f ? 100f : legacyPercent;

        Assert.AreEqual(expectedAxis, migratedAxis, 0.6f);
        Assert.AreEqual(legacyPercent >= 100f, migrated.isGolden);
    }
}
