using NUnit.Framework;

public class SaveNormalizerSkillArchetypeTests
{
    [Test]
    public void Normalize_OldSkillWithLegacyIcon_AssignsArchetypeAndCanonicalIcon()
    {
        SaveData data = new SaveData
        {
            skillsData = new SkillsData
            {
                skills =
                {
                    new SkillEntry
                    {
                        id = "skill_old",
                        name = "Coding",
                        icon = "DEV",
                        totalSP = 120
                    }
                }
            }
        };

        SaveData normalized = SaveNormalizer.Normalize(data);
        SkillEntry skill = normalized.skillsData.skills[0];

        Assert.AreEqual(SaveNormalizer.CurrentSaveVersion, normalized.saveVersion);
        Assert.AreEqual(SkillArchetypeCatalog.Logic, skill.archetypeId);
        Assert.AreEqual("MTH", skill.icon);
        Assert.AreEqual(120, skill.totalSP);
    }

    [Test]
    public void Normalize_UnknownLegacyIcon_FallsBackToGeneral()
    {
        SaveData data = new SaveData
        {
            skillsData = new SkillsData
            {
                skills =
                {
                    new SkillEntry
                    {
                        id = "skill_unknown",
                        name = "Something",
                        icon = "???",
                        totalSP = 45
                    }
                }
            }
        };

        SaveData normalized = SaveNormalizer.Normalize(data);
        SkillEntry skill = normalized.skillsData.skills[0];

        Assert.AreEqual(SkillArchetypeCatalog.General, skill.archetypeId);
        Assert.AreEqual("SKL", skill.icon);
        Assert.AreEqual(45, skill.totalSP);
    }
}
