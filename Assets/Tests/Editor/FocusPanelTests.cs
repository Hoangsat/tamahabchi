using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FocusPanelTests
{
    [Test]
    public void OpenPanel_BuildsScrollableSetupLayout()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        try
        {
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(720f, 1280f);

            FocusPanelUI panel = canvasObject.AddComponent<FocusPanelUI>();
            panel.OpenPanel();

            Transform panelRoot = canvasObject.transform.Find("FocusPanelRoot");
            Assert.NotNull(panelRoot);
            Assert.True(panelRoot.gameObject.activeSelf);

            Transform windowRoot = panelRoot.Find("WindowRoot");
            Assert.NotNull(windowRoot);
            Assert.NotNull(windowRoot.GetComponent<VerticalLayoutGroup>());

            Transform setupRoot = windowRoot.Find("SetupRoot");
            Assert.NotNull(setupRoot);

            Transform skillList = setupRoot.Find("SkillList");
            Assert.NotNull(skillList);
            Assert.NotNull(skillList.GetComponent<ScrollRect>());
            Assert.NotNull(skillList.Find("Viewport"));
            Assert.NotNull(skillList.Find("Viewport").GetComponent<RectMask2D>());
            Assert.NotNull(skillList.Find("Viewport/Content"));

            LayoutElement skillListLayout = skillList.GetComponent<LayoutElement>();
            Assert.NotNull(skillListLayout);
            Assert.AreEqual(420f, skillListLayout.preferredHeight, 0.01f);

            Transform durationRow = setupRoot.Find("DurationRow");
            Assert.NotNull(durationRow);
            LayoutElement durationRowLayout = durationRow.GetComponent<LayoutElement>();
            Assert.NotNull(durationRowLayout);
            Assert.AreEqual(76f, durationRowLayout.preferredHeight, 0.01f);

            Transform startButton = setupRoot.Find("StartButton");
            Assert.NotNull(startButton);
            Assert.NotNull(startButton.GetComponent<Button>());
            Assert.AreEqual("Start Focus", startButton.GetComponentInChildren<TextMeshProUGUI>(true).text);
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }
}
