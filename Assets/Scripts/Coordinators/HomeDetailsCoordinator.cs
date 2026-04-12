using System.Collections.Generic;

public sealed class HomeDetailsCoordinator
{
    private readonly PetFlowCoordinator petFlowCoordinator;
    private readonly CurrencySystem currencySystem;
    private readonly CurrencyData currencyData;
    private readonly PetData petData;
    private readonly ProgressionData progressionData;
    private readonly RoomData roomData;
    private readonly SkillsSystem skillsSystem;

    public HomeDetailsCoordinator(
        PetFlowCoordinator petFlowCoordinator,
        CurrencySystem currencySystem,
        CurrencyData currencyData,
        PetData petData,
        ProgressionData progressionData,
        RoomData roomData,
        SkillsSystem skillsSystem)
    {
        this.petFlowCoordinator = petFlowCoordinator;
        this.currencySystem = currencySystem;
        this.currencyData = currencyData;
        this.petData = petData;
        this.progressionData = progressionData;
        this.roomData = roomData;
        this.skillsSystem = skillsSystem;
    }

    public PetStatusSummary GetPetStatusSummary()
    {
        return petFlowCoordinator != null ? petFlowCoordinator.GetStatusSummary() : PetHomePresenter.BuildUnavailableStatusSummary();
    }

    public string GetPetVitalsSummaryText()
    {
        return PetHomePresenter.GetVitalsSummaryText(petData);
    }

    public HomeDetailsViewData GetView(HomeDetailsTab tab)
    {
        PetStatusSummary summary = GetPetStatusSummary();
        PetData pet = petData ?? new PetData();
        ProgressionData progression = progressionData ?? new ProgressionData();
        RoomData room = roomData ?? new RoomData();

        return HomeDetailsPresenter.Build(
            tab,
            summary,
            GetCurrentCoins(),
            progression.level,
            room.roomLevel,
            GetTrackedSkillCount(),
            pet.hunger,
            pet.mood,
            progression.xp,
            0);
    }

    private int GetCurrentCoins()
    {
        if (currencySystem != null)
        {
            return currencySystem.GetCoins();
        }

        return currencyData != null ? currencyData.coins : 0;
    }

    private int GetTrackedSkillCount()
    {
        List<SkillEntry> skills = skillsSystem != null ? skillsSystem.GetSkills() : null;
        return skills != null ? skills.Count : 0;
    }
}
