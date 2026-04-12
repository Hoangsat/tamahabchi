using System;
using System.Collections.Generic;
using NUnit.Framework;

public class IdleBehaviorSystemTests
{
    [Test]
    public void Tick_WithoutSkills_InitializesBaseAction()
    {
        IdleData idleData = new IdleData();
        IdleBehaviorSystem system = new IdleBehaviorSystem(idleData, new FixedIdleRandomSource(0d));

        IdleRuntimeUpdate update = system.Tick(new List<SkillProgressionViewData>(), false, new RoomData(), new DateTime(2026, 4, 12, 12, 0, 0, DateTimeKind.Utc));

        Assert.IsTrue(update.StateChanged);
        Assert.AreEqual(SkillArchetypeCatalog.General, idleData.currentArchetypeId);
        StringAssert.StartsWith("base_", idleData.currentActionId);
        Assert.Greater(idleData.nextActionAtUtcTicks, idleData.currentActionStartedAtUtcTicks);
    }

    [Test]
    public void Tick_WithStrongSkillAndSkillRoll_UsesSkillArchetypeAction()
    {
        IdleData idleData = new IdleData();
        IdleBehaviorSystem system = new IdleBehaviorSystem(idleData, new SequenceIdleRandomSource(0.9d, 0.0d, 0.0d));
        List<SkillProgressionViewData> skills = new List<SkillProgressionViewData>
        {
            new SkillProgressionViewData
            {
                id = "skill_logic",
                name = "Logic",
                archetypeId = SkillArchetypeCatalog.Logic,
                axisPercent = 82f,
                icon = "MTH"
            }
        };

        system.Tick(skills, false, new RoomData(), new DateTime(2026, 4, 12, 12, 0, 0, DateTimeKind.Utc));

        Assert.AreEqual(SkillArchetypeCatalog.Logic, idleData.currentArchetypeId);
        StringAssert.StartsWith("logic_", idleData.currentActionId);
    }

    [Test]
    public void ApplyOffline_CapsAtFourPendingEvents()
    {
        IdleData idleData = new IdleData();
        IdleBehaviorSystem system = new IdleBehaviorSystem(idleData, new FixedIdleRandomSource(0d));

        IdleRuntimeUpdate update = system.ApplyOffline(
            IdleBehaviorSystem.OfflineCapSeconds * 2d,
            new List<SkillProgressionViewData>(),
            false,
            new RoomData(),
            new DateTime(2026, 4, 12, 20, 0, 0, DateTimeKind.Utc));

        Assert.IsTrue(update.StateChanged);
        Assert.IsTrue(update.SaveRequired);
        Assert.AreEqual(4, update.NewPendingEvents);
        Assert.AreEqual(4, idleData.pendingEvents.Count);
        Assert.Greater(idleData.lastResolvedUtcTicks, 0L);
    }

    [Test]
    public void Tick_WhenRewardsBlocked_DoesNotQueueEvents()
    {
        IdleData idleData = new IdleData();
        IdleBehaviorSystem system = new IdleBehaviorSystem(idleData, new FixedIdleRandomSource(0d));
        DateTime startUtc = new DateTime(2026, 4, 12, 12, 0, 0, DateTimeKind.Utc);

        system.Tick(new List<SkillProgressionViewData>(), false, new RoomData(), startUtc);
        idleData.nextActionAtUtcTicks = startUtc.AddSeconds(-1d).Ticks;

        IdleRuntimeUpdate update = system.Tick(new List<SkillProgressionViewData>(), true, new RoomData(), startUtc.AddSeconds(40d));

        Assert.IsTrue(update.StateChanged);
        Assert.AreEqual(0, update.NewPendingEvents);
        Assert.AreEqual(0, idleData.pendingEvents.Count);
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

    private sealed class SequenceIdleRandomSource : IIdleRandomSource
    {
        private readonly Queue<double> values;
        private readonly double fallback;

        public SequenceIdleRandomSource(params double[] values)
        {
            this.values = new Queue<double>(values ?? Array.Empty<double>());
            fallback = this.values.Count > 0 ? 0d : 0d;
        }

        public double NextDouble()
        {
            return values.Count > 0 ? values.Dequeue() : fallback;
        }
    }
}
