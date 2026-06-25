using MarbleCraftOMS.Application.Orders;
using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Core.Enums;
using MarbleCraftOMS.Core.Events;
using MarbleCraftOMS.Core.Interfaces;
using NSubstitute;

namespace MarbleCraftOMS.UnitTests.Application;

public class OrderServiceTests
{
    private readonly IOrderRepository _orderRepo = Substitute.For<IOrderRepository>();
    private readonly IInventoryRepository _inventoryRepo = Substitute.For<IInventoryRepository>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly OrderService _svc;

    public OrderServiceTests()
    {
        _svc = new OrderService(_orderRepo, _inventoryRepo, _eventBus);
    }

    // ── PlaceAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task PlaceAsync_WithNoLines_ThrowsArgumentException()
    {
        var cmd = new PlaceOrderCommand { CustomerId = 1 };

        await Assert.ThrowsAsync<ArgumentException>(() => _svc.PlaceAsync(cmd));
    }

    [Fact]
    public async Task PlaceAsync_WhenLotNotFound_ThrowsKeyNotFoundException()
    {
        _inventoryRepo.GetLotAsync(99).Returns((StockLot?)null);
        var cmd = new PlaceOrderCommand
        {
            CustomerId = 1,
            Lines = [new OrderLineRequest(1, 99, 10, 50m)]
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _svc.PlaceAsync(cmd));
    }

    [Fact]
    public async Task PlaceAsync_WhenInsufficientStock_ThrowsAndDoesNotSave()
    {
        var lot = new StockLot { Id = 1, LotNumber = "LOT-001", QuantityOnHand = 5, QuantityCommitted = 5 };
        _inventoryRepo.GetLotAsync(1).Returns(lot);
        var cmd = new PlaceOrderCommand
        {
            CustomerId = 1,
            Lines = [new OrderLineRequest(1, 1, 10, 50m)]
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _svc.PlaceAsync(cmd));
        await _orderRepo.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task PlaceAsync_WithValidLine_CommitsStockAddsOrderAndSaves()
    {
        var lot = new StockLot { Id = 1, LotNumber = "LOT-001", QuantityOnHand = 100, QuantityCommitted = 0 };
        _inventoryRepo.GetLotAsync(1).Returns(lot);
        var cmd = new PlaceOrderCommand
        {
            CustomerId = 1,
            Lines = [new OrderLineRequest(1, 1, 10, 50m)]
        };

        var result = await _svc.PlaceAsync(cmd);

        Assert.Equal(10, lot.QuantityCommitted);
        Assert.Equal(OrderStatus.Pending, result.Status);
        await _orderRepo.Received(1).AddAsync(Arg.Is<DistributorOrder>(o => o.CustomerId == 1));
        await _orderRepo.Received(1).SaveAsync();
    }

    // ── ConfirmAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmAsync_WhenOrderNotFound_ThrowsKeyNotFoundException()
    {
        _orderRepo.GetByIdAsync(99).Returns((DistributorOrder?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _svc.ConfirmAsync(99));
    }

    [Fact]
    public async Task ConfirmAsync_WhenValid_SavesAndPublishesOrderStatusEvent()
    {
        var lot = new StockLot { LotNumber = "LOT-001", QuantityOnHand = 200, QuantityCommitted = 10 };
        var order = new DistributorOrder { Id = 1, CustomerId = 2, Status = OrderStatus.Pending };
        order.Lines.Add(new OrderLine { StockLot = lot, StockLotId = 1, ProductId = 1, Quantity = 5 });
        _orderRepo.GetByIdAsync(1).Returns(order);

        await _svc.ConfirmAsync(1);

        await _orderRepo.Received(1).SaveAsync();
        _eventBus.Received(1).Publish(Arg.Is<IDomainEvent>(e => e is OrderStatusChangedEvent));
        _eventBus.DidNotReceive().Publish(Arg.Is<IDomainEvent>(e => e is LowStockEvent));
    }

    [Fact]
    public async Task ConfirmAsync_WhenLotBelowThreshold_PublishesLowStockEvent()
    {
        // QuantityAvailable = 60 - 15 = 45, below the 50-unit threshold
        var lot = new StockLot { LotNumber = "LOT-001", QuantityOnHand = 60, QuantityCommitted = 15 };
        var order = new DistributorOrder { Id = 1, CustomerId = 2, Status = OrderStatus.Pending };
        order.Lines.Add(new OrderLine { StockLot = lot, StockLotId = 1, ProductId = 1, Quantity = 5 });
        _orderRepo.GetByIdAsync(1).Returns(order);

        await _svc.ConfirmAsync(1);

        _eventBus.Received().Publish(Arg.Is<IDomainEvent>(e => e is LowStockEvent));
    }

    // ── CancelAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelAsync_WhenOrderNotFound_ThrowsKeyNotFoundException()
    {
        _orderRepo.GetByIdAsync(99).Returns((DistributorOrder?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _svc.CancelAsync(99));
    }

    [Fact]
    public async Task CancelAsync_WhenPending_ReleasesStockAndSaves()
    {
        var lot = new StockLot { LotNumber = "LOT-001", QuantityOnHand = 100, QuantityCommitted = 20 };
        var order = new DistributorOrder { Id = 1, Status = OrderStatus.Pending };
        order.Lines.Add(new OrderLine { StockLot = lot, Quantity = 10 });
        _orderRepo.GetByIdAsync(1).Returns(order);

        await _svc.CancelAsync(1);

        Assert.Equal(10, lot.QuantityCommitted);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        await _orderRepo.Received(1).SaveAsync();
    }

    // ── DispatchAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DispatchAsync_WhenOrderNotFound_ThrowsKeyNotFoundException()
    {
        _orderRepo.GetByIdAsync(99).Returns((DistributorOrder?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _svc.DispatchAsync(99));
    }

    [Fact]
    public async Task DispatchAsync_WhenConfirmed_SetsDispatchedAndSaves()
    {
        var order = new DistributorOrder { Id = 1, Status = OrderStatus.Confirmed };
        _orderRepo.GetByIdAsync(1).Returns(order);

        await _svc.DispatchAsync(1);

        Assert.Equal(OrderStatus.Dispatched, order.Status);
        await _orderRepo.Received(1).SaveAsync();
    }
}
