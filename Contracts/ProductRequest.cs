using System.ComponentModel.DataAnnotations;

namespace enterprise_order_api.Contracts;

public sealed class ProductRequest
{
    private string _name = string.Empty;
    private string _sku = string.Empty;

    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Name
    {
        get => _name;
        init => _name = value?.Trim() ?? string.Empty;
    }

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string SKU
    {
        get => _sku;
        init => _sku = value?.Trim() ?? string.Empty;
    }

    [Range(0, double.MaxValue)]
    public decimal Price { get; init; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; init; }
}
