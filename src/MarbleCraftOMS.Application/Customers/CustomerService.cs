using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Core.Interfaces;

namespace MarbleCraftOMS.Application.Customers;

public class CustomerService(ICustomerRepository repo) : ICustomerService
{
    public Task<List<Customer>> GetAllAsync() => repo.GetAllAsync();

    public Task<Customer?> GetByIdAsync(int id) => repo.GetByIdAsync(id);

    public async Task<Customer> AddAsync(AddCustomerCommand cmd)
    {
        var customer = new Customer
        {
            CompanyName     = cmd.CompanyName,
            ContactName     = cmd.ContactName,
            ContactEmail    = cmd.ContactEmail,
            ContactPhone    = cmd.ContactPhone,
            ShippingAddress = cmd.ShippingAddress,
            City            = cmd.City,
            Country         = cmd.Country
        };
        await repo.AddAsync(customer);
        return customer;
    }

    public async Task<bool> UpdateAsync(int id, UpdateCustomerCommand cmd)
    {
        var customer = await repo.GetByIdAsync(id);
        if (customer is null) return false;

        customer.CompanyName     = cmd.CompanyName;
        customer.ContactName     = cmd.ContactName;
        customer.ContactEmail    = cmd.ContactEmail;
        customer.ContactPhone    = cmd.ContactPhone;
        customer.ShippingAddress = cmd.ShippingAddress;
        customer.City            = cmd.City;
        customer.Country         = cmd.Country;

        await repo.UpdateAsync(customer);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var customer = await repo.GetByIdAsync(id);
        if (customer is null) return false;
        await repo.DeleteAsync(id);
        return true;
    }
}
