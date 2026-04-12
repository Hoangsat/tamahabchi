using NUnit.Framework;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDUITests
{
    [Test]
    public void Awake_CreatesSideActionsAndHidesLegacyHomeActions()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        try
        {
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(720f, 1280f);

            GameObject homeRoot = CreateNode("HomeRoot", canvasObject.transform);
            GameObject topBar = CreateNode("TopBar", homeRoot.transform);
            CreateText("CoinsText", topBar.transform);
            GameObject progressionRoot = CreateNode("ProgressionRoot", topBar.transform);
            CreateText("LevelText", progressionRoot.transform);
            CreateText("XPText", progressionRoot.transform);

            GameObject mainStatusBlock = CreateNode("MainStatusBlock", homeRoot.transform);
            CreateText("MainStatusText", mainStatusBlock.transform);

            GameObject homeBottomBlock = CreateNode("HomeBottomBlock", homeRoot.transform);
            CreateButton("MissionsSummaryButton", homeBottomBlock.transform, out _);

            GameObject homeActionsBlock = CreateNode("HomeActionsBlock", homeBottomBlock.transform);
            CreateButton("FeedButton", homeActionsBlock.transform, out _);
            CreateButton("FocusButton", homeActionsBlock.transform, out _);

            HUDUI hud = canvasObject.AddComponent<HUDUI>();
            MethodInfo awakeMethod = typeof(HUDUI).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(awakeMethod);
            awakeMethod.Invoke(hud, null);

            Transform sideActionsRoot = homeRoot.transform.Find("HomeSideActionsRoot");
            Assert.NotNull(sideActionsRoot);

            RectTransform sideRect = sideActionsRoot as RectTransform;
            Assert.NotNull(sideRect);
            Assert.AreEqual(new Vector2(1f, 0.5f), sideRect.anchorMin);
            Assert.AreEqual(new Vector2(1f, 0.5f), sideRect.anchorMax);
            Assert.AreEqual(new Vector2(140f, 180f), sideRect.sizeDelta);

            VerticalLayoutGroup sideLayout = sideActionsRoot.GetComponent<VerticalLayoutGroup>();
            Assert.NotNull(sideLayout);
            Assert.AreEqual(10f, sideLayout.spacing, 0.01f);
            Assert.AreEqual(TextAnchor.MiddleRight, sideLayout.childAlignment);

            Transform focusButton = sideActionsRoot.Find("FocusButton");
            Transform battleButton = sideActionsRoot.Find("BattleButton");
            Assert.NotNull(focusButton);
            Assert.NotNull(battleButton);

            LayoutElement focusLayout = focusButton.GetComponent<LayoutElement>();
            LayoutElement battleLayout = battleButton.GetComponent<LayoutElement>();
            Assert.NotNull(focusLayout);
            Assert.NotNull(battleLayout);
            Assert.AreEqual(132f, focusLayout.preferredWidth, 0.01f);
            Assert.AreEqual(52f, focusLayout.preferredHeight, 0.01f);
            Assert.AreEqual(132f, battleLayout.preferredWidth, 0.01f);
            Assert.AreEqual(52f, battleLayout.preferredHeight, 0.01f);

            Assert.False(homeActionsBlock.activeSelf);
            Assert.Null(homeActionsBlock.transform.Find("FeedButton"));
            Assert.Null(homeActionsBlock.transform.Find("FocusButton"));
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

    private static Button CreateButton(string name, Transform parent, out TextMeshProUGUI label)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);
        label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = name;

        return buttonObject.GetComponent<Button>();
    }
}
