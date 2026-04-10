using UnityEngine;

public class PetSystem
{
    private PetData petData;
    private float revivedStatusUntilRealtime = -1f;

    public PetSystem(PetData petData)
    {
        this.petData = petData;
    }

    public bool UpdateHunger(float deltaTime, float hungerDrain)
    {
        if (petData == null || petData.isDead || deltaTime <= 0f)
        {
            return false;
        }

        float previousHunger = petData.hunger;
        bool wasDead = petData.isDead;
        petData.hunger -= hungerDrain * deltaTime;
        petData.hunger = Mathf.Clamp(petData.hunger, 0f, 100f);

        if (petData.hunger <= 0)
        {
            petData.isDead = true;
            petData.statusText = "Dead";
        }

        return !Mathf.Approximately(previousHunger, petData.hunger) || wasDead != petData.isDead;
    }

    public void Feed(float amount)
    {
        if (petData.isDead) return;

        petData.hunger += amount;
        petData.hunger = Mathf.Clamp(petData.hunger, 0f, 100f);
    }

    public bool UpdateStatus()
    {
        if (petData == null)
        {
            return false;
        }

        string previousStatus = petData.statusText ?? string.Empty;

        if (petData.isDead)
        {
            petData.statusText = "Dead";
            return previousStatus != petData.statusText;
        }

        if (IsRecentlyRevived())
            petData.statusText = "Revived";
        else if (petData.hunger <= 10f)
            petData.statusText = "Starving";
        else if (petData.energy <= 10f)
            petData.statusText = "Exhausted";
        else if (petData.mood <= 20f)
            petData.statusText = "Sad";
        else if (petData.energy <= 25f)
            petData.statusText = "Tired";
        else if (petData.hunger <= 30f)
            petData.statusText = "Hungry";
        else if (petData.mood <= 45f)
            petData.statusText = "Low Mood";
        else if (petData.hunger > 90f && petData.mood > 70f && petData.energy > 60f)
            petData.statusText = "Full";
        else if (petData.hunger > 60)
            petData.statusText = "Happy";
        else if (petData.hunger > 40)
            petData.statusText = "Content";
        else if (petData.hunger > 10)
            petData.statusText = "Okay";
        else
            petData.statusText = "Hungry";

        return previousStatus != petData.statusText;
    }

    public bool Revive(float hunger, float mood, float energy)
    {
        if (petData == null || !petData.isDead)
        {
            return false;
        }

        petData.isDead = false;
        petData.hunger = Mathf.Clamp(hunger, 0f, 100f);
        petData.mood = Mathf.Clamp(mood, 0f, 100f);
        petData.energy = Mathf.Clamp(energy, 0f, 100f);
        revivedStatusUntilRealtime = Time.realtimeSinceStartup + 4f;
        petData.statusText = "Revived";
        return true;
    }

    public PetStatusSummary GetStatusSummary(float lowHungerMoodThreshold, float lowEnergyMoodThreshold)
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

        if (petData.isDead)
        {
            summary.flowState = PetFlowState.Dead;
            summary.priorityStatus = PetPriorityStatus.Dead;
            summary.headline = "Pet is dead";
            summary.guidance = "Revive your pet to return to the loop.";
            summary.blocksGameplay = true;
            summary.needsAttention = true;
            return summary;
        }

        if (IsRecentlyRevived())
        {
            summary.flowState = PetFlowState.Revived;
            summary.priorityStatus = PetPriorityStatus.Revived;
            summary.headline = "Revived";
            summary.guidance = "Your pet is back. Feed it or take a gentle action to stabilise.";
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

        if (petData.energy <= 10f)
        {
            summary.flowState = PetFlowState.Critical;
            summary.priorityStatus = PetPriorityStatus.Exhausted;
            summary.headline = "Exhausted";
            summary.guidance = "Your pet is exhausted. Avoid focus and help it recover.";
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
            summary.guidance = "Feed your pet before the state gets critical.";
            summary.needsAttention = true;
            return summary;
        }

        if (petData.energy < lowEnergyMoodThreshold || petData.energy <= 25f)
        {
            summary.flowState = PetFlowState.Warning;
            summary.priorityStatus = PetPriorityStatus.Tired;
            summary.headline = "Tired";
            summary.guidance = "Focus sessions will hit harder while energy is low.";
            summary.needsAttention = true;
            return summary;
        }

        if (petData.hunger > 90f && petData.mood > 70f && petData.energy > 60f)
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
        summary.guidance = "Your pet is okay. Feed, focus, or work to keep momentum.";
        return summary;
    }

