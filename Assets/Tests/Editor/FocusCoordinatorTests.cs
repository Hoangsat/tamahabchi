using System.Reflection;
using NUnit.Framework;

public class FocusCoordinatorTests
{
    [Test]
    public void ClearLastResult_ReturnsFalseAndDoesNotNotify_WhenNothingToClear()
    {
        int notifications = 0;
        FocusCoordinator coordinator = CreateCoordinator(() => notifications++);

        bool cleared = coordinator.ClearLastResult();

        Assert.False(cleared);
        Assert.AreEqual(0, notifications);
    }

    [Test]
    public void ClearLastResult_ReturnsTrueAndNotifies_WhenResultExists()
    {
        int notifications = 0;
        FocusCoordinator coordinator = CreateCoordinator(() => notifications++);

        FieldInfo field = typeof(FocusCoordinator).GetField("lastFocusSessionResult", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(coordinator, new FocusSessionResultData
        {
            skillId = "skill_test",
            skillName = "Test"
        });

        bool cleared = coordinator.ClearLastResult();

        Assert.True(cleared);
        Assert.AreEqual(1, notifications);
        Assert.Null(coordinator.GetLastResult());
    }

    private static FocusCoordinator CreateCoordinator(System.Action onSessionChanged)
    {
        return new FocusCoordinator(
            new FocusSystem(),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            new FocusCoordinatorCallbacks
            {
                OnFocusSessionChanged = onSessionChanged
            });
    }
}
