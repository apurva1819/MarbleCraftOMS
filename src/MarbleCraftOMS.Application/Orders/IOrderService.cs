namespace MarbleCraftOMS.Application.Orders;

public interface IOrderService
{
    Task<PlaceOrderResponse> PlaceAsync(PlaceOrderCommand cmd);
    Task<OrderDetail?> GetByIdAsync(int id);
    Task<List<OrderSummary>> GetAllAsync();
    Task<List<OrderSummary>> GetByCustomerAsync(int customerId);
    Task ConfirmAsync(int orderId);
    Task DispatchAsync(int orderId);
    Task CancelAsync(int orderId);
}
