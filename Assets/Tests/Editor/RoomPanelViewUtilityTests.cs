using NUnit.Framework;
using UnityEngine;

public class RoomPanelViewUtilityTests
{
    [Test]
    public void BuildResponsiveProfile_UsesVeryCompactValuesOnShortCanvas()
    {
        RoomPanelResponsiveProfile profile = RoomPanelViewUtility.BuildResponsiveProfile(720f);

        Assert.AreEqual(14, profile.ScreenInset);
        Assert.AreEqual(10f, profile.ScreenSpacing, 0.01f);
        Assert.AreEqual(102f, profile.HeroHeight, 0.01f);
        Assert.AreEqual(132f, profile.ActionHeight, 0.01f);
        Assert.AreEqual(50f, profile.UpgradeHeight, 0.01f);
        Assert.AreEqual(34f, profile.TitleFontSize, 0.01f);
    }

    [Test]
    public void EnsurePanelLayering_MovesPanelToShellRuntimeIndex()
    {
        GameObject root = new GameObject("Root", typeof(RectTransform));
        try
        {
            GameObject before = new GameObject("Before", typeof(RectTransform));
            before.transform.SetParent(root.transform, false);

            GameObject shellRuntimeRoot = new GameObject("ShellRuntimeRoot", typeof(RectTransform));
            shellRuntimeRoot.transform.SetParent(root.transform, false);

            GameObject after = new GameObject("After", typeof(RectTransform));
            after.transform.SetParent(root.transform, false);

            GameObject panelRoot = new GameObject("PanelRoot", typeof(RectTransform));
            panelRoot.transform.SetParent(root.transform, false);

            int shellIndex = shellRuntimeRoot.transform.GetSiblingIndex();
            RoomPanelViewUtility.EnsurePanelLayering(root.transform, panelRoot);

            Assert.AreEqual(shellIndex, panelRoot.transform.GetSiblingIndex());
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
