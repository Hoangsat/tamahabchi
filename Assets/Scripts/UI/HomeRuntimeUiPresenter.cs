using System;
using UnityEngine;

public readonly struct HomeRuntimeUiViewData
{
    public HomeRuntimeUiViewData(
        bool canFeedBasic,
        bool canFeedSnack,
        bool canFeedMeal,
        bool canFeedPremium,
        bool canBuySnack,
        bool canBuyMeal,
        bool canBuyPremium,
        bool canFocus,
        string focusTimerText,
        string focusButtonText)
    {
        CanFeedBasic = canFeedBasic;
        CanFeedSnack = canFeedSnack;
        CanFeedMeal = canFeedMeal;
        CanFeedPremium = canFeedPremium;
        CanBuySnack = canBuySnack;
        CanBuyMeal = canBuyMeal;
        CanBuyPremium = canBuyPremium;
        CanFocus = canFocus;
        FocusTimerText = focusTimerText ?? string.Empty;
        FocusButtonText = focusButtonText ?? string.Empty;
    }

    public bool CanFeedBasic { get; }
    public bool CanFeedSnack { get; }
    public bool CanFeedMeal { get; }
    public bool CanFeedPremium { get; }
    public bool CanBuySnack { get; }
    public bool CanBuyMeal { get; }
    public bool CanBuyPremium { get; }
    public bool CanFocus { get; }
    public string FocusTimerText { get; }
    public string FocusButtonText { get; }
}

public static class HomeRuntimeUiPresenter
{
    public static HomeRuntimeUiViewData Build(
        bool canFeedBasic,
        bool canFeedSnack,
        bool canFeedMeal,
        bool canFeedPremium,
        bool canBuySnack,
        bool canBuyMeal,
        bool canBuyPremium,
        bool isNeglected,
        bool isFocusPaused,
        bool isFocusRunning,
        float remainingFocusSeconds,
        bool hasActiveFocusSession,
        int focusReward)
    {
        return new HomeRuntimeUiViewData(
            canFeedBasic,
            canFeedSnack,
            canFeedMeal,
            canFeedPremium,
            canBuySnack,
            canBuyMeal,
            canBuyPremium,
            !isNeglected,
            BuildFocusTimerText(isNeglected, isFocusPaused, isFocusRunning, remainingFocusSeconds),
            hasActiveFocusSession ? "Focus Session" : $"Focus (+{focusReward})");
    }

    public static string GetOnboardingHint(OnboardingData onboardingData)
    {
        return PetHomePresenter.GetOnboardingHint(onboardingData);
    }

    private static string BuildFocusTimerText(bool isNeglected, bool isFocusPaused, bool isFocusRunning, float remainingFocusSeconds)
    {
        if (isNeglected)
        {
            return "Focus: Care first";
        }

        if (isFocusPaused)
        {
            return "Focus: Paused";
        }

        if (isFocusRunning)
        {
            return "Focus: " + FormatFocusTime(remainingFocusSeconds);
        }

        return "Focus: Ready";
    }

    private static string FormatFocusTime(float remainingSeconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
        TimeSpan time = TimeSpan.FromSeconds(totalSeconds);
        return time.TotalHours >= 1d ? time.ToString(@"hh\:mm\:ss") : time.ToString(@"mm\:ss");
    }
}
