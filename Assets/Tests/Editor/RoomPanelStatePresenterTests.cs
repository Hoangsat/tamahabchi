using NUnit.Framework;
using UnityEngine;

public class RoomPanelStatePresenterTests
{
    [Test]
    public void Build_UsesCoinGateWhenUpgradeIsNotAffordable()
    {
        BalanceConfig balanceConfig = ScriptableObject.CreateInstance<BalanceConfig>();
        try
        {
            balanceConfig.roomUpgrade1Cost = 25;
            balanceConfig.roomUpgrade1UnlockLevel = 2;
            balanceConfig.roomUpgradeMoodBonus = 8f;

            RoomPanelStateData state = RoomPanelStatePresenter.Build(balanceConfig, 0, 2, 5, 1);

            Assert.AreEqual(0, state.currentLevel);
            Assert.AreEqual("Need 25 coins", state.blockedReason);
            Assert.AreEqual("Starter Room", state.currentVisualStateLabel);
            StringAssert.Contains("Save 20 more coins", state.footerNote);
            Assert.False(state.canUpgradeNow);
        }
        finally
        {
            Object.DestroyImmediate(balanceConfig);
        }
    }

    [Test]
    public void Build_WhenAffordableAndUnlocked_ReportsReadyState()
    {
        BalanceConfig balanceConfig = ScriptableObject.CreateInstance<BalanceConfig>();
        try
        {
            balanceConfig.roomUpgrade2Cost = 50;
            balanceConfig.roomUpgrade2UnlockLevel = 4;
            balanceConfig.roomUpgradeMoodBonus = 8f;

            RoomPanelStateData state = RoomPanelStatePresenter.Build(balanceConfig, 1, 2, 90, 4);

            Assert.AreEqual(1, state.currentLevel);
            Assert.AreEqual(50, state.currentUpgradeCost);
            Assert.AreEqual(0, state.currentUnlockLevel);
            Assert.AreEqual("Dream Room", state.nextVisualStateLabel);
            StringAssert.Contains("Upgrade now", state.footerNote);
            Assert.True(state.canUpgradeNow);
        }
        finally
        {
            Object.DestroyImmediate(balanceConfig);
        }
    }

    [Test]
    public void Build_MaxLevelReturnsMaxStateLabels()
    {
        BalanceConfig balanceConfig = ScriptableObject.CreateInstance<BalanceConfig>();
        try
        {
            RoomPanelStateData state = RoomPanelStatePresenter.Build(balanceConfig, 2, 2, 999, 10);

            Assert.True(state.isMaxLevel);
            Assert.AreEqual("Room is max level", state.blockedReason);
            Assert.AreEqual(0, state.currentUpgradeCost);
            Assert.AreEqual("Dream Room", state.currentVisualStateLabel);
            StringAssert.Contains("Room v1 is complete here", state.footerNote);
        }
        finally
        {
            Object.DestroyImmediate(balanceConfig);
        }
    }
}
