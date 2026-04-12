using TMPro;
using UnityEngine.UI;

public readonly struct HomeRuntimeUiRefs
{
    public HomeRuntimeUiRefs(
        Button feedButton,
        Button buySnackButton,
        Button buyMealButton,
        Button buyPremiumButton,
        Button feedSnackButton,
        Button feedMealButton,
        Button feedPremiumButton,
        Button focusButton,
        TextMeshProUGUI focusTimerText,
        TextMeshProUGUI focusButtonText,
        TextMeshProUGUI onboardingHintText)
    {
        FeedButton = feedButton;
        BuySnackButton = buySnackButton;
        BuyMealButton = buyMealButton;
        BuyPremiumButton = buyPremiumButton;
        FeedSnackButton = feedSnackButton;
        FeedMealButton = feedMealButton;
        FeedPremiumButton = feedPremiumButton;
        FocusButton = focusButton;
        FocusTimerText = focusTimerText;
        FocusButtonText = focusButtonText;
        OnboardingHintText = onboardingHintText;
    }

    public Button FeedButton { get; }
    public Button BuySnackButton { get; }
    public Button BuyMealButton { get; }
    public Button BuyPremiumButton { get; }
    public Button FeedSnackButton { get; }
    public Button FeedMealButton { get; }
    public Button FeedPremiumButton { get; }
    public Button FocusButton { get; }
    public TextMeshProUGUI FocusTimerText { get; }
    public TextMeshProUGUI FocusButtonText { get; }
    public TextMeshProUGUI OnboardingHintText { get; }
}

public sealed class HomeRuntimeUiCoordinator
{
    private readonly InventorySystem inventorySystem;
    private readonly ProgressionSystem progressionSystem;
    private readonly FocusSystem focusSystem;
    private readonly PetSystem petSystem;
    private readonly CurrencyData currencyData;
    private readonly OnboardingData onboardingData;
    private readonly BalanceConfig balanceConfig;

    public HomeRuntimeUiCoordinator(
        InventorySystem inventorySystem,
        ProgressionSystem progressionSystem,
        FocusSystem focusSystem,
        PetSystem petSystem,
        CurrencyData currencyData,
        OnboardingData onboardingData,
        BalanceConfig balanceConfig)
    {
        this.inventorySystem = inventorySystem;
        this.progressionSystem = progressionSystem;
        this.focusSystem = focusSystem;
        this.petSystem = petSystem;
        this.currencyData = currencyData;
        this.onboardingData = onboardingData;
        this.balanceConfig = balanceConfig;
    }

    public void RefreshOnboardingCompletion()
    {
        if (onboardingData == null)
        {
            return;
        }

        onboardingData.isCompleted = PetHomePresenter.IsOnboardingComplete(onboardingData);
    }

    public void UpdateUi(HomeRuntimeUiRefs refs)
    {
        bool buyUnlocked = progressionSystem != null && progressionSystem.IsBuyUnlocked();
        int coins = currencyData != null ? currencyData.coins : 0;
        int focusReward = progressionSystem != null && balanceConfig != null
            ? progressionSystem.GetFocusReward(balanceConfig.baseFocusReward)
            : 0;

        HomeRuntimeUiViewData viewData = HomeRuntimeUiPresenter.Build(
            HasItem("food_basic"),
            HasItem("food_snack"),
            HasItem("food_meal"),
            HasItem("food_premium"),
            buyUnlocked && coins >= GetPrice(balanceConfig != null ? balanceConfig.snackPrice : 0),
            buyUnlocked && coins >= GetPrice(balanceConfig != null ? balanceConfig.mealPrice : 0),
            buyUnlocked && coins >= GetPrice(balanceConfig != null ? balanceConfig.premiumPrice : 0),
            petSystem != null && petSystem.IsNeglected(),
            focusSystem != null && focusSystem.IsPaused,
            focusSystem != null && focusSystem.IsRunning,
            focusSystem != null ? focusSystem.GetRemainingTime() : 0f,
            focusSystem != null && focusSystem.HasActiveSession,
            focusReward);

        if (refs.FeedButton != null)
        {
            refs.FeedButton.interactable = viewData.CanFeedBasic;
        }

        if (refs.FeedSnackButton != null)
        {
            refs.FeedSnackButton.interactable = viewData.CanFeedSnack;
        }

        if (refs.FeedMealButton != null)
        {
            refs.FeedMealButton.interactable = viewData.CanFeedMeal;
        }

        if (refs.FeedPremiumButton != null)
        {
            refs.FeedPremiumButton.interactable = viewData.CanFeedPremium;
        }

        if (refs.BuySnackButton != null)
        {
            refs.BuySnackButton.interactable = viewData.CanBuySnack;
        }

        if (refs.BuyMealButton != null)
        {
            refs.BuyMealButton.interactable = viewData.CanBuyMeal;
        }

        if (refs.BuyPremiumButton != null)
        {
            refs.BuyPremiumButton.interactable = viewData.CanBuyPremium;
        }

        if (refs.FocusButton != null)
        {
            refs.FocusButton.interactable = viewData.CanFocus;
        }

        if (refs.FocusTimerText != null)
        {
            refs.FocusTimerText.text = viewData.FocusTimerText;
        }

        if (refs.FocusButtonText != null)
        {
            refs.FocusButtonText.text = viewData.FocusButtonText;
        }
    }

    public void UpdateOnboardingUi(HomeRuntimeUiRefs refs)
    {
        if (refs.OnboardingHintText == null)
        {
            return;
        }

        string hint = HomeRuntimeUiPresenter.GetOnboardingHint(onboardingData);
        refs.OnboardingHintText.text = hint;
        refs.OnboardingHintText.gameObject.SetActive(!string.IsNullOrEmpty(hint));
    }

    public bool HasPartialTierButtonWiring(HomeRuntimeUiRefs refs)
    {
        bool hasAnyTierButtons =
            refs.BuySnackButton != null || refs.BuyMealButton != null || refs.BuyPremiumButton != null ||
            refs.FeedSnackButton != null || refs.FeedMealButton != null || refs.FeedPremiumButton != null;
        bool hasAllTierButtons =
            refs.BuySnackButton != null && refs.BuyMealButton != null && refs.BuyPremiumButton != null &&
            refs.FeedSnackButton != null && refs.FeedMealButton != null && refs.FeedPremiumButton != null;

        return hasAnyTierButtons && !hasAllTierButtons;
    }

    private bool HasItem(string itemId)
    {
        return inventorySystem != null && inventorySystem.HasItem(itemId, 1);
    }

    private static int GetPrice(int configuredPrice)
    {
        return configuredPrice < 0 ? 0 : configuredPrice;
    }
}
