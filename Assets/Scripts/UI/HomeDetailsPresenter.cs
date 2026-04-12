public readonly struct HomeDetailsViewData
{
    public HomeDetailsViewData(string subtitle, string title, string body, string footer)
    {
        Subtitle = subtitle;
        Title = title;
        Body = body;
        Footer = footer;
    }

    public string Subtitle { get; }
    public string Title { get; }
    public string Body { get; }
    public string Footer { get; }
}

public static class HomeDetailsPresenter
{
    public static HomeDetailsViewData BuildUnavailable(HomeDetailsTab tab)
    {
        return new HomeDetailsViewData(
            "Pet profile unavailable",
            GetTabLabel(tab),
            "GameManager missing.",
            string.Empty);
    }

    public static HomeDetailsViewData Build(
        HomeDetailsTab tab,
        PetStatusSummary summary,
        int currentCoins,
        int playerLevel,
        int roomLevel,
        int trackedSkills,
        float hunger,
        float mood,
        int currentXp,
        int xpRequiredForNextLevel)
    {
        string body;
        string footer;

        switch (tab)
        {
            case HomeDetailsTab.Pet:
                body =
                    $"Headline: {summary.headline}\n" +
                    $"Guidance: {summary.guidance}\n\n" +
                    $"Room level: {roomLevel}\n" +
                    $"Tracked skills: {trackedSkills}\n\n" +
                    "This first tab is the flexible pet profile shell. We can later turn it into a richer character sheet, slime-style pet card, or avatar hub.";
                footer = "Scaffold panel inspired by the Home detail UX reference.";
                break;
            case HomeDetailsTab.Stats:
                body =
                    $"Hunger: {hunger:0}\n" +
                    $"Mood: {mood:0}\n" +
                    $"Coins: {currentCoins}\n" +
                    $"Room level: {roomLevel}\n\n" +
                    "This tab can later become the full characteristic panel.";
                footer = "Scaffold panel inspired by the Home detail UX reference.";
                break;
            case HomeDetailsTab.Relic:
                body = "Relic tab scaffold.\n\nUse this slot to explore future passive item systems, collectible bonuses, or long-term progression.";
                footer = "Placeholder tab. We can decide the real feature and architecture later.";
                break;
            case HomeDetailsTab.Mastery:
                body = "Mastery tab scaffold.\n\nThis can later become a high-level growth tree, account progression layer, or focus mastery system.";
                footer = "Placeholder tab. We can decide the real feature and architecture later.";
                break;
            case HomeDetailsTab.Trait:
                body = "Trait tab scaffold.\n\nThis can later hold pet personality modifiers, strengths, weaknesses, or archetype-style bonuses.";
                footer = "Placeholder tab. We can decide the real feature and architecture later.";
                break;
            default:
                body = "Tab scaffold.";
                footer = "Placeholder tab. We can decide the real feature and architecture later.";
                break;
        }

        return new HomeDetailsViewData(
            $"{summary.headline}  •  Coins {currentCoins}",
            GetTabLabel(tab),
            body,
            footer);
    }

    public static string GetTabLabel(HomeDetailsTab tab)
    {
        switch (tab)
        {
            case HomeDetailsTab.Pet:
                return "Pet";
            case HomeDetailsTab.Stats:
                return "Stats";
            case HomeDetailsTab.Relic:
                return "Relic";
            case HomeDetailsTab.Mastery:
                return "Mastery";
            case HomeDetailsTab.Trait:
                return "Trait";
            default:
                return tab.ToString();
        }
    }
}
