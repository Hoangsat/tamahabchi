using System.IO;
using NUnit.Framework;
using UnityEngine;

public class SaveManagerTests
{
    [SetUp]
    public void SetUp()
    {
        new SaveManager().Reset();
    }

    [TearDown]
    public void TearDown()
    {
        new SaveManager().Reset();
    }

    [Test]
    public void Save_OverwritePreservesBackupAndReplacesMainAtomically()
    {
        SaveManager saveManager = new SaveManager();

        SaveData firstSave = SaveNormalizer.CreateDefault();
        firstSave.currencyData.coins = 12;
        saveManager.Save(firstSave);

        Assert.True(File.Exists(SavePaths.MainSaveFilePath));
        Assert.False(File.Exists(SavePaths.BackupSaveFilePath));

        SaveData secondSave = SaveNormalizer.CreateDefault();
        secondSave.currencyData.coins = 34;
        saveManager.Save(secondSave);

        Assert.True(File.Exists(SavePaths.MainSaveFilePath));
        Assert.True(File.Exists(SavePaths.BackupSaveFilePath));
        Assert.False(File.Exists(SavePaths.TempSaveFilePath));

        SaveData currentMain = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePaths.MainSaveFilePath));
        SaveData backupMain = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePaths.BackupSaveFilePath));

        Assert.NotNull(currentMain);
        Assert.NotNull(backupMain);
        Assert.AreEqual(34, currentMain.currencyData.coins);
        Assert.AreEqual(12, backupMain.currencyData.coins);
    }

    [Test]
    public void SaveLoad_RoundTripPreservesSkillArchetype()
    {
        SaveManager saveManager = new SaveManager();

        SaveData save = SaveNormalizer.CreateDefault();
        save.skillsData.skills.Add(new SkillEntry
        {
            id = "skill_arch",
            name = "Python",
            icon = "MSC",
            archetypeId = SkillArchetypeCatalog.Music,
            totalSP = 320
        });

        saveManager.Save(save);
        SaveData loaded = saveManager.Load();
        SkillEntry skill = loaded.skillsData.skills.Find(entry => entry != null && entry.id == "skill_arch");

        Assert.NotNull(skill);
        Assert.AreEqual(SkillArchetypeCatalog.Music, skill.archetypeId);
        Assert.AreEqual("MSC", skill.icon);
        Assert.AreEqual(320, skill.totalSP);
    }
}
