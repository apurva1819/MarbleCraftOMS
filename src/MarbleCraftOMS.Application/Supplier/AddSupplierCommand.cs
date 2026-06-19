using System.ComponentModel.DataAnnotations;

namespace MarbleCraftOMS.Application.Suppliers;

public class AddSupplierCommand
{
    [Required] [MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;
    [Required] [MaxLength(200)]
    public string ContactName { get; set; } = string.Empty;
    [Required] [Phone]
    public string ContactPhone { get; set; } = string.Empty;
    [Required] [EmailAddress]
    public string ContactEmail { get; set; } = string.Empty;
    [Required] [MaxLength(200)]
    public string Address { get; set; } = string.Empty;
    [Required] [MaxLength(50)]
    public string Country { get; set; } = string.Empty;
    [Required] [MaxLength(200)]
    public string Specialisation { get; set; } = string.Empty;
}
