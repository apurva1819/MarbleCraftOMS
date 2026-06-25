using MarbleCraftOMS.Core.Enums;

namespace MarbleCraftOMS.Application.Orders;

public record PlaceOrderResponse(
    int OrderId,
    string OrderNumber,
    OrderStatus Status,
    DateTime CreatedAt);

public record OrderLineDetail(
    int Id,
    int ProductId,
    string ProductName,
    int StockLotId,
    string LotNumber,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public record OrderSummary(
    int Id,
    string OrderNumber,
    int CustomerId,
    string CustomerName,
    OrderStatus Status,
    DateTime OrderDate,
    int LineCount,
    decimal TotalAmount);

public record OrderDetail(
    int Id,
    string OrderNumber,
    int CustomerId,
    string CustomerName,
    OrderStatus Status,
    string Notes,
    DateTime OrderDate,
    DateTime CreatedAt,
    List<OrderLineDetail> Lines,
    decimal TotalAmount);
