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
        Assert.False(manager.feedButton != null && manager.feedButton.gameObject.activeSelf);
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
        Assert.True(manager.feedButton != null && manager.feedButton.gameObject.activeSelf);
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
        Assert.False(manager.feedButton != null && manager.feedButton.gameObject.activeSelf);

        Assert.True(shell.OpenSkills());
        yield return null;

        Assert.False(skillsPanel.IsPanelVisible());
        Assert.True(manager.feedButton != null && manager.feedButton.gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator MainScene_FocusLifecycle_StartPauseResumeCancel_Works()
    {
        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        AppShellUI shell = Object.FindAnyObjectByType<AppShellUI>();
        FocusPanelUI focusPanel = Object.FindAnyObjectByType<FocusPanelUI>();

        Assert.NotNull(manager);
        Assert.NotNull(shell);
        Assert.NotNull(focusPanel);

        SkillEntry skill = manager.AddSkill("Coding", "DEV");
        Assert.NotNull(skill);
        Assert.True(manager.SetSelectedFocusSkill(skill.id));
        yield return null;

        Assert.True(shell.OpenFocus(skill.id));
        yield return null;

        Assert.True(focusPanel.IsPanelVisible());
        Assert.True(manager.TryStartFocusSession(skill.id, 15));
        yield return null;

        FocusSessionSnapshot runningSnapshot = manager.GetFocusSessionSnapshot();
        Assert.AreEqual(FocusSessionState.Running, runningSnapshot.state);
        Assert.AreEqual(skill.id, runningSnapshot.skillId);
        Assert.Greater(runningSnapshot.configuredDurationSeconds, 0f);

        Assert.True(manager.PauseFocusSession());
        yield return null;

        FocusSessionSnapshot pausedSnapshot = manager.GetFocusSessionSnapshot();
        Assert.AreEqual(FocusSessionState.Paused, pausedSnapshot.state);

        Assert.True(manager.ResumeFocusSession());
        yield return null;

        FocusSessionSnapshot resumedSnapshot = manager.GetFocusSessionSnapshot();
        Assert.AreEqual(FocusSessionState.Running, resumedSnapshot.state);

        Assert.True(manager.CancelFocusSession());
        yield return null;

        FocusSessionSnapshot cancelledSnapshot = manager.GetFocusSessionSnapshot();
        Assert.AreEqual(FocusSessionState.Idle, cancelledSnapshot.state);
        Assert.False(cancelledSnapshot.HasActiveSession());

        focusPanel.ForceClosePanel(false);
        yield return null;

        Assert.False(focusPanel.IsPanelVisible());
        Assert.True(shell.OpenHome());
        yield return null;
        Assert.True(manager.feedButton != null && manager.feedButton.gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator MainScene_FocusSession_PersistsAcrossSceneReload()
    {
        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        AppShellUI shell = Object.FindAnyObjectByType<AppShellUI>();
        FocusPanelUI focusPanel = Object.FindAnyObjectByType<FocusPanelUI>();

        Assert.NotNull(manager);
        Assert.NotNull(shell);
        Assert.NotNull(focusPanel);

        SkillEntry skill = manager.AddSkill("Coding", "DEV");
        Assert.NotNull(skill);
        Assert.True(manager.SetSelectedFocusSkill(skill.id));
        yield return null;

        Assert.True(shell.OpenFocus(skill.id));
        yield return null;

        Assert.True(manager.TryStartFocusSession(skill.id, 15));
        yield return null;

        FocusSessionSnapshot beforeSave = manager.GetFocusSessionSnapshot();
        Assert.AreEqual(FocusSessionState.Running, beforeSave.state);
        Assert.AreEqual(skill.id, beforeSave.skillId);

        manager.SaveGame();

        yield return SceneManager.LoadSceneAsync(MainSceneName, LoadSceneMode.Single);
        yield return null;

        GameManager reloadedManager = Object.FindAnyObjectByType<GameManager>();
        FocusPanelUI reloadedFocusPanel = Object.FindAnyObjectByType<FocusPanelUI>();

        Assert.NotNull(reloadedManager);
        Assert.NotNull(reloadedFocusPanel);
        Assert.AreEqual(skill.id, reloadedManager.GetSelectedFocusSkill());

        FocusSessionSnapshot afterReload = reloadedManager.GetFocusSessionSnapshot();
        Assert.True(afterReload.HasActiveSession());
        Assert.AreEqual(FocusSessionState.Running, afterReload.state);
        Assert.AreEqual(skill.id, afterReload.skillId);
        Assert.Greater(afterReload.remainingSeconds, 0f);
        Assert.LessOrEqual(afterReload.remainingSeconds, beforeSave.remainingSeconds + 1f);

        Assert.True(reloadedManager.OpenFocusPanel(skill.id));
        yield return null;

        Assert.True(reloadedFocusPanel.IsPanelVisible());
    }
}
