using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class HomeDetailsPanelTests
{
    [Test]
    public void OpenHomeTwice_TogglesHomeDetailsPanel()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        try
        {
            AppShellUI shell = canvasObject.AddComponent<AppShellUI>();
            HomeDetailsPanelUI homeDetails = canvasObject.AddComponent<HomeDetailsPanelUI>();

            shell.SetDependencies(null, null, null, null, null, null, null, homeDetails);

            Assert.True(shell.OpenHome());
            Assert.True(homeDetails.IsPanelVisible());

            Assert.True(shell.OpenHome());
            Assert.False(homeDetails.IsPanelVisible());
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void HomeDetailsPanel_ExposesRequestedTabLabels()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        try
        {
            HomeDetailsPanelUI panel = canvasObject.AddComponent<HomeDetailsPanelUI>();
            panel.ShowPanel();

            Assert.AreEqual("Pet", panel.GetSelectedTabLabel());

            panel.SelectTab(HomeDetailsTab.Stats);
            Assert.AreEqual("Stats", panel.GetSelectedTabLabel());

            panel.SelectTab(HomeDetailsTab.Relic);
            Assert.AreEqual("Relic", panel.GetSelectedTabLabel());

            panel.SelectTab(HomeDetailsTab.Mastery);
            Assert.AreEqual("Mastery", panel.GetSelectedTabLabel());

            panel.SelectTab(HomeDetailsTab.Trait);
            Assert.AreEqual("Trait", panel.GetSelectedTabLabel());
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void HomeDetailsPanel_DoesNotPreventSwitchingToAnotherSection()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        try
        {
            AppShellUI shell = canvasObject.AddComponent<AppShellUI>();
            HomeDetailsPanelUI homeDetails = canvasObject.AddComponent<HomeDetailsPanelUI>();
            SkillsPanelUI skillsPanel = canvasObject.AddComponent<SkillsPanelUI>();

            skillsPanel.panelRoot = new GameObject("SkillsPanelRoot");
            skillsPanel.panelRoot.SetActive(false);

            shell.SetDependencies(null, skillsPanel, null, null, null, null, null, homeDetails);
            Assert.True(shell.OpenHome());
            Assert.True(homeDetails.IsPanelVisible());

            Assert.True(shell.OpenSkills());
            Assert.False(homeDetails.IsPanelVisible());
            Assert.True(skillsPanel.IsPanelVisible());
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void HomeDetailsPanel_ClipsContentToPanelBounds()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        try
        {
            HomeDetailsPanelUI panel = canvasObject.AddComponent<HomeDetailsPanelUI>();
            panel.ShowPanel();

            Assert.NotNull(panel.panelRoot);
            Assert.NotNull(panel.panelRoot.GetComponent<RectMask2D>());
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }
}
