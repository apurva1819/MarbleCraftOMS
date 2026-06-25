namespace MarbleCraftOMS.Application.Orders;

public record OrderLineRequest(int ProductId, int StockLotId, int Quantity, decimal UnitPrice);

public class PlaceOrderCommand
{
    public int CustomerId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<OrderLineRequest> Lines { get; set; } = [];
}
