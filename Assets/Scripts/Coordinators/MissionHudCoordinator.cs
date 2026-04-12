using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public readonly struct MissionHudSlotRefs
{
    public MissionHudSlotRefs(string labelBindingName, TextMeshProUGUI label, string claimButtonBindingName, Button claimButton)
    {
        LabelBindingName = labelBindingName ?? string.Empty;
        Label = label;
        ClaimButtonBindingName = claimButtonBindingName ?? string.Empty;
        ClaimButton = claimButton;
    }

    public string LabelBindingName { get; }
    public TextMeshProUGUI Label { get; }
    public string ClaimButtonBindingName { get; }
    public Button ClaimButton { get; }
}

public sealed class MissionHudCoordinator
{
    private readonly MissionCoordinator missionCoordinator;
    private readonly int slotCount;
    private readonly Action onMissionsChanged;
    private readonly Action<string> showFeedback;

    public MissionHudCoordinator(MissionCoordinator missionCoordinator, int slotCount, Action onMissionsChanged, Action<string> showFeedback)
    {
        this.missionCoordinator = missionCoordinator;
        this.slotCount = slotCount;
        this.onMissionsChanged = onMissionsChanged;
        this.showFeedback = showFeedback;
    }

    public List<string> GetMissingBindings(MissionHudSlotRefs[] slots)
    {
        List<string> missing = new List<string>();
        if (slots == null)
        {
            return missing;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            MissionHudSlotRefs slot = slots[i];
            if (slot.Label == null)
            {
                missing.Add(slot.LabelBindingName);
            }

            if (slot.ClaimButton == null)
            {
                missing.Add(slot.ClaimButtonBindingName);
            }
        }

        return missing;
    }

    public void UpdateHud(MissionHudSlotRefs[] slots)
    {
        if (slots == null)
        {
            return;
        }

        List<MissionEntryData> visibleMissions = GetVisibleEntries();
        for (int i = 0; i < slots.Length; i++)
        {
            MissionEntryData mission = i < visibleMissions.Count ? visibleMissions[i] : null;
            UpdateEntry(mission, slots[i]);
        }

        onMissionsChanged?.Invoke();
    }

    public void ClaimMission(string missionId)
    {
        if (!TryClaimMission(missionId, out string message))
        {
            if (!string.IsNullOrEmpty(message) && !string.Equals(message, "Mission system unavailable", StringComparison.Ordinal))
            {
                showFeedback?.Invoke(message);
            }
        }
    }

    public void ClaimMissionAtSlot(int slotIndex)
    {
        MissionEntryData mission = GetMissionAtSlot(slotIndex);
        if (mission == null)
        {
            showFeedback?.Invoke("No mission available");
            return;
        }

        ClaimMission(mission.missionId);
    }

    public List<MissionEntryData> GetVisibleEntries()
    {
        return missionCoordinator != null ? missionCoordinator.GetVisibleMissions(slotCount) : new List<MissionEntryData>();
    }

    private bool TryClaimMission(string missionId, out string message)
    {
        if (missionCoordinator == null)
        {
            message = "Mission system unavailable";
            return false;
        }

        return missionCoordinator.TryClaimMission(missionId, out message);
    }

    private MissionEntryData GetMissionAtSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotCount)
        {
            return null;
        }

        List<MissionEntryData> visibleMissions = GetVisibleEntries();
        return slotIndex < visibleMissions.Count ? visibleMissions[slotIndex] : null;
    }

    private static void UpdateEntry(MissionEntryData mission, MissionHudSlotRefs slot)
    {
        MissionHudSlotViewData viewData = MissionHudPresenter.Build(mission);

        if (slot.Label != null)
        {
            slot.Label.text = viewData.DisplayText;
        }

        if (slot.ClaimButton != null)
        {
            slot.ClaimButton.interactable = viewData.CanClaim;
        }
    }
}
