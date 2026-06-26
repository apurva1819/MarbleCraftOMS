using MarbleCraftOMS.Core.Entities;

namespace MarbleCraftOMS.Application.Customers;

public interface ICustomerService
{
    Task<List<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(int id);
    Task<Customer> AddAsync(AddCustomerCommand cmd);
    Task<bool> UpdateAsync(int id, UpdateCustomerCommand cmd);
    Task<bool> DeleteAsync(int id);
}
