using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class SkillsPlayModeTests
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
    public IEnumerator MainScene_SkillsFlow_AddSelectFocusAndReturnHome_Works()
    {
        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        AppShellUI shell = Object.FindAnyObjectByType<AppShellUI>();
        SkillsPanelUI skillsPanel = Object.FindAnyObjectByType<SkillsPanelUI>();
        FocusPanelUI focusPanel = Object.FindAnyObjectByType<FocusPanelUI>();

        Assert.NotNull(manager);
        Assert.NotNull(shell);
        Assert.NotNull(skillsPanel);
        Assert.NotNull(focusPanel);

        manager.AddSkill("Coding", "DEV");
        manager.AddSkill("Art", "ART");
        manager.AddSkill("Music", "MSC");
        yield return null;

        Assert.True(shell.OpenSkills());
        yield return null;

        Assert.True(skillsPanel.IsPanelVisible());
        Assert.False(manager.workButton != null && manager.workButton.gameObject.activeSelf);
        Assert.GreaterOrEqual(manager.GetSkills().Count, 3);

        string selectedSkillId = manager.GetSkills()[0].id;
        Assert.True(manager.SetSelectedFocusSkill(selectedSkillId));
        Assert.True(shell.OpenFocus(selectedSkillId));
        yield return null;

        Assert.True(focusPanel.IsPanelVisible());
        Assert.True(skillsPanel.IsPanelVisible());
        Assert.False(shell.OpenHome(), "Shell navigation should stay blocked while Focus is open.");

        focusPanel.ForceClosePanel(false);
        yield return null;

        Assert.False(focusPanel.IsPanelVisible());
        Assert.True(skillsPanel.IsPanelVisible());

        Assert.True(shell.OpenHome());
        yield return null;

        Assert.False(skillsPanel.IsPanelVisible());
        Assert.False(focusPanel.IsPanelVisible());
        Assert.True(manager.workButton != null && manager.workButton.gameObject.activeSelf);
        Assert.False(skillsPanel.skillGainPopupRoot != null && skillsPanel.skillGainPopupRoot.activeSelf);
    }

    [UnityTest]
    public IEnumerator MainScene_RepeatingSkillsTab_ReturnsToHome()
    {
        AppShellUI shell = Object.FindAnyObjectByType<AppShellUI>();
        SkillsPanelUI skillsPanel = Object.FindAnyObjectByType<SkillsPanelUI>();
        GameManager manager = Object.FindAnyObjectByType<GameManager>();

        Assert.NotNull(shell);
        Assert.NotNull(skillsPanel);
        Assert.NotNull(manager);

        Assert.True(shell.OpenSkills());
        yield return null;

        Assert.True(skillsPanel.IsPanelVisible());
        Assert.False(manager.workButton != null && manager.workButton.gameObject.activeSelf);

        Assert.True(shell.OpenSkills());
        yield return null;

        Assert.False(skillsPanel.IsPanelVisible());
        Assert.True(manager.workButton != null && manager.workButton.gameObject.activeSelf);
    }
}
