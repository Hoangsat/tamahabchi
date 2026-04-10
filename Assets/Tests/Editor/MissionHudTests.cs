using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionHudTests
{
    [Test]
    public void MissingMissionHudBindings_ReportsExtraSlotsAsRequired()
    {
        GameObject managerObject = new GameObject("GameManager");
        try
        {
            GameManager manager = managerObject.AddComponent<GameManager>();

            List<string> missing = InvokeMissingMissionHudBindings(manager);

            CollectionAssert.Contains(missing, "missionExtra1Text");
            CollectionAssert.Contains(missing, "missionExtra2Text");
            CollectionAssert.Contains(missing, "claimExtra1Button");
            CollectionAssert.Contains(missing, "claimExtra2Button");
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void CompleteMissionHudBindings_ClearMissingList()
    {
        GameObject managerObject = new GameObject("GameManager");
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        try
        {
            GameManager manager = managerObject.AddComponent<GameManager>();
            manager.missionFeedText = CreateLabel("MissionFeedText", canvasObject.transform);
            manager.missionWorkText = CreateLabel("MissionWorkText", canvasObject.transform);
            manager.missionFocusText = CreateLabel("MissionFocusText", canvasObject.transform);
            manager.missionExtra1Text = CreateLabel("MissionExtra1Text", canvasObject.transform);
            manager.missionExtra2Text = CreateLabel("MissionExtra2Text", canvasObject.transform);
            manager.claimFeedButton = CreateButton("ClaimFeedButton", canvasObject.transform);
            manager.claimWorkButton = CreateButton("ClaimWorkButton", canvasObject.transform);
            manager.claimFocusButton = CreateButton("ClaimFocusButton", canvasObject.transform);
            manager.claimExtra1Button = CreateButton("ClaimExtra1Button", canvasObject.transform);
            manager.claimExtra2Button = CreateButton("ClaimExtra2Button", canvasObject.transform);

            List<string> missing = InvokeMissingMissionHudBindings(manager);

            Assert.AreEqual(0, missing.Count);
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(managerObject);
        }
    }

    private static List<string> InvokeMissingMissionHudBindings(GameManager manager)
    {
        MethodInfo method = typeof(GameManager).GetMethod("GetMissingMissionHudBindings", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return method.Invoke(manager, null) as List<string>;
    }

    private static TextMeshProUGUI CreateLabel(string name, Transform parent)
    {
        GameObject labelObject = new GameObject(name, typeof(RectTransform));
        labelObject.transform.SetParent(parent, false);
        return labelObject.AddComponent<TextMeshProUGUI>();
    }

    private static Button CreateButton(string name, Transform parent)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        return buttonObject.GetComponent<Button>();
    }
}
