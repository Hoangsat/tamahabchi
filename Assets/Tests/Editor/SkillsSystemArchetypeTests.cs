using NUnit.Framework;

public class SkillsSystemArchetypeTests
{
    [Test]
    public void AddSkillWithArchetype_StoresArchetypeAndCanonicalIcon()
    {
        SkillsSystem skillsSystem = new SkillsSystem();
        skillsSystem.Init(new SkillsData());

        SkillEntry skill = skillsSystem.AddSkillWithArchetype("Python", SkillArchetypeCatalog.Logic);

        Assert.NotNull(skill);
        Assert.AreEqual(SkillArchetypeCatalog.Logic, skill.archetypeId);
        Assert.AreEqual("MTH", skill.icon);
    }

    [Test]
    public void LegacyAddSkill_ResolvesArchetypeFromIconAndRewritesCanonicalIcon()
    {
        SkillsSystem skillsSystem = new SkillsSystem();
        skillsSystem.Init(new SkillsData());

        SkillEntry skill = skillsSystem.AddSkill("Coding", "DEV");

        Assert.NotNull(skill);
        Assert.AreEqual(SkillArchetypeCatalog.Logic, skill.archetypeId);
        Assert.AreEqual("MTH", skill.icon);
    }

    [Test]
    public void ChangeSkillArchetype_DoesNotAlterProgression()
    {
        SkillsData data = new SkillsData
        {
            skills =
            {
                new SkillEntry
                {
                    id = "skill_music",
                    name = "Music",
                    icon = "MSC",
                    archetypeId = SkillArchetypeCatalog.Music,
                    totalSP = 900,
                    totalFocusMinutes = 55
                }
            }
        };

        SkillsSystem skillsSystem = new SkillsSystem();
        skillsSystem.Init(data);

        SkillProgressionViewData before = skillsSystem.GetSkillProgressionView("skill_music");
        bool changed = skillsSystem.ChangeSkillArchetype("skill_music", SkillArchetypeCatalog.Expression);
        SkillProgressionViewData after = skillsSystem.GetSkillProgressionView("skill_music");
        SkillEntry skill = skillsSystem.GetSkillById("skill_music");

        Assert.True(changed);
        Assert.AreEqual(SkillArchetypeCatalog.Expression, skill.archetypeId);
        Assert.AreEqual("DNC", skill.icon);
        Assert.AreEqual(before.totalSP, after.totalSP);
        Assert.AreEqual(before.level, after.level);
        Assert.AreEqual(before.axisPercent, after.axisPercent);
        Assert.AreEqual(55, skill.totalFocusMinutes);
    }
}
