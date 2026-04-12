using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class SkillsPanelCoordinatorTests
{
    [Test]
    public void GetSnapshot_ReturnsSkillsViewsAndSelectedId()
    {
        GameObject managerObject = new GameObject("GameManager");
        try
        {
            GameManager manager = managerObject.AddComponent<GameManager>();

            SkillsSystem skillsSystem = new SkillsSystem();
            skillsSystem.Init(new SkillsData
            {
                skills =
                {
                    new SkillEntry { id = "skill_alpha", name = "Alpha", icon = "DEV", totalSP = 200 },
                    new SkillEntry { id = "skill_beta", name = "Beta", icon = "ART", totalSP = 350 }
                }
            });

            typeof(GameManager)
                .GetField("skillsSystem", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(manager, skillsSystem);

            SkillsPanelCoordinator coordinator = new SkillsPanelCoordinator(manager);
            SkillsPanelSnapshot snapshot = coordinator.GetSnapshot();

            Assert.AreEqual(2, snapshot.Skills.Count);
            Assert.AreEqual(2, snapshot.SkillViews.Count);
            Assert.AreEqual(string.Empty, snapshot.SelectedSkillId);
            Assert.AreEqual("skill_alpha", snapshot.Skills[0].id);
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void AddSkill_WhenDuplicateNameExists_ReturnsDuplicateMessage()
    {
        GameObject managerObject = new GameObject("GameManager");
        try
        {
            GameManager manager = managerObject.AddComponent<GameManager>();

            SkillsSystem skillsSystem = new SkillsSystem();
            skillsSystem.Init(new SkillsData
            {
                skills =
                {
                    new SkillEntry { id = "skill_existing", name = "Coding", icon = "DEV", totalSP = 100 }
                }
            });

            typeof(GameManager)
                .GetField("skillsSystem", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(manager, skillsSystem);

            SkillsPanelCoordinator coordinator = new SkillsPanelCoordinator(manager);
            SkillsPanelActionResult result = coordinator.AddSkill(" coding ", "ART");

            Assert.False(result.Success);
            Assert.AreEqual("Skill already exists", result.Message);
            Assert.IsNull(result.Skill);
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void GetHeroState_UsesSelectedSkillWhenAvailable()
    {
        GameObject managerObject = new GameObject("GameManager");
        try
        {
            GameManager manager = managerObject.AddComponent<GameManager>();

            SkillsSystem skillsSystem = new SkillsSystem();
            skillsSystem.Init(new SkillsData
            {
                skills =
                {
                    new SkillEntry { id = "skill_alpha", name = "Alpha", icon = "DEV", totalSP = 800, totalFocusMinutes = 30 },
                    new SkillEntry { id = "skill_beta", name = "Beta", icon = "ART", totalSP = 200, totalFocusMinutes = 10 }
                }
            });

            typeof(GameManager)
                .GetField("skillsSystem", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(manager, skillsSystem);

            FocusCoordinator focusCoordinator = new FocusCoordinator(
                null,
                skillsSystem,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                new FocusCoordinatorCallbacks());
            typeof(GameManager)
                .GetField("focusCoordinator", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(manager, focusCoordinator);
            Assert.True(manager.SetSelectedFocusSkill("skill_beta"));

            SkillsPanelCoordinator coordinator = new SkillsPanelCoordinator(manager);
            SkillsHeroState heroState = coordinator.GetHeroState(coordinator.GetSnapshot());

            Assert.NotNull(heroState.HeroSkill);
            Assert.AreEqual("skill_beta", heroState.HeroSkill.id);
            Assert.True(heroState.UsingSelectedSkill);
            Assert.AreEqual("Current Focus", heroState.HeroLabel);
            Assert.AreEqual("Start Focus", heroState.ActionText);
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void AddSkill_WithArchetype_CreatesSkillWithCanonicalIcon()
    {
        GameObject managerObject = new GameObject("GameManager");
        try
        {
            GameManager manager = managerObject.AddComponent<GameManager>();

            SkillsData data = new SkillsData();
            SkillsSystem skillsSystem = new SkillsSystem();
            skillsSystem.Init(data);
            manager.skillsData = data;

            typeof(GameManager)
                .GetField("skillsSystem", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(manager, skillsSystem);

            SkillsPanelCoordinator coordinator = new SkillsPanelCoordinator(manager);
            SkillsPanelActionResult result = coordinator.AddSkill("Python", SkillArchetypeCatalog.Logic);

            Assert.True(result.Success);
            Assert.NotNull(result.Skill);
            Assert.AreEqual(SkillArchetypeCatalog.Logic, result.Skill.archetypeId);
            Assert.AreEqual("MTH", result.Skill.icon);
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void ChangeSkillArchetype_PreservesProgressionData()
    {
        GameObject managerObject = new GameObject("GameManager");
        try
        {
            GameManager manager = managerObject.AddComponent<GameManager>();

            SkillsData data = new SkillsData
            {
                skills =
                {
                    new SkillEntry
                    {
                        id = "skill_code",
                        name = "Coding",
                        icon = "DEV",
                        archetypeId = SkillArchetypeCatalog.Productivity,
                        totalSP = 680,
                        totalFocusMinutes = 120
                    }
                }
            };
            SkillsSystem skillsSystem = new SkillsSystem();
            skillsSystem.Init(data);
            manager.skillsData = data;

            typeof(GameManager)
                .GetField("skillsSystem", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(manager, skillsSystem);

            SkillsPanelCoordinator coordinator = new SkillsPanelCoordinator(manager);
            SkillProgressionViewData before = skillsSystem.GetSkillProgressionView("skill_code");

            SkillsPanelActionResult result = coordinator.ChangeSkillArchetype("skill_code", SkillArchetypeCatalog.Music);
            SkillEntry updatedSkill = skillsSystem.GetSkillById("skill_code");
            SkillProgressionViewData after = skillsSystem.GetSkillProgressionView("skill_code");

            Assert.True(result.Success);
            Assert.AreEqual(SkillArchetypeCatalog.Music, updatedSkill.archetypeId);
            Assert.AreEqual("MSC", updatedSkill.icon);
            Assert.AreEqual(before.totalSP, after.totalSP);
            Assert.AreEqual(before.level, after.level);
            Assert.AreEqual(before.axisPercent, after.axisPercent);
            Assert.AreEqual(120, updatedSkill.totalFocusMinutes);
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
        }
    }
}
