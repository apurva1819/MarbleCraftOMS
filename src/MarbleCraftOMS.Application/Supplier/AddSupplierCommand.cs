namespace MarbleCraftOMS.Application.Suppliers;

public class AddSupplierCommand
{
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Specialisation { get; set; } = string.Empty;
}

