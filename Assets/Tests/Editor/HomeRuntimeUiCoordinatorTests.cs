using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HomeRuntimeUiCoordinatorTests
{
    [Test]
    public void RefreshOnboardingCompletion_MarksOnboardingComplete()
    {
        OnboardingData onboardingData = new OnboardingData
        {
            didBuyFood = true,
            didFeed = true,
            didFocus = true,
            isCompleted = false
        };

        HomeRuntimeUiCoordinator coordinator = new HomeRuntimeUiCoordinator(
            null,
            null,
            null,
            null,
            null,
            onboardingData,
            null);

        coordinator.RefreshOnboardingCompletion();

        Assert.IsTrue(onboardingData.isCompleted);
    }

    [Test]
    public void UpdateOnboardingUi_AppliesHintTextAndVisibility()
    {
        GameObject hintObject = new GameObject("OnboardingHint", typeof(RectTransform), typeof(TextMeshProUGUI));
        try
        {
            TextMeshProUGUI hintText = hintObject.GetComponent<TextMeshProUGUI>();
            OnboardingData onboardingData = new OnboardingData
            {
                didBuyFood = true,
                didFeed = true,
                didFocus = false,
                isCompleted = false
            };

            HomeRuntimeUiCoordinator coordinator = new HomeRuntimeUiCoordinator(
                null,
                null,
                null,
                null,
                null,
                onboardingData,
                null);

            coordinator.UpdateOnboardingUi(new HomeRuntimeUiRefs(
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                hintText));

            Assert.AreEqual("Hint: Complete a focus session", hintText.text);
            Assert.IsTrue(hintText.gameObject.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(hintObject);
        }
    }

    [Test]
    public void HasPartialTierButtonWiring_ReturnsTrueWhenOnlySomeTierButtonsExist()
    {
        GameObject buySnackObject = new GameObject("BuySnack", typeof(RectTransform), typeof(Image), typeof(Button));
        GameObject feedSnackObject = new GameObject("FeedSnack", typeof(RectTransform), typeof(Image), typeof(Button));
        try
        {
            HomeRuntimeUiCoordinator coordinator = new HomeRuntimeUiCoordinator(
                null,
                null,
                null,
                null,
                null,
                null,
                null);

            bool hasPartialWiring = coordinator.HasPartialTierButtonWiring(new HomeRuntimeUiRefs(
                null,
                buySnackObject.GetComponent<Button>(),
                null,
                null,
                feedSnackObject.GetComponent<Button>(),
                null,
                null,
                null,
                null,
                null,
                null));

            Assert.IsTrue(hasPartialWiring);
        }
        finally
        {
            Object.DestroyImmediate(buySnackObject);
            Object.DestroyImmediate(feedSnackObject);
        }
    }
}
