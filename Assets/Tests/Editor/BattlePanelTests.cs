using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattlePanelTests
{
    [Test]
    public void ShowPanel_BuildsScrollableBattleLayout()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        try
        {
            BattlePanelUI panel = canvasObject.AddComponent<BattlePanelUI>();
            panel.ShowPanel();

            Assert.NotNull(panel.panelRoot);

            Transform screenRoot = panel.panelRoot.transform.Find("BattleScreenRoot");
            Assert.NotNull(screenRoot);
            Assert.IsNull(screenRoot.GetComponent<ContentSizeFitter>());
            Assert.NotNull(screenRoot.Find("BattleScrollRoot/Viewport/Content"));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void ShowPanel_UsesReadableBossFightCopy()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        try
        {
            BattlePanelUI panel = canvasObject.AddComponent<BattlePanelUI>();
            panel.ShowPanel();

            Assert.NotNull(panel.resultText);
            StringAssert.Contains("Select a boss and fight.", panel.resultText.text);
            StringAssert.DoesNotContain("Skill Radar", panel.resultText.text);
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }
}
