using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class GameUiShellCoordinatorTests
{
    [Test]
    public void OpenSkills_WithAppShell_UsesShellNavigation()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        try
        {
            AppShellUI shell = canvasObject.AddComponent<AppShellUI>();
            SkillsPanelUI skillsPanel = canvasObject.AddComponent<SkillsPanelUI>();
            skillsPanel.panelRoot = new GameObject("SkillsPanelRoot");
            skillsPanel.panelRoot.SetActive(false);

            GameUiShellCoordinator coordinator = new GameUiShellCoordinator(
                null,
                null,
                shell,
                skillsPanel,
                null,
                null,
                null,
                null,
                null,
                null);
            coordinator.BindDependencies();

            Assert.True(coordinator.OpenSkills());
            Assert.True(skillsPanel.IsPanelVisible());
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void OpenBattle_WithoutShell_FallsBackToPanel()
    {
        GameObject root = new GameObject("Root", typeof(RectTransform));
        try
        {
            BattlePanelUI battlePanel = root.AddComponent<BattlePanelUI>();
            battlePanel.panelRoot = new GameObject("BattlePanelRoot", typeof(RectTransform));
            battlePanel.panelRoot.SetActive(false);
            MethodInfo awakeMethod = typeof(BattlePanelUI).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(awakeMethod);
            awakeMethod.Invoke(battlePanel, null);

            GameUiShellCoordinator coordinator = new GameUiShellCoordinator(
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                battlePanel,
                null,
                null);

            Assert.True(coordinator.OpenBattle());
            Assert.True(battlePanel.IsPanelVisible());
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
