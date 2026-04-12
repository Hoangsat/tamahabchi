using NUnit.Framework;
using UnityEngine;

public class MissionPanelPopupBuilderTests
{
    [Test]
    public void Build_ReturnsRequiredPopupReferences()
    {
        GameObject rootObject = new GameObject("MissionPanelRoot", typeof(RectTransform));
        try
        {
            RectTransform root = rootObject.GetComponent<RectTransform>();

            MissionPanelPopupRefs popup = MissionPanelPopupBuilder.Build(
                root,
                () => { },
                () => { },
                () => { },
                () => { });

            Assert.NotNull(popup.Root);
            Assert.NotNull(popup.TitleText);
            Assert.NotNull(popup.StatusText);
            Assert.NotNull(popup.SkillRoot);
            Assert.NotNull(popup.RoutineRoot);
            Assert.NotNull(popup.SkillListContent);
            Assert.NotNull(popup.RoutineSkillListContent);
            Assert.NotNull(popup.SkillMinutesSlider);
            Assert.NotNull(popup.RoutineTitleInput);
            Assert.NotNull(popup.RoutineCostText);
            Assert.False(popup.Root.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(rootObject);
        }
    }
}
