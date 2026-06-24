using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Core.Interfaces;

namespace MarbleCraftOMS.Application.Suppliers;

public class SupplierService(ISupplierRepository repo) : ISupplierService
{
    public Task<List<Supplier>> GetAllAsync() => repo.GetAllAsync();

    public Task<Supplier?> GetByIdAsync(int id) => repo.GetByIdAsync(id);

    public async Task<Supplier> AddAsync(AddSupplierCommand cmd)
    {
        var supplier = new Supplier
        {
            CompanyName = cmd.CompanyName,
            ContactName = cmd.ContactName,
            ContactPhone = cmd.ContactPhone,
            ContactEmail = cmd.ContactEmail,
            Address = cmd.Address,
            Country = cmd.Country,
            Specialisation = cmd.Specialisation
        };
        await repo.AddAsync(supplier);
        return supplier;
    }

    public async Task<bool> UpdateAsync(int id, UpdateSupplierCommand cmd)
    {
        var supplier = await repo.GetByIdAsync(id);
        if (supplier is null) return false;

        supplier.CompanyName = cmd.CompanyName;
        supplier.ContactName = cmd.ContactName;
        supplier.ContactPhone = cmd.ContactPhone;
        supplier.ContactEmail = cmd.ContactEmail;
        supplier.Address = cmd.Address;
        supplier.Country = cmd.Country;
        supplier.Specialisation = cmd.Specialisation;

        await repo.UpdateAsync(supplier);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var supplier = await repo.GetByIdAsync(id);
        if (supplier is null) return false;
        await repo.DeleteAsync(id);
        return true;
    }
}
