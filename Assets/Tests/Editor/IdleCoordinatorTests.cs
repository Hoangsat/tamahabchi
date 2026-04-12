using NUnit.Framework;

public class IdleCoordinatorTests
{
    [Test]
    public void ClaimPendingEvents_AppliesCoinsItemsSkinsAndMoments()
    {
        CurrencyData currencyData = new CurrencyData { coins = 5 };
        InventoryData inventoryData = new InventoryData();
        SkillsData skillsData = new SkillsData();
        PetData petData = new PetData
        {
            hunger = 80f,
            mood = 80f,
            energy = 100f,
            hasIndependentStats = true,
            statusText = "Happy"
        };
        IdleData idleData = new IdleData();
        idleData.pendingEvents.Add(new IdleEventEntryData
        {
            id = "coins",
            type = "coins",
            coins = 12
        });
        idleData.pendingEvents.Add(new IdleEventEntryData
        {
            id = "chest",
            type = "chest",
            itemId = "food_snack"
        });
        idleData.pendingEvents.Add(new IdleEventEntryData
        {
            id = "rare",
            type = "rare",
            skinId = "skin_midnight"
        });
        idleData.pendingEvents.Add(new IdleEventEntryData
        {
            id = "moment",
            type = "moment",
            momentId = "logic_logic_think_advanced"
        });

        IdleCoordinator coordinator = new IdleCoordinator(
            idleData,
            CreateSkillsSystem(skillsData),
            new PetSystem(petData),
            new CurrencySystem(currencyData),
            new InventorySystem(inventoryData),
            new RoomData(),
            new BalanceConfig
            {
                lowHungerMoodThreshold = 30f,
                lowEnergyMoodThreshold = 20f
            });

        IdleClaimResult result = coordinator.ClaimPendingEvents();

        Assert.IsTrue(result.Success);
        Assert.AreEqual(4, result.ClaimedEventCount);
        Assert.AreEqual(17, currencyData.coins);
        Assert.AreEqual(1, result.ItemsGranted);
        Assert.AreEqual(1, result.SkinsGranted);
        Assert.AreEqual(1, result.MomentsLogged);
        Assert.AreEqual(0, coordinator.GetPendingEventCount());
        Assert.True(inventoryData.ownedSkins.Contains("skin_midnight"));
        Assert.AreEqual(1, inventoryData.items.Find(entry => entry != null && entry.itemId == "food_snack")?.count ?? 0);
        Assert.True(idleData.collectedMomentIds.Contains("logic_logic_think_advanced"));
    }

    [Test]
    public void GetHomeView_UsesLatestPendingSummary()
    {
        IdleData idleData = new IdleData
        {
            currentActionId = "base_walk",
            currentArchetypeId = SkillArchetypeCatalog.General
        };
        idleData.pendingEvents.Add(new IdleEventEntryData
        {
            id = "idle_evt",
            type = "coins",
            summary = "Питомец нашёл 8 монет."
        });

        IdleCoordinator coordinator = new IdleCoordinator(
            idleData,
            CreateSkillsSystem(new SkillsData()),
            new PetSystem(new PetData
            {
                hunger = 80f,
                mood = 80f,
                energy = 100f,
                hasIndependentStats = true,
                statusText = "Happy"
            }),
            new CurrencySystem(new CurrencyData()),
            new InventorySystem(new InventoryData()),
            new RoomData(),
            new BalanceConfig
            {
                lowHungerMoodThreshold = 30f,
                lowEnergyMoodThreshold = 20f
            });

        IdleHomeView view = coordinator.GetHomeView();

        Assert.AreEqual(1, view.PendingCount);
        Assert.AreEqual("Питомец нашёл 8 монет.", view.SummaryText);
        Assert.True(view.HasClaimableEvents);
    }

    private static SkillsSystem CreateSkillsSystem(SkillsData skillsData)
    {
        SkillsSystem system = new SkillsSystem();
        system.Init(skillsData);
        return system;
    }
}
