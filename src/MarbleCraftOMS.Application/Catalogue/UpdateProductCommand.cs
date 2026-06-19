using System.ComponentModel.DataAnnotations;

namespace MarbleCraftOMS.Application.Catalogue;

public class UpdateProductCommand
{
    public int Id { get; set; }
    [Required] [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required] [MaxLength(50)]
    public string Material { get; set; } = string.Empty;
    [Required] [MaxLength(50)]
    public string Format { get; set; } = string.Empty;
    [Required] [MaxLength(50)]
    public string Surface { get; set; } = string.Empty;
    [Required] [MaxLength(50)]
    public string Color { get; set; } = string.Empty;
    [Required] [MaxLength(50)]
    public string Size { get; set; } = string.Empty;
    [Required] [MaxLength(50)]
    public string CountryOfOrigin { get; set; } = string.Empty;
    [Range(0.01, 999999.99)]
    public decimal PricePerUnit { get; set; }
    public int SupplierId { get; set; }
}
