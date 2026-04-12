using System.Collections.Generic;
using UnityEngine;

public readonly struct GameManagerCriticalUiRefs
{
    public GameManagerCriticalUiRefs(
        HUDUI hudUi,
        SkillsPanelUI skillsPanelUi,
        MissionPanelUI missionPanelUi,
        ShopPanelUI shopPanelUi,
        RoomPanelUI roomPanelUi,
        BattlePanelUI battlePanelUi,
        FocusPanelUI focusPanelUi,
        AppShellUI appShellUi)
    {
        HudUi = hudUi;
        SkillsPanelUi = skillsPanelUi;
        MissionPanelUi = missionPanelUi;
        ShopPanelUi = shopPanelUi;
        RoomPanelUi = roomPanelUi;
        BattlePanelUi = battlePanelUi;
        FocusPanelUi = focusPanelUi;
        AppShellUi = appShellUi;
    }

    public HUDUI HudUi { get; }
    public SkillsPanelUI SkillsPanelUi { get; }
    public MissionPanelUI MissionPanelUi { get; }
    public ShopPanelUI ShopPanelUi { get; }
    public RoomPanelUI RoomPanelUi { get; }
    public BattlePanelUI BattlePanelUi { get; }
    public FocusPanelUI FocusPanelUi { get; }
    public AppShellUI AppShellUi { get; }
}

public static class GameManagerUiBootstrapUtility
{
    public static Transform ResolveSceneUiRoot(Component[] anchors)
    {
        if (anchors == null)
        {
            return null;
        }

        for (int i = 0; i < anchors.Length; i++)
        {
            Component anchor = anchors[i];
            if (anchor == null)
            {
                continue;
            }

            Canvas canvas = anchor.GetComponentInParent<Canvas>(true);
            if (canvas != null)
            {
                return canvas.transform;
            }
        }

        return null;
    }

    public static T ResolveUiComponent<T>(Transform sceneUiRoot) where T : Component
    {
        if (sceneUiRoot == null)
        {
            return null;
        }

        T directMatch = sceneUiRoot.GetComponent<T>();
        if (directMatch != null)
        {
            return directMatch;
        }

        return sceneUiRoot.GetComponentInChildren<T>(true);
    }

    public static List<string> GetMissingDependencies(GameManagerCriticalUiRefs refs)
    {
        List<string> missing = new List<string>();
        if (refs.HudUi == null) missing.Add("hudUI");
        if (refs.SkillsPanelUi == null) missing.Add("skillsPanelUI");
        if (refs.MissionPanelUi == null) missing.Add("missionPanelUI");
        if (refs.ShopPanelUi == null) missing.Add("shopPanelUI");
        if (refs.RoomPanelUi == null) missing.Add("roomPanelUI");
        if (refs.BattlePanelUi == null) missing.Add("battlePanelUI");
        if (refs.FocusPanelUi == null) missing.Add("focusPanelUI");
        if (refs.AppShellUi == null) missing.Add("appShellUI");
        return missing;
    }
}
