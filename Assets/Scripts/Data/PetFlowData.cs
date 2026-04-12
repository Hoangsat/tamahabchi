using System;

[Serializable]
public enum PetFlowState
{
    Healthy,
    Warning,
    Critical,
    Neglected
}

[Serializable]
public enum PetPriorityStatus
{
    None,
    Neglected,
    Starving,
    LowMood,
    Hungry,
    Normal,
    Full
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
