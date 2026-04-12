using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionPanelViewUtilityTests
{
    [Test]
    public void EnsureCached_ReusesExistingButtonInstance()
    {
        GameObject root = new GameObject("Root", typeof(RectTransform));
        try
        {
            RectTransform parent = root.GetComponent<RectTransform>();
            List<Button> cache = new List<Button>();

            Button first = MissionPanelViewUtility.EnsureCached(
                cache,
                0,
                index =>
                {
                    GameObject buttonObject = new GameObject($"Button_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
                    buttonObject.transform.SetParent(parent, false);
                    return buttonObject.GetComponent<Button>();
                });
            Button second = MissionPanelViewUtility.EnsureCached(cache, 0, _ => null);

            Assert.AreSame(first, second);
            Assert.AreEqual(1, cache.Count);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void ConfigureChoiceButton_SetsLabelAndSelectedColor()
    {
        GameObject buttonObject = new GameObject("ChoiceButton", typeof(RectTransform), typeof(Image), typeof(Button));
        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        try
        {
            labelObject.transform.SetParent(buttonObject.transform, false);
            Button button = buttonObject.GetComponent<Button>();

            MissionPanelViewUtility.ConfigureChoiceButton(button, "Math", null, true, 0);

            Assert.AreEqual("Math", labelObject.GetComponent<TextMeshProUGUI>().text);
            Assert.AreEqual(new Color(0.28f, 0.56f, 0.92f, 1f), buttonObject.GetComponent<Image>().color);
        }
        finally
        {
            Object.DestroyImmediate(buttonObject);
            Object.DestroyImmediate(labelObject);
        }
    }

    [Test]
    public void CreateSectionHeader_CreatesInactiveCardWithTexts()
    {
        GameObject root = new GameObject("Root", typeof(RectTransform));
        try
        {
            MissionPanelSectionHeaderRefs refs = MissionPanelViewUtility.CreateSectionHeader(root.GetComponent<RectTransform>(), "Section_0");

            Assert.NotNull(refs.root);
            Assert.NotNull(refs.titleText);
            Assert.NotNull(refs.subtitleText);
            Assert.False(refs.root.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
