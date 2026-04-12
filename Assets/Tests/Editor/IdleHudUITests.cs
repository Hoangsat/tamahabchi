using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IdleHudUITests
{
    [Test]
    public void Awake_CreatesIdleSummaryBlock()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        try
        {
            GameObject homeRoot = CreateNode("HomeRoot", canvasObject.transform);
            GameObject topBar = CreateNode("TopBar", homeRoot.transform);
            CreateText("CoinsText", topBar.transform);
            GameObject progressionRoot = CreateNode("ProgressionRoot", topBar.transform);
            CreateText("LevelText", progressionRoot.transform);
            CreateText("XPText", progressionRoot.transform);

            GameObject mainStatusBlock = CreateNode("MainStatusBlock", homeRoot.transform);
            CreateText("MainStatusText", mainStatusBlock.transform);

            GameObject homeBottomBlock = CreateNode("HomeBottomBlock", homeRoot.transform);
            CreateButton("MissionsSummaryButton", homeBottomBlock.transform);

            GameObject homeActionsBlock = CreateNode("HomeActionsBlock", homeBottomBlock.transform);
            CreateButton("FeedButton", homeActionsBlock.transform);
            CreateButton("FocusButton", homeActionsBlock.transform);

            HUDUI hud = canvasObject.AddComponent<HUDUI>();
            MethodInfo awakeMethod = typeof(HUDUI).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(awakeMethod);
            awakeMethod.Invoke(hud, null);

            Transform idleBlock = homeRoot.transform.Find("MainStatusBlock/IdleSummaryBlock");
            Assert.NotNull(idleBlock);
            Assert.NotNull(idleBlock.Find("Header/IconText"));
            Assert.NotNull(idleBlock.Find("Header/ActionText"));
            Assert.NotNull(idleBlock.Find("SummaryText"));
            Assert.NotNull(idleBlock.Find("ClaimRow/BadgeText"));
            Assert.NotNull(idleBlock.Find("ClaimRow/ClaimButton"));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    private static GameObject CreateNode(string name, Transform parent)
    {
        GameObject node = new GameObject(name, typeof(RectTransform));
        node.transform.SetParent(parent, false);
        return node;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        return textObject.AddComponent<TextMeshProUGUI>();
    }

    private static Button CreateButton(string name, Transform parent)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);
        labelObject.AddComponent<TextMeshProUGUI>().text = name;

        return buttonObject.GetComponent<Button>();
    }
}
