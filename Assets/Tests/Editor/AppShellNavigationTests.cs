using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class AppShellNavigationTests
{
    [Test]
    public void OpenSkillsTwice_ReturnsToHome()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        try
        {
            AppShellUI shell = canvasObject.AddComponent<AppShellUI>();
            SkillsPanelUI skillsPanel = canvasObject.AddComponent<SkillsPanelUI>();

            skillsPanel.panelRoot = new GameObject("SkillsPanelRoot");
            skillsPanel.panelRoot.SetActive(false);

            shell.SetDependencies(null, skillsPanel, null, null, null, null, null);

            Assert.True(shell.OpenSkills());
            Assert.True(skillsPanel.IsPanelVisible());

            Assert.True(shell.OpenSkills());
            Assert.False(skillsPanel.IsPanelVisible());
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void RepeatingSectionTabs_ReturnsToHome()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        try
        {
            AppShellUI shell = canvasObject.AddComponent<AppShellUI>();
            FieldInfo currentScreenField = typeof(AppShellUI).GetField("currentScreen", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(currentScreenField);

            Assert.True(shell.OpenMissions());
            Assert.AreEqual(AppScreen.Missions, (AppScreen)currentScreenField.GetValue(shell));
            Assert.True(shell.OpenMissions());
            Assert.AreEqual(AppScreen.Home, (AppScreen)currentScreenField.GetValue(shell));

            Assert.True(shell.OpenShop());
            Assert.AreEqual(AppScreen.Shop, (AppScreen)currentScreenField.GetValue(shell));
            Assert.True(shell.OpenShop());
            Assert.AreEqual(AppScreen.Home, (AppScreen)currentScreenField.GetValue(shell));

            Assert.True(shell.OpenRoom());
            Assert.AreEqual(AppScreen.Room, (AppScreen)currentScreenField.GetValue(shell));
            Assert.True(shell.OpenRoom());
            Assert.AreEqual(AppScreen.Home, (AppScreen)currentScreenField.GetValue(shell));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void OpenHomeDetails_HidesShellHintToKeepNavVisible()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        try
        {
            AppShellUI shell = canvasObject.AddComponent<AppShellUI>();
            HomeDetailsPanelUI homeDetails = canvasObject.AddComponent<HomeDetailsPanelUI>();
            FieldInfo shellHintField = typeof(AppShellUI).GetField("shellHintText", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo buildShellMethod = typeof(AppShellUI).GetMethod("BuildShellIfNeeded", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(shellHintField);
            Assert.NotNull(buildShellMethod);
            buildShellMethod.Invoke(shell, null);

            shell.SetDependencies(null, null, null, null, null, null, homeDetails);
            Assert.True(shell.OpenHome());

            var shellHint = shellHintField.GetValue(shell) as TMPro.TextMeshProUGUI;
            Assert.NotNull(shellHint);
            Assert.False(shellHint.gameObject.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void Shell_HidesDuplicateBottomContextTitle()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        try
        {
            AppShellUI shell = canvasObject.AddComponent<AppShellUI>();
            MethodInfo buildShellMethod = typeof(AppShellUI).GetMethod("BuildShellIfNeeded", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo currentContextField = typeof(AppShellUI).GetField("currentContextText", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(buildShellMethod);
            Assert.NotNull(currentContextField);

            buildShellMethod.Invoke(shell, null);
            shell.OpenShop();

            var currentContext = currentContextField.GetValue(shell) as TMPro.TextMeshProUGUI;
            Assert.NotNull(currentContext);
            Assert.False(currentContext.gameObject.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }
}
