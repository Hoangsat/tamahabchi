using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionHudCoordinatorTests
{
    [Test]
    public void GetMissingBindings_ReportsAllNullReferences()
    {
        MissionHudCoordinator coordinator = new MissionHudCoordinator(null, 5, null, null);

        List<string> missing = coordinator.GetMissingBindings(new[]
        {
            new MissionHudSlotRefs("labelA", null, "buttonA", null),
            new MissionHudSlotRefs("labelB", null, "buttonB", null)
        });

        CollectionAssert.AreEquivalent(new[] { "labelA", "buttonA", "labelB", "buttonB" }, missing);
    }

    [Test]
    public void ClaimMissionAtSlot_WhenNoMission_ShowsFeedback()
    {
        string feedback = null;
        MissionHudCoordinator coordinator = new MissionHudCoordinator(null, 5, null, message => feedback = message);

        coordinator.ClaimMissionAtSlot(0);

        Assert.AreEqual("No mission available", feedback);
    }
}
