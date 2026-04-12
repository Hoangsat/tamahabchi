using UnityEngine;

public class PetSystem
{
    private const float OfflineStepSeconds = 60f;

    private readonly PetData petData;

    public PetSystem(PetData petData)
    {
        this.petData = petData;
    }

    public bool UpdateHunger(float deltaTime, float hungerDrain)
    {
        if (petData == null || deltaTime <= 0f)
        {
            return false;
        }

        float previousHunger = petData.hunger;
        petData.hunger = Mathf.Clamp(petData.hunger - Mathf.Max(0f, hungerDrain) * deltaTime, 0f, 100f);
        return !Mathf.Approximately(previousHunger, petData.hunger);
    }

    public void Feed(float amount)
    {
        if (petData == null)
        {
            return;
        }

        petData.hunger = Mathf.Clamp(petData.hunger + Mathf.Max(0f, amount), 0f, 100f);
    }

    public void ApplyCare(float amount)
    {
        if (petData == null)
        {
            return;
        }

        float safeAmount = Mathf.Max(0f, amount);
        float hungerGain = safeAmount * 0.5f;
        float moodGain = safeAmount - hungerGain;
        Feed(hungerGain);
        AddMood(moodGain);
    }

    public bool UpdateStatus()
    {
        if (petData == null)
        {
            return false;
        }

        string previousStatus = petData.statusText ?? string.Empty;

        if (IsNeglected())
        {
            petData.statusText = "Neglected";
        }
        else if (petData.hunger <= 10f)
        {
            petData.statusText = "Starving";
        }
        else if (petData.mood <= 20f)
        {
            petData.statusText = "Sad";
        }
        else if (petData.hunger <= 30f)
        {
            petData.statusText = "Hungry";
        }
        else if (petData.mood <= 45f)
        {
            petData.statusText = "Low Mood";
        }
        else if (petData.hunger > 90f && petData.mood > 70f)
        {
            petData.statusText = "Full";
        }
        else if (petData.hunger > 60f)
        {
            petData.statusText = "Happy";
        }
        else if (petData.hunger > 40f)
        {
            petData.statusText = "Content";
        }
        else
        {
            petData.statusText = "Okay";
        }

        return previousStatus != petData.statusText;
    }

    public PetStatusSummary GetStatusSummary(float lowHungerMoodThreshold, float _unusedEnergyThreshold)
    {
        PetStatusSummary summary = new PetStatusSummary();

        if (petData == null)
        {
            summary.flowState = PetFlowState.Warning;
            summary.priorityStatus = PetPriorityStatus.None;
            summary.headline = "Pet status unavailable";
            summary.guidance = "Try reopening the scene.";
            summary.needsAttention = true;
            return summary;
        }

        if (IsNeglected())
        {
            summary.flowState = PetFlowState.Neglected;
            summary.priorityStatus = PetPriorityStatus.Neglected;
            summary.headline = "Neglected";
            summary.guidance = "Pet neglected. Care first to stop skill decay.";
            summary.blocksGameplay = false;
            summary.needsAttention = true;
            return summary;
        }

        if (petData.hunger <= 10f)
        {
            summary.flowState = PetFlowState.Critical;
            summary.priorityStatus = PetPriorityStatus.Starving;
            summary.headline = "Critical";
            summary.guidance = "Your pet is starving. Feed it now.";
            summary.needsAttention = true;
            return summary;
        }

        if (petData.mood <= 20f)
        {
            summary.flowState = PetFlowState.Warning;
            summary.priorityStatus = PetPriorityStatus.LowMood;
            summary.headline = "Low Mood";
            summary.guidance = "Your pet feels down. Gentle care will help.";
            summary.needsAttention = true;
            return summary;
        }

        if (petData.hunger < lowHungerMoodThreshold)
        {
            summary.flowState = PetFlowState.Warning;
            summary.priorityStatus = PetPriorityStatus.Hungry;
            summary.headline = "Hungry";
            summary.guidance = "Feed your pet before it slips into neglect.";
            summary.needsAttention = true;
            return summary;
        }

        if (petData.hunger > 90f && petData.mood > 70f)
        {
            summary.flowState = PetFlowState.Healthy;
            summary.priorityStatus = PetPriorityStatus.Full;
            summary.headline = "Full";
            summary.guidance = "Your pet is thriving. Keep the loop going.";
            return summary;
        }

        summary.flowState = PetFlowState.Healthy;
        summary.priorityStatus = PetPriorityStatus.Normal;
        summary.headline = "Normal";
        summary.guidance = "Your pet is okay. Care, focus, or shop to keep momentum.";
        return summary;
    }

