using NUnit.Framework;

public class SaveNormalizerIdleTests
{
    [Test]
    public void Normalize_WhenIdleDataMissing_AddsDefaults()
    {
        SaveData normalized = SaveNormalizer.Normalize(new SaveData());

        Assert.NotNull(normalized.idleData);
        Assert.NotNull(normalized.idleData.pendingEvents);
        Assert.NotNull(normalized.idleData.collectedMomentIds);
        Assert.AreEqual(0, normalized.idleData.pendingEvents.Count);
        Assert.AreEqual(0, normalized.idleData.collectedMomentIds.Count);
    }

    [Test]
    public void Normalize_WhenIdleEventContainsNulls_SanitizesFields()
    {
        SaveData normalized = SaveNormalizer.Normalize(new SaveData
        {
            idleData = new IdleData
            {
                currentArchetypeId = "??",
                pendingEvents =
                {
                    new IdleEventEntryData
                    {
                        id = null,
                        type = "???",
                        archetypeId = "",
                        title = null,
                        summary = null,
                        coins = -5,
                        source = null
                    }
                }
            }
        });

        IdleEventEntryData entry = normalized.idleData.pendingEvents[0];
        Assert.AreEqual(SkillArchetypeCatalog.General, normalized.idleData.currentArchetypeId);
        Assert.AreEqual("coins", entry.type);
        Assert.AreEqual(SkillArchetypeCatalog.General, entry.archetypeId);
        Assert.AreEqual(string.Empty, entry.title);
        Assert.AreEqual(string.Empty, entry.summary);
        Assert.AreEqual(0, entry.coins);
        Assert.AreEqual("live", entry.source);
        Assert.False(string.IsNullOrWhiteSpace(entry.id));
    }
}
