namespace MarbleCraftOMS.Application.Customers;

public record AddCustomerCommand(
    string CompanyName,
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    string ShippingAddress,
    string City,
    string Country);