    public bool UpdateMoodDecay(
        float deltaTime,
        float lowHungerMoodThreshold,
        float lowEnergyMoodThreshold,
        float moodDecayPerSecondWhenHungry,
        float moodDecayPerSecondWhenTired)
    {
        if (petData == null || petData.isDead || deltaTime <= 0f)
        {
            return false;
        }

        float totalDecay = 0f;

        if (petData.hunger < lowHungerMoodThreshold)
        {
            totalDecay += Mathf.Max(0f, moodDecayPerSecondWhenHungry) * deltaTime;
        }

        if (petData.energy < lowEnergyMoodThreshold)
        {
            totalDecay += Mathf.Max(0f, moodDecayPerSecondWhenTired) * deltaTime;
        }

        if (totalDecay <= 0f)
        {
            return false;
        }

        float previousMood = petData.mood;
        petData.mood = Mathf.Clamp(petData.mood - totalDecay, 0f, 100f);
        return !Mathf.Approximately(previousMood, petData.mood);
    }

    public float GetMoodPercent()
    {
        if (petData == null)
        {
            return 0f;
        }

        return Mathf.Clamp(petData.mood, 0f, 100f);
    }

    public float GetEnergyPercent()
    {
        if (petData == null)
        {
            return 0f;
        }

        return Mathf.Clamp(petData.energy, 0f, 100f);
    }

    public void AddMood(float amount)
    {
        if (petData == null) return;

        petData.mood = Mathf.Clamp(petData.mood + amount, 0f, 100f);
    }

    public void ReduceMood(float amount)
    {
        if (petData == null) return;

        petData.mood = Mathf.Clamp(petData.mood - amount, 0f, 100f);
    }

    public void AddEnergy(float amount)
    {
        if (petData == null) return;

        petData.energy = Mathf.Clamp(petData.energy + amount, 0f, 100f);
    }

    public void ConsumeEnergy(float amount)
    {
        if (petData == null) return;

        petData.energy = Mathf.Clamp(petData.energy - amount, 0f, 100f);
    }

    public bool ApplyOfflineProgress(
        float elapsedSeconds,
        float hungerDrainPerSecond,
        float lowHungerMoodThreshold,
        float lowEnergyMoodThreshold,
        float moodDecayPerSecondWhenHungry,
        float moodDecayPerSecondWhenTired)
    {
        if (petData == null || petData.isDead || elapsedSeconds <= 0f)
        {
            return false;
        }

        bool changed = false;
        float clampedSeconds = Mathf.Max(0f, elapsedSeconds);

        float previousHunger = petData.hunger;
        petData.hunger = Mathf.Clamp(petData.hunger - hungerDrainPerSecond * clampedSeconds, 0f, 100f);
        changed |= !Mathf.Approximately(previousHunger, petData.hunger);

        if (petData.hunger <= 0f)
        {
            petData.hunger = 0f;
            petData.isDead = true;
            petData.statusText = "Dead";
            return true;
        }

        float totalMoodDecay = 0f;
        if (petData.hunger < lowHungerMoodThreshold)
        {
            totalMoodDecay += Mathf.Max(0f, moodDecayPerSecondWhenHungry) * clampedSeconds;
        }

        if (petData.energy < lowEnergyMoodThreshold)
        {
            totalMoodDecay += Mathf.Max(0f, moodDecayPerSecondWhenTired) * clampedSeconds;
        }

        if (totalMoodDecay > 0f)
        {
            float previousMood = petData.mood;
            petData.mood = Mathf.Clamp(petData.mood - totalMoodDecay, 0f, 100f);
            changed |= !Mathf.Approximately(previousMood, petData.mood);
        }

        changed |= UpdateStatus();
        return changed;
    }

    private bool IsRecentlyRevived()
    {
        return revivedStatusUntilRealtime > 0f && Time.realtimeSinceStartup < revivedStatusUntilRealtime;
    }
}
