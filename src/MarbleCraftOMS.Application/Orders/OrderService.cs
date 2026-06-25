using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Core.Events;
using MarbleCraftOMS.Core.Interfaces;

namespace MarbleCraftOMS.Application.Orders;

public class OrderService(
    IOrderRepository orderRepo,
    IInventoryRepository inventoryRepo,
    IEventBus eventBus) : IOrderService
{
    public async Task<PlaceOrderResponse> PlaceAsync(PlaceOrderCommand cmd)
    {
        if (cmd.Lines.Count == 0)
            throw new ArgumentException("An order must have at least one line.");

        var order = new DistributorOrder
        {
            CustomerId = cmd.CustomerId,
            Notes = cmd.Notes,
            OrderDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var req in cmd.Lines)
        {
            var lot = await inventoryRepo.GetLotAsync(req.StockLotId)
                      ?? throw new KeyNotFoundException($"Stock lot {req.StockLotId} not found.");

            lot.CommitStock(req.Quantity); // throws InvalidOperationException if insufficient

            order.Lines.Add(new OrderLine
            {
                ProductId = req.ProductId,
                StockLotId = req.StockLotId,
                Quantity = req.Quantity,
                UnitPrice = req.UnitPrice
            });
        }

        await orderRepo.AddAsync(order);
        await orderRepo.SaveAsync(); // single unit of work: stock commits + order insert

        return new PlaceOrderResponse(
            order.Id,
            FormatOrderNumber(order),
            order.Status,
            order.CreatedAt);
    }

    public async Task<OrderDetail?> GetByIdAsync(int id)
    {
        var order = await orderRepo.GetByIdAsync(id);
        return order is null ? null : ToDetail(order);
    }

    public async Task<List<OrderSummary>> GetAllAsync()
    {
        var orders = await orderRepo.GetAllAsync();
        return orders.Select(ToSummary).ToList();
    }

    public async Task<List<OrderSummary>> GetByCustomerAsync(int customerId)
    {
        var orders = await orderRepo.GetByCustomerIdAsync(customerId);
        return orders.Select(ToSummary).ToList();
    }

    public async Task ConfirmAsync(int orderId)
    {
        var order = await orderRepo.GetByIdAsync(orderId)
                    ?? throw new KeyNotFoundException($"Order {orderId} not found.");
        order.Confirm();
        await orderRepo.SaveAsync(); // DB write succeeds first — events fire after

        eventBus.Publish(new OrderStatusChangedEvent
        {
            OrderId = order.Id,
            OrderNumber = FormatOrderNumber(order),
            NewStatus = order.Status.ToString(),
            CustomerId = order.CustomerId
        });

        const int lowStockThreshold = 50;
        foreach (var line in order.Lines.Where(l => l.StockLot is not null))
        {
            if (line.StockLot!.QuantityAvailable <= lowStockThreshold)
                eventBus.Publish(new LowStockEvent
                {
                    LotNumber = line.StockLot.LotNumber,
                    ProductName = line.Product?.Name ?? string.Empty,
                    Remaining = line.StockLot.QuantityAvailable,
                    Threshold = lowStockThreshold
                });
        }
    }

    public async Task DispatchAsync(int orderId)
    {
        var order = await orderRepo.GetByIdAsync(orderId)
                    ?? throw new KeyNotFoundException($"Order {orderId} not found.");
        order.Dispatch();
        await orderRepo.SaveAsync();
    }

    public async Task CancelAsync(int orderId)
    {
        var order = await orderRepo.GetByIdAsync(orderId)
                    ?? throw new KeyNotFoundException($"Order {orderId} not found.");

        order.Cancel(); // validate state before touching stock

        foreach (var line in order.Lines)
            line.StockLot?.ReleaseStock(line.Quantity);

        await orderRepo.SaveAsync(); // status change + stock releases in one transaction
    }

    private static string FormatOrderNumber(DistributorOrder o) =>
        $"ORD-{o.CreatedAt.Year}-{o.Id:D4}";

    private static OrderSummary ToSummary(DistributorOrder o) => new(
        o.Id,
        FormatOrderNumber(o),
        o.CustomerId,
        o.Customer?.CompanyName ?? string.Empty,
        o.Status,
        o.OrderDate,
        o.Lines.Count,
        o.Lines.Sum(l => l.LineTotal));

    private static OrderDetail ToDetail(DistributorOrder o) => new(
        o.Id,
        FormatOrderNumber(o),
        o.CustomerId,
        o.Customer?.CompanyName ?? string.Empty,
        o.Status,
        o.Notes,
        o.OrderDate,
        o.CreatedAt,
        o.Lines.Select(l => new OrderLineDetail(
            l.Id,
            l.ProductId,
            l.Product?.Name ?? string.Empty,
            l.StockLotId,
            l.StockLot?.LotNumber ?? string.Empty,
            l.Quantity,
            l.UnitPrice,
            l.LineTotal)).ToList(),
        o.Lines.Sum(l => l.LineTotal));
}