    public bool UpdateMoodDecay(
        float deltaTime,
        float lowHungerMoodThreshold,
        float _unusedLowEnergyThreshold,
        float moodDecayPerSecondWhenHungry,
        float _unusedMoodDecayPerSecondWhenTired)
    {
        if (petData == null || deltaTime <= 0f)
        {
            return false;
        }

        if (petData.hunger >= lowHungerMoodThreshold)
        {
            return false;
        }

        float previousMood = petData.mood;
        petData.mood = Mathf.Clamp(petData.mood - Mathf.Max(0f, moodDecayPerSecondWhenHungry) * deltaTime, 0f, 100f);
        return !Mathf.Approximately(previousMood, petData.mood);
    }

    public bool ApplyOfflineProgress(
        float elapsedSeconds,
        float hungerDrainPerSecond,
        float lowHungerMoodThreshold,
        float moodDecayPerSecondWhenHungry,
        out float neglectSecondsAccrued)
    {
        neglectSecondsAccrued = 0f;
        if (petData == null || elapsedSeconds <= 0f)
        {
            return false;
        }

        bool changed = false;
        float remainingSeconds = Mathf.Max(0f, elapsedSeconds);

        while (remainingSeconds > 0.001f)
        {
            float stepSeconds = Mathf.Min(OfflineStepSeconds, remainingSeconds);

            float previousHunger = petData.hunger;
            float previousMood = petData.mood;

            petData.hunger = Mathf.Clamp(petData.hunger - Mathf.Max(0f, hungerDrainPerSecond) * stepSeconds, 0f, 100f);
            if (petData.hunger < lowHungerMoodThreshold)
            {
                petData.mood = Mathf.Clamp(petData.mood - Mathf.Max(0f, moodDecayPerSecondWhenHungry) * stepSeconds, 0f, 100f);
            }

            if (IsNeglected())
            {
                neglectSecondsAccrued += stepSeconds;
            }

            changed |= !Mathf.Approximately(previousHunger, petData.hunger);
            changed |= !Mathf.Approximately(previousMood, petData.mood);

            remainingSeconds -= stepSeconds;
        }

        changed |= UpdateStatus();
        return changed;
    }

    public bool ApplyOfflineProgress(
        float elapsedSeconds,
        float hungerDrainPerSecond,
        float lowHungerMoodThreshold,
        float _unusedLowEnergyThreshold,
        float moodDecayPerSecondWhenHungry,
        float _unusedMoodDecayPerSecondWhenTired)
    {
        return ApplyOfflineProgress(
            elapsedSeconds,
            hungerDrainPerSecond,
            lowHungerMoodThreshold,
            moodDecayPerSecondWhenHungry,
            out _);
    }

    public bool IsNeglected()
    {
        return petData != null && petData.hunger <= 0f && petData.mood <= 0f;
    }

    public float GetMoodPercent()
    {
        return petData == null ? 0f : Mathf.Clamp(petData.mood, 0f, 100f);
    }

    public float GetEnergyPercent()
    {
        return petData == null ? 0f : Mathf.Clamp(petData.energy, 0f, 100f);
    }

    public void AddMood(float amount)
    {
        if (petData == null)
        {
            return;
        }

        petData.mood = Mathf.Clamp(petData.mood + amount, 0f, 100f);
    }

    public void ReduceMood(float amount)
    {
        if (petData == null)
        {
            return;
        }

        petData.mood = Mathf.Clamp(petData.mood - Mathf.Max(0f, amount), 0f, 100f);
    }

    public void AddEnergy(float amount)
    {
        if (petData == null)
        {
            return;
        }

        petData.energy = Mathf.Clamp(petData.energy + amount, 0f, 100f);
    }

    public void ConsumeEnergy(float amount)
    {
        if (petData == null)
        {
            return;
        }

        petData.energy = Mathf.Clamp(petData.energy - Mathf.Max(0f, amount), 0f, 100f);
    }
}
