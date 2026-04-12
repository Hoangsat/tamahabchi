using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class GameManagerUiBootstrapUtilityTests
{
    [Test]
    public void ResolveSceneUiRoot_FindsCanvasFromAssignedControl()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(RectTransform));
        GameObject buttonObject = new GameObject("FeedButton", typeof(RectTransform), typeof(Image), typeof(Button));
        try
        {
            buttonObject.transform.SetParent(canvasObject.transform, false);

            Transform resolved = GameManagerUiBootstrapUtility.ResolveSceneUiRoot(new Component[]
            {
                buttonObject.GetComponent<Button>()
            });

            Assert.AreSame(canvasObject.transform, resolved);
        }
        finally
        {
            Object.DestroyImmediate(buttonObject);
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void GetMissingDependencies_ReportsNullBindings()
    {
        List<string> missing = GameManagerUiBootstrapUtility.GetMissingDependencies(new GameManagerCriticalUiRefs(
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null));

        CollectionAssert.AreEquivalent(new[]
        {
            "hudUI",
            "skillsPanelUI",
            "missionPanelUI",
            "shopPanelUI",
            "roomPanelUI",
            "battlePanelUI",
            "focusPanelUI",
            "appShellUI"
        }, missing);
    }
}
