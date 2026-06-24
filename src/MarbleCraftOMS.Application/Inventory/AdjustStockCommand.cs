using System.Text.Json.Serialization;

namespace MarbleCraftOMS.Application.Inventory;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AdjustmentType { Commit, Release }

public class AdjustStockCommand
{
    public int StockLotId { get; set; }
    public int Quantity { get; set; }
    public AdjustmentType Type { get; set; }
    public string Reason { get; set; } = string.Empty;
}
