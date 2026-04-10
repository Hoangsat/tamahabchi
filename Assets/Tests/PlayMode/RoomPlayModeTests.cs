using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class RoomPlayModeTests
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
    public IEnumerator MainScene_RoomShellNavigation_Works()
    {
        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        AppShellUI shell = Object.FindAnyObjectByType<AppShellUI>();
        RoomPanelUI roomPanel = Object.FindAnyObjectByType<RoomPanelUI>();

        Assert.NotNull(manager);
        Assert.NotNull(shell);
        Assert.NotNull(roomPanel);

        Assert.True(shell.OpenRoom());
        yield return null;

        Assert.True(roomPanel.IsPanelVisible());
        Assert.False(manager.workButton != null && manager.workButton.gameObject.activeSelf);

        Assert.True(shell.OpenHome());
        yield return null;

        Assert.False(roomPanel.IsPanelVisible());
        Assert.True(manager.workButton != null && manager.workButton.gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator MainScene_HomeSkillsMissionsFocus_NavigationWorks()
    {
        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        AppShellUI shell = Object.FindAnyObjectByType<AppShellUI>();
        HomeDetailsPanelUI homeDetailsPanel = Object.FindAnyObjectByType<HomeDetailsPanelUI>();
        SkillsPanelUI skillsPanel = Object.FindAnyObjectByType<SkillsPanelUI>();
        MissionPanelUI missionPanel = Object.FindAnyObjectByType<MissionPanelUI>();
        FocusPanelUI focusPanel = Object.FindAnyObjectByType<FocusPanelUI>();

        Assert.NotNull(manager);
        Assert.NotNull(shell);
        Assert.NotNull(homeDetailsPanel);
        Assert.NotNull(skillsPanel);
        Assert.NotNull(missionPanel);
        Assert.NotNull(focusPanel);

        Assert.True(shell.OpenHome());
        yield return null;

        Assert.True(homeDetailsPanel.IsPanelVisible());
        Assert.False(manager.workButton != null && manager.workButton.gameObject.activeSelf);

        Assert.True(shell.OpenHome());
        yield return null;

        Assert.False(homeDetailsPanel.IsPanelVisible());
        Assert.True(manager.workButton != null && manager.workButton.gameObject.activeSelf);
        Assert.False(skillsPanel.IsPanelVisible());
        Assert.False(missionPanel.IsPanelVisible());
        Assert.False(focusPanel.IsPanelVisible());

        Assert.True(shell.OpenSkills());
        yield return null;

        Assert.True(skillsPanel.IsPanelVisible());
        Assert.False(missionPanel.IsPanelVisible());
        Assert.False(focusPanel.IsPanelVisible());
        Assert.False(manager.workButton != null && manager.workButton.gameObject.activeSelf);

        Assert.True(shell.OpenMissions());
        yield return null;

        Assert.False(skillsPanel.IsPanelVisible());
        Assert.True(missionPanel.IsPanelVisible());
        Assert.False(focusPanel.IsPanelVisible());

        Assert.True(shell.OpenFocus());
        yield return null;

        Assert.True(focusPanel.IsPanelVisible());
        Assert.False(shell.OpenHome(), "Shell navigation should be blocked while Focus is open.");

        focusPanel.ForceClosePanel(false);
        yield return null;

        Assert.False(focusPanel.IsPanelVisible());
        Assert.True(missionPanel.IsPanelVisible());

        Assert.True(shell.OpenHome());
        yield return null;

        Assert.False(skillsPanel.IsPanelVisible());
        Assert.False(missionPanel.IsPanelVisible());
        Assert.False(focusPanel.IsPanelVisible());
        Assert.True(manager.workButton != null && manager.workButton.gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator MainScene_RoomUpgrade_PersistsAfterSceneReload()
    {
        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        Assert.NotNull(manager);

        manager.currencyData.coins = 120;
        manager.progressionData.level = 4;
        manager.petData.isDead = false;
        manager.petData.hunger = 50f;
        manager.petData.mood = 50f;
        manager.petData.energy = 50f;

        Assert.True(manager.OpenRoomPanel());
        yield return null;

        bool success = manager.TryUpgradeRoomFromPanel(out string message);
        Assert.True(success, message);
        Assert.AreEqual(1, manager.roomData.roomLevel);
        manager.SaveGame();

        yield return SceneManager.LoadSceneAsync(MainSceneName, LoadSceneMode.Single);
        yield return null;

        GameManager reloadedManager = Object.FindAnyObjectByType<GameManager>();
        RoomPanelUI roomPanel = Object.FindAnyObjectByType<RoomPanelUI>();

        Assert.NotNull(reloadedManager);
        Assert.NotNull(roomPanel);
        Assert.AreEqual(1, reloadedManager.roomData.roomLevel);

        Assert.True(reloadedManager.OpenRoomPanel());
        yield return null;

        Assert.True(roomPanel.IsPanelVisible());
    }

    [UnityTest]
    public IEnumerator MainScene_RepeatingMissionsShopAndRoomTabs_ReturnsToHome()
    {
        AppShellUI shell = Object.FindAnyObjectByType<AppShellUI>();
        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        MissionPanelUI missionPanel = Object.FindAnyObjectByType<MissionPanelUI>();
        ShopPanelUI shopPanel = Object.FindAnyObjectByType<ShopPanelUI>();
        RoomPanelUI roomPanel = Object.FindAnyObjectByType<RoomPanelUI>();

        Assert.NotNull(shell);
        Assert.NotNull(manager);
        Assert.NotNull(missionPanel);
        Assert.NotNull(shopPanel);
        Assert.NotNull(roomPanel);

        Assert.True(shell.OpenMissions());
        yield return null;
        Assert.True(missionPanel.IsPanelVisible());
        Assert.True(shell.OpenMissions());
        yield return null;
        Assert.False(missionPanel.IsPanelVisible());
        Assert.True(manager.workButton != null && manager.workButton.gameObject.activeSelf);

        Assert.True(shell.OpenShop());
        yield return null;
        Assert.True(shopPanel.IsPanelVisible());
        Assert.True(shell.OpenShop());
        yield return null;
        Assert.False(shopPanel.IsPanelVisible());
        Assert.True(manager.workButton != null && manager.workButton.gameObject.activeSelf);

        Assert.True(shell.OpenRoom());
        yield return null;
        Assert.True(roomPanel.IsPanelVisible());
        Assert.True(shell.OpenRoom());
        yield return null;
        Assert.False(roomPanel.IsPanelVisible());
        Assert.True(manager.workButton != null && manager.workButton.gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator MainScene_HomeDetails_DoesNotBlockShellTabSwitch()
    {
        AppShellUI shell = Object.FindAnyObjectByType<AppShellUI>();
        HomeDetailsPanelUI homeDetailsPanel = Object.FindAnyObjectByType<HomeDetailsPanelUI>();
        SkillsPanelUI skillsPanel = Object.FindAnyObjectByType<SkillsPanelUI>();

        Assert.NotNull(shell);
        Assert.NotNull(homeDetailsPanel);
        Assert.NotNull(skillsPanel);

        Assert.True(shell.OpenHome());
        yield return null;
        Assert.True(homeDetailsPanel.IsPanelVisible());

        Assert.True(shell.OpenSkills());
        yield return null;

        Assert.False(homeDetailsPanel.IsPanelVisible(), "Home details should not keep blocking shell navigation.");
        Assert.True(skillsPanel.IsPanelVisible(), "Skills should open directly from Home details.");
    }
}
