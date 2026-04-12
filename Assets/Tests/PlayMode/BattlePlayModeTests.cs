using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class BattlePlayModeTests
{
    private const string MainSceneName = "Main";

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        new SaveManager().Reset();
        yield return SceneManager.LoadSceneAsync(MainSceneName, LoadSceneMode.Single);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        new SaveManager().Reset();
        yield return null;
    }

    [UnityTest]
    public IEnumerator MainScene_HomeBattleFight_Works()
    {
        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        AppShellUI shell = Object.FindAnyObjectByType<AppShellUI>();
        BattlePanelUI battlePanel = Object.FindAnyObjectByType<BattlePanelUI>();

        Assert.NotNull(manager);
        Assert.NotNull(shell);
        Assert.NotNull(battlePanel);

        SkillEntry coding = manager.AddSkill("Coding", "DEV");
        SkillEntry art = manager.AddSkill("Art", "ART");
        SkillEntry music = manager.AddSkill("Music", "MSC");
        coding.totalSP = 100;
        art.totalSP = 100;
        music.totalSP = 100;

        yield return null;

        Button battleButton = FindButtonByName("BattleButton");
        Assert.NotNull(battleButton);
        battleButton.onClick.Invoke();
        yield return null;

        Assert.True(battlePanel.IsPanelVisible());
        Assert.False(manager.feedButton != null && manager.feedButton.gameObject.activeSelf);
        Assert.AreEqual(10, CountButtonsWithPrefix("BossButton_"));

        Button bossOneButton = FindButtonByName("BossButton_0");
        Assert.NotNull(bossOneButton);
        bossOneButton.onClick.Invoke();
        yield return null;

        int coinsBefore = manager.GetCurrentCoins();
        float energyBefore = manager.petData.energy;

        Assert.NotNull(battlePanel.fightButton);
        battlePanel.fightButton.onClick.Invoke();
        yield return null;

        Assert.NotNull(battlePanel.resultText);
        Assert.True(battlePanel.resultText.text.Contains("WIN"));
        Assert.True(battlePanel.resultText.text.Contains("Rewards +30 Coins"));
        Assert.AreEqual(energyBefore - 10f, manager.petData.energy, 0.01f);
        Assert.Greater(manager.GetCurrentCoins(), coinsBefore);
        Assert.AreEqual(1, manager.progressionData.level);
        Assert.AreEqual(0, manager.progressionData.xp);
        Assert.True(shell.OpenHome());
        yield return null;

        Assert.False(battlePanel.IsPanelVisible());
        Assert.True(manager.feedButton != null && manager.feedButton.gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator MainScene_BattleWithTwoSkills_UsesAvailableSkills()
    {
        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        BattlePanelUI battlePanel = Object.FindAnyObjectByType<BattlePanelUI>();

        Assert.NotNull(manager);
        Assert.NotNull(battlePanel);

        SkillEntry coding = manager.AddSkill("Coding", "DEV");
        SkillEntry art = manager.AddSkill("Art", "ART");
        coding.totalSP = 100;
        art.totalSP = 260;

        yield return null;

        Assert.True(manager.OpenBattlePanel());
        yield return null;

        BattlePlayerPreviewData preview = manager.GetBattlePlayerPreview();
        Assert.AreEqual(2, preview.combatSkills.Count);
        Assert.AreEqual(180f, preview.playerBattlePower, 0.01f);
        Assert.True(battlePanel.playerSkillsText.text.Contains("Coding"));
        Assert.True(battlePanel.playerSkillsText.text.Contains("Art"));
        Assert.False(battlePanel.fightButton.interactable);
        Assert.True(battlePanel.resultText.text.Contains("Track at least 3 skills"));
    }

    [UnityTest]
    public IEnumerator MainScene_AddingWeakFourthSkill_DoesNotChangeBattlePowerWhenTopThreeStaySame()
    {
        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        BattlePanelUI battlePanel = Object.FindAnyObjectByType<BattlePanelUI>();

        Assert.NotNull(manager);
        Assert.NotNull(battlePanel);

        SkillEntry coding = manager.AddSkill("Coding", "DEV");
        SkillEntry art = manager.AddSkill("Art", "ART");
        SkillEntry music = manager.AddSkill("Music", "MSC");
        coding.totalSP = 3000;
        art.totalSP = 2500;
        music.totalSP = 2000;

        yield return null;

        Assert.True(manager.OpenBattlePanel());
        yield return null;

        float beforePower = manager.GetBattlePlayerPreview().playerBattlePower;
        BattleOutcome beforeOutcome = manager.ResolveBattle("boss_05").result;

        SkillEntry dance = manager.AddSkill("Dance", "DNC");
        Assert.NotNull(dance);
        dance.totalSP = 0;
        yield return null;

        float afterPower = manager.GetBattlePlayerPreview().playerBattlePower;
        BattleOutcome afterOutcome = manager.ResolveBattle("boss_05").result;

        Assert.AreEqual(beforePower, afterPower, 0.01f);
        Assert.AreEqual(beforeOutcome, afterOutcome);
        Assert.True(battlePanel.playerSkillsText.text.Contains("Coding"));
        Assert.False(battlePanel.playerSkillsText.text.Contains("Dance"));
    }

    [UnityTest]
    public IEnumerator MainScene_BattleWithLowEnergy_ShowsEnergyGate()
    {
        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        BattlePanelUI battlePanel = Object.FindAnyObjectByType<BattlePanelUI>();

        Assert.NotNull(manager);
        Assert.NotNull(battlePanel);

        manager.AddSkill("Coding", "DEV").totalSP = 100;
        manager.AddSkill("Art", "ART").totalSP = 100;
        manager.AddSkill("Music", "MSC").totalSP = 100;
        manager.petData.energy = 5f;

        yield return null;

        Assert.True(manager.OpenBattlePanel());
        yield return null;

        Button bossOneButton = FindButtonByName("BossButton_0");
        Assert.NotNull(bossOneButton);
        bossOneButton.onClick.Invoke();
        yield return null;

        Assert.False(battlePanel.fightButton.interactable);
        Assert.True(battlePanel.resultText.text.Contains("Need 10 energy"));
    }

    private Button FindButtonByName(string name)
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].name == name)
            {
                return buttons[i];
            }
        }

        return null;
    }

    private int CountButtonsWithPrefix(string prefix)
    {
        int count = 0;
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].name.StartsWith(prefix))
            {
                count++;
            }
        }

        return count;
    }
}
