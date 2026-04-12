public sealed class GameUiShellCoordinator
{
    private readonly GameManager gameManager;
    private readonly HUDUI hudUI;
    private readonly AppShellUI appShellUI;
    private readonly SkillsPanelUI skillsPanelUI;
    private readonly MissionPanelUI missionPanelUI;
    private readonly ShopPanelUI shopPanelUI;
    private readonly RoomPanelUI roomPanelUI;
    private readonly BattlePanelUI battlePanelUI;
    private readonly FocusPanelUI focusPanelUI;
    private readonly HomeDetailsPanelUI homeDetailsPanelUI;

    public GameUiShellCoordinator(
        GameManager gameManager,
        HUDUI hudUI,
        AppShellUI appShellUI,
        SkillsPanelUI skillsPanelUI,
        MissionPanelUI missionPanelUI,
        ShopPanelUI shopPanelUI,
        RoomPanelUI roomPanelUI,
        BattlePanelUI battlePanelUI,
        FocusPanelUI focusPanelUI,
        HomeDetailsPanelUI homeDetailsPanelUI)
    {
        this.gameManager = gameManager;
        this.hudUI = hudUI;
        this.appShellUI = appShellUI;
        this.skillsPanelUI = skillsPanelUI;
        this.missionPanelUI = missionPanelUI;
        this.shopPanelUI = shopPanelUI;
        this.roomPanelUI = roomPanelUI;
        this.battlePanelUI = battlePanelUI;
        this.focusPanelUI = focusPanelUI;
        this.homeDetailsPanelUI = homeDetailsPanelUI;
    }

    public void BindDependencies()
    {
        focusPanelUI?.SetGameManager(gameManager);
        skillsPanelUI?.SetGameManager(gameManager);
        missionPanelUI?.SetGameManager(gameManager);
        shopPanelUI?.SetGameManager(gameManager);
        roomPanelUI?.SetGameManager(gameManager);
        battlePanelUI?.SetGameManager(gameManager);
        homeDetailsPanelUI?.SetGameManager(gameManager);
        appShellUI?.SetDependencies(gameManager, skillsPanelUI, missionPanelUI, shopPanelUI, roomPanelUI, battlePanelUI, focusPanelUI, homeDetailsPanelUI);
        hudUI?.SetDependencies(gameManager, appShellUI, skillsPanelUI, missionPanelUI, shopPanelUI, roomPanelUI, battlePanelUI, focusPanelUI, homeDetailsPanelUI);
    }

    public bool OpenFocus(string preselectedSkillId = null)
    {
        if (appShellUI != null)
        {
            return appShellUI.OpenFocus(preselectedSkillId);
        }

        if (focusPanelUI == null)
        {
            return false;
        }

        focusPanelUI.OpenPanel(preselectedSkillId);
        return true;
    }

    public bool OpenSkills()
    {
        if (appShellUI != null)
        {
            return appShellUI.OpenSkills();
        }

        if (skillsPanelUI == null)
        {
            return false;
        }

        skillsPanelUI.ShowPanel();
        return true;
    }

    public bool OpenShop()
    {
        if (appShellUI != null)
        {
            return appShellUI.OpenShop();
        }

        if (shopPanelUI == null)
        {
            return false;
        }

        shopPanelUI.ShowPanel();
        return true;
    }

    public bool OpenRoom()
    {
        if (appShellUI != null)
        {
            return appShellUI.OpenRoom();
        }

        if (roomPanelUI == null)
        {
            return false;
        }

        roomPanelUI.ShowPanel();
        return true;
    }

    public bool OpenBattle()
    {
        if (appShellUI != null)
        {
            return appShellUI.OpenBattle();
        }

        if (battlePanelUI == null)
        {
            return false;
        }

        battlePanelUI.ShowPanel();
        return true;
    }

    public bool OpenHomeDetails()
    {
        if (appShellUI != null)
        {
            return appShellUI.OpenHomeDetails();
        }

        if (homeDetailsPanelUI == null)
        {
            return false;
        }

        homeDetailsPanelUI.ShowPanel();
        return true;
    }
}
