using FluentAssertions;

using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Notifications;

using Xunit;

namespace RestaurantPOS.UnitTests.Domain;

public class NotificationTests
{
    private static readonly DateTime Now = new(2026, 8, 15, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Raise_StartsUnread()
    {
        var notification = Notification.Raise(
            Guid.NewGuid(), NotificationType.MainStoreLowStock, NotificationSeverity.Warning,
            "Rice is low", "3 kg left", "LowStock:MainStore:1", "/inventory/main-store", Now);

        notification.IsRead.Should().BeFalse();
        notification.ReadAtUtc.Should().BeNull();
        notification.RaisedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void MarkRead_IsIdempotentSoTheFirstReadingIsTheOneRecorded()
    {
        var notification = Notification.Raise(
            Guid.NewGuid(), NotificationType.MainStoreLowStock, NotificationSeverity.Warning,
            "Rice is low", null, "key", null, Now);

        notification.MarkRead(Now.AddMinutes(5));
        notification.MarkRead(Now.AddMinutes(30));

        notification.ReadAtUtc.Should().Be(Now.AddMinutes(5));
    }

    [Fact]
    public void Raise_TrimsATitleTooLongForTheColumn()
    {
        var longName = new string('x', Notification.TitleMaxLength + 50);

        var notification = Notification.Raise(
            Guid.NewGuid(), NotificationType.MainStoreLowStock, NotificationSeverity.Warning,
            longName, null, "key", null, Now);

        notification.Title.Length.Should().Be(Notification.TitleMaxLength);
        notification.Title.Should().EndWith("…", "losing the tail of a sentence beats losing the alert");
    }

    [Fact]
    public void Raise_TreatsAnEmptyBodyAsAbsent()
    {
        var notification = Notification.Raise(
            Guid.NewGuid(), NotificationType.MainStoreLowStock, NotificationSeverity.Warning,
            "Rice is low", "   ", "key", null, Now);

        notification.Body.Should().BeNull();
    }
}

public class NotificationCatalogTests
{
    [Fact]
    public void EveryTypeInTheEnumIsDescribed()
    {
        var described = NotificationCatalog.All.Select(d => d.Type).ToHashSet();

        foreach (var type in Enum.GetValues<NotificationType>())
        {
            described.Should().Contain(type, $"{type} must appear in the catalog to be raisable");
        }
    }

    [Fact]
    public void EveryNotificationBelongsToAtLeastOneModule()
    {
        NotificationCatalog.All.Should().OnlyContain(d => d.Modules.Count > 0,
            "a notification with no audience could never be delivered");
    }

    [Fact]
    public void EveryThresholdedNotificationHasADefault()
    {
        NotificationCatalog.All
            .Where(d => d.HasThreshold)
            .Should().OnlyContain(d => d.DefaultThreshold.HasValue,
                "a tunable notification needs a value to start from");
    }

    [Fact]
    public void NotificationsWithoutAThresholdDeclareNoUnit()
    {
        NotificationCatalog.All
            .Where(d => !d.HasThreshold)
            .Should().OnlyContain(d => d.ThresholdUnit == ThresholdUnit.None);
    }

    [Fact]
    public void MostNotificationsAreOffByDefault()
    {
        var enabled = NotificationCatalog.All.Count(d => d.DefaultEnabled);

        // A bell that lights up all day is one nobody reads by the end of the week.
        enabled.Should().BeLessThan(NotificationCatalog.All.Count,
            "the default set is deliberately smaller than everything available");
    }

    [Fact]
    public void ForModules_GivesAnAdministratorEverything()
    {
        var forAdmin = NotificationCatalog.ForModules([], isAdmin: true);

        forAdmin.Should().HaveCount(NotificationCatalog.All.Count);
    }

    [Fact]
    public void ForModules_GivesACashierOnlyTheirOwn()
    {
        var forCashier = NotificationCatalog.ForModules([AppModule.PosBilling], isAdmin: false).ToList();

        forCashier.Should().NotBeEmpty();
        forCashier.Should().OnlyContain(d => d.Modules.Contains(AppModule.PosBilling));
        forCashier.Should().NotContain(d => d.Type == NotificationType.SupplierPaymentDue,
            "a cashier is never shown supplier alerts");
    }

    [Fact]
    public void Describe_ReturnsTheDescriptorForAKnownType()
    {
        var descriptor = NotificationCatalog.Describe(NotificationType.KitchenStockNegative);

        descriptor.Severity.Should().Be(NotificationSeverity.Urgent);
        descriptor.DefaultEnabled.Should().BeTrue();
    }
}

public class NotificationPreferenceTests
{
    [Fact]
    public void Update_ReplacesBothChoices()
    {
        var preference = NotificationPreference.Create(
            Guid.NewGuid(), NotificationType.FoodReadyToServe, isEnabled: true);

        preference.DesktopEnabled.Should().BeFalse("desktop pop-ups are opt-in");

        preference.Update(isEnabled: false, desktopEnabled: true);

        preference.IsEnabled.Should().BeFalse();
        preference.DesktopEnabled.Should().BeTrue();
    }
}

public class NotificationSettingTests
{
    [Fact]
    public void Create_RejectsANegativeThreshold()
    {
        var act = () => NotificationSetting.Create(NotificationType.KitchenTicketWaitingTooLong, -1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateThreshold_ReplacesTheValue()
    {
        var setting = NotificationSetting.Create(NotificationType.KitchenTicketWaitingTooLong, 15m);

        setting.UpdateThreshold(25m);

        setting.Threshold.Should().Be(25m);
    }
}
