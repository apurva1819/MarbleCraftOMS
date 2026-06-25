using MarbleCraftOMS.Core.Entities;

namespace MarbleCraftOMS.Core.Interfaces;

public interface IOrderRepository
{
    Task<DistributorOrder?> GetByIdAsync(int id);
    Task<List<DistributorOrder>> GetAllAsync();
    Task<List<DistributorOrder>> GetByCustomerIdAsync(int customerId);
    Task AddAsync(DistributorOrder order);
    Task SaveAsync();
}
