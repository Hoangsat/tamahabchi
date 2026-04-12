using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class IdlePlayModeTests
{
    private const string MainSceneName = "Main";

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        IdleBehaviorSystem.TestRandomSourceFactory = null;
        new SaveManager().Reset();
        yield return SceneManager.LoadSceneAsync(MainSceneName, LoadSceneMode.Single);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        IdleBehaviorSystem.TestRandomSourceFactory = null;
        new SaveManager().Reset();
        yield return null;
    }

    [UnityTest]
    public IEnumerator MainScene_IdleInbox_PersistsAcrossReloadAndClaimWorks()
    {
        GameManager manager = UnityEngine.Object.FindAnyObjectByType<GameManager>();
        HUDUI hud = UnityEngine.Object.FindAnyObjectByType<HUDUI>();

        Assert.NotNull(manager);
        Assert.NotNull(hud);

        manager.idleData.currentActionId = "base_walk";
        manager.idleData.currentArchetypeId = SkillArchetypeCatalog.General;
        manager.idleData.pendingEvents.Add(new IdleEventEntryData
        {
            id = "idle_test_event",
            type = "coins",
            title = "Нашёл монеты",
            summary = "Питомец нашёл 9 монет.",
            coins = 9,
            source = "live"
        });
        manager.SaveGame();

        yield return SceneManager.LoadSceneAsync(MainSceneName, LoadSceneMode.Single);
        yield return null;

        GameManager reloadedManager = UnityEngine.Object.FindAnyObjectByType<GameManager>();
        HUDUI reloadedHud = UnityEngine.Object.FindAnyObjectByType<HUDUI>();

        Assert.NotNull(reloadedManager);
        Assert.NotNull(reloadedHud);
        Assert.AreEqual(1, reloadedManager.GetPendingIdleEventCount());

        Transform idleBlock = reloadedHud.transform.Find("HomeRoot/MainStatusBlock/IdleSummaryBlock");
        Assert.NotNull(idleBlock);
        Assert.True(idleBlock.gameObject.activeSelf);

        TextMeshProUGUI badgeText = idleBlock.Find("ClaimRow/BadgeText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI summaryText = idleBlock.Find("SummaryText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI claimButtonText = idleBlock.Find("ClaimRow/ClaimButton/Label").GetComponent<TextMeshProUGUI>();
        Button claimButton = idleBlock.Find("ClaimRow/ClaimButton").GetComponent<Button>();
        Assert.NotNull(badgeText);
        Assert.NotNull(summaryText);
        Assert.NotNull(claimButtonText);
        Assert.NotNull(claimButton);
        Assert.AreEqual("x1", badgeText.text);

        int coinsBeforeClaim = reloadedManager.GetCurrentCoins();
        claimButton.onClick.Invoke();
        yield return null;

        Assert.AreEqual(coinsBeforeClaim + 9, reloadedManager.GetCurrentCoins());
        Assert.AreEqual(0, reloadedManager.GetPendingIdleEventCount());
        Assert.AreEqual("0", badgeText.text);
        Assert.AreEqual("Награда питомца получена", summaryText.text);
        Assert.AreEqual("Пусто", claimButtonText.text);
        Assert.False(claimButton.interactable);
    }

    [UnityTest]
    public IEnumerator MainScene_IdleSummary_WhenNeglected_ShowsCareFirstState()
    {
        GameManager manager = UnityEngine.Object.FindAnyObjectByType<GameManager>();
        Assert.NotNull(manager);

        manager.petData.hunger = 0f;
        manager.petData.mood = 0f;
        manager.petData.energy = 100f;
        manager.petData.hasIndependentStats = true;
        manager.idleData.currentActionId = "base_idle";
        manager.idleData.currentArchetypeId = SkillArchetypeCatalog.General;
        manager.idleData.pendingEvents.Clear();
        manager.SaveGame();

        yield return SceneManager.LoadSceneAsync(MainSceneName, LoadSceneMode.Single);
        yield return null;

        HUDUI hud = UnityEngine.Object.FindAnyObjectByType<HUDUI>();
        Assert.NotNull(hud);

        Transform idleBlock = hud.transform.Find("HomeRoot/MainStatusBlock/IdleSummaryBlock");
        Assert.NotNull(idleBlock);

        TextMeshProUGUI summaryText = idleBlock.Find("SummaryText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI badgeText = idleBlock.Find("ClaimRow/BadgeText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI claimButtonText = idleBlock.Find("ClaimRow/ClaimButton/Label").GetComponent<TextMeshProUGUI>();
        Button claimButton = idleBlock.Find("ClaimRow/ClaimButton").GetComponent<Button>();

        Assert.NotNull(summaryText);
        Assert.NotNull(badgeText);
        Assert.NotNull(claimButtonText);
        Assert.NotNull(claimButton);
        StringAssert.Contains("Сначала верните hunger и mood", summaryText.text);
        Assert.AreEqual("0", badgeText.text);
        Assert.AreEqual("Пусто", claimButtonText.text);
        Assert.False(claimButton.interactable);
    }

    [UnityTest]
    public IEnumerator MainScene_OfflineIdleSummary_GeneratesOnceAndDoesNotDuplicateAcrossReloads()
    {
        IdleBehaviorSystem.TestRandomSourceFactory = () => new FixedIdleRandomSource(0d);

        GameManager manager = UnityEngine.Object.FindAnyObjectByType<GameManager>();
        Assert.NotNull(manager);

        manager.AddSkill("Logic", "DEV");
        manager.SaveGame();

        SaveManager saveManager = new SaveManager();
        SaveData firstSave = saveManager.Load();
        Assert.NotNull(firstSave);
        firstSave.lastSeenUtc = System.DateTime.UtcNow.AddHours(-2).ToString("O");
        saveManager.Save(firstSave);

        yield return SceneManager.LoadSceneAsync(MainSceneName, LoadSceneMode.Single);
        yield return null;

        GameManager reloadedManager = UnityEngine.Object.FindAnyObjectByType<GameManager>();
        HUDUI hud = UnityEngine.Object.FindAnyObjectByType<HUDUI>();
        Assert.NotNull(reloadedManager);
        Assert.NotNull(hud);

        int pendingAfterOffline = reloadedManager.GetPendingIdleEventCount();
        Assert.AreEqual(4, pendingAfterOffline);

        Transform idleBlock = hud.transform.Find("HomeRoot/MainStatusBlock/IdleSummaryBlock");
        Assert.NotNull(idleBlock);

        TextMeshProUGUI summaryText = idleBlock.Find("SummaryText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI badgeText = idleBlock.Find("ClaimRow/BadgeText").GetComponent<TextMeshProUGUI>();
        Assert.NotNull(summaryText);
        Assert.NotNull(badgeText);
        StringAssert.StartsWith("Пока вас не было:", summaryText.text);
        Assert.AreEqual("x4", badgeText.text);

        SaveData savedAfterOfflineApply = saveManager.Load();
        Assert.NotNull(savedAfterOfflineApply);
        Assert.NotNull(savedAfterOfflineApply.idleData);
        Assert.AreEqual(4, savedAfterOfflineApply.idleData.pendingEvents.Count);
        Assert.True(savedAfterOfflineApply.idleData.lastResolvedUtcTicks > 0L);

        yield return SceneManager.LoadSceneAsync(MainSceneName, LoadSceneMode.Single);
        yield return null;

        GameManager secondReloadManager = UnityEngine.Object.FindAnyObjectByType<GameManager>();
        HUDUI secondHud = UnityEngine.Object.FindAnyObjectByType<HUDUI>();
        Assert.NotNull(secondReloadManager);
        Assert.NotNull(secondHud);
        Assert.AreEqual(pendingAfterOffline, secondReloadManager.GetPendingIdleEventCount());

        Transform secondIdleBlock = secondHud.transform.Find("HomeRoot/MainStatusBlock/IdleSummaryBlock");
        Assert.NotNull(secondIdleBlock);
        TextMeshProUGUI secondBadgeText = secondIdleBlock.Find("ClaimRow/BadgeText").GetComponent<TextMeshProUGUI>();
        Assert.NotNull(secondBadgeText);
        Assert.AreEqual("x4", secondBadgeText.text);
    }

    private sealed class FixedIdleRandomSource : IIdleRandomSource
    {
        private readonly double value;

        public FixedIdleRandomSource(double value)
        {
            this.value = value;
        }

        public double NextDouble()
        {
            return value;
        }
    }
}
