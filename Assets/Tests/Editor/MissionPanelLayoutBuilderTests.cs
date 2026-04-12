using NUnit.Framework;
using UnityEngine;

public class MissionPanelLayoutBuilderTests
{
    [Test]
    public void Build_ReturnsCoreScreenReferences()
    {
        GameObject rootObject = new GameObject("MissionPanelRoot", typeof(RectTransform));
        try
        {
            MissionPanelLayoutRefs refs = MissionPanelLayoutBuilder.Build(
                rootObject.GetComponent<RectTransform>(),
                () => { },
                () => { },
                () => { });

            Assert.NotNull(refs.ScreenRoot);
            Assert.NotNull(refs.TitleText);
            Assert.NotNull(refs.CloseButton);
            Assert.NotNull(refs.ResetInfoText);
            Assert.NotNull(refs.HeaderStatsText);
            Assert.NotNull(refs.PanelStatusText);
            Assert.NotNull(refs.ScrollContent);
            Assert.NotNull(refs.EmptyStateText);
            Assert.NotNull(refs.FooterCreateButton);
            Assert.NotNull(refs.FooterRerollButton);
            Assert.False(refs.FooterRerollButton.interactable);
        }
        finally
        {
            Object.DestroyImmediate(rootObject);
        }
    }
}
