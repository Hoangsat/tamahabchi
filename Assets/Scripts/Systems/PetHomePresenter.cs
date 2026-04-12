using UnityEngine;

public static class PetHomePresenter
{
    public static PetStatusSummary BuildUnavailableStatusSummary()
    {
        return new PetStatusSummary
        {
            flowState = PetFlowState.Warning,
            priorityStatus = PetPriorityStatus.None,
            headline = "Pet status unavailable",
            guidance = "Try reopening the scene.",
            needsAttention = true
        };
    }

    public static string GetVitalsSummaryText(PetData petData)
    {
        if (petData == null)
        {
            return "Hunger: --  |  Mood: --";
        }

        int hunger = Mathf.RoundToInt(Mathf.Clamp(petData.hunger, 0f, 100f));
        int mood = Mathf.RoundToInt(Mathf.Clamp(petData.mood, 0f, 100f));
        return $"Hunger: {hunger}  |  Mood: {mood}";
    }

    public static bool IsOnboardingComplete(OnboardingData onboardingData)
    {
        return onboardingData != null &&
               onboardingData.didBuyFood &&
               onboardingData.didFeed &&
               onboardingData.didFocus;
    }

    public static string GetOnboardingHint(OnboardingData onboardingData)
    {
        if (onboardingData == null || onboardingData.isCompleted)
        {
            return string.Empty;
        }

        if (!onboardingData.didBuyFood)
        {
            return "Hint: Claim missions, then buy care items";
        }

        if (!onboardingData.didFeed)
        {
            return "Hint: Feed or care for your pet";
        }

        if (!onboardingData.didFocus)
        {
            return "Hint: Complete a focus session";
        }

        return string.Empty;
    }
}
