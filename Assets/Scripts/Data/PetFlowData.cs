using System;

[Serializable]
public enum PetFlowState
{
    Healthy,
    Warning,
    Critical,
    Dead,
    Revived
}

[Serializable]
public enum PetPriorityStatus
{
    None,
    Dead,
    Starving,
    Exhausted,
    LowMood,
    Hungry,
    Tired,
    Normal,
    Full,
    Revived
}

[Serializable]
public class PetStatusSummary
{
    public PetFlowState flowState = PetFlowState.Healthy;
    public PetPriorityStatus priorityStatus = PetPriorityStatus.None;
    public string headline = string.Empty;
    public string guidance = string.Empty;
    public bool blocksGameplay = false;
    public bool needsAttention = false;
}
