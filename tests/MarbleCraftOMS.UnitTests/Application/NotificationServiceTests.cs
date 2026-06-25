using MarbleCraftOMS.Application.Notifications;
using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Core.Interfaces;
using NSubstitute;

namespace MarbleCraftOMS.UnitTests.Application;

public class NotificationServiceTests
{
    private readonly INotificationRepository _repo = Substitute.For<INotificationRepository>();
    private readonly NotificationService _svc;

    public NotificationServiceTests()
    {
        _svc = new NotificationService(_repo);
    }

    [Fact]
    public async Task GetRecentAsync_MapsEntityFieldsToResponse()
    {
        var now = DateTime.UtcNow;
        _repo.GetRecentAsync(null, 20).Returns(new List<Notification>
        {
            new() { Id = 1, Type = "LowStock", Title = "Low stock alert", Body = "Lot X is low", IsRead = false, CreatedAt = now }
        });

        var result = await _svc.GetRecentAsync(null);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("LowStock", result[0].Type);
        Assert.Equal("Low stock alert", result[0].Title);
        Assert.False(result[0].IsRead);
        Assert.Equal(now, result[0].CreatedAt);
    }

    [Fact]
    public async Task GetRecentAsync_PassesCustomerIdAndCountToRepo()
    {
        _repo.GetRecentAsync(5, 20).Returns(new List<Notification>());

        await _svc.GetRecentAsync(5);

        await _repo.Received(1).GetRecentAsync(5, 20);
    }

    [Fact]
    public async Task MarkReadAsync_CallsMarkThenSave()
    {
        await _svc.MarkReadAsync(3);

        await _repo.Received(1).MarkReadAsync(3);
        await _repo.Received(1).SaveAsync();
    }

    [Fact]
    public async Task MarkAllReadAsync_DelegatesToRepo()
    {
        await _svc.MarkAllReadAsync(7);

        await _repo.Received(1).MarkAllReadAsync(7);
    }
}
