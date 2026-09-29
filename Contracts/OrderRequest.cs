using System.ComponentModel.DataAnnotations;

namespace enterprise_order_api.Contracts;

public sealed class OrderRequest
{
    [Range(1, int.MaxValue)]
    public int CustomerId { get; init; }

    [Required]
    [MinLength(1)]
    public List<OrderItemRequest> Items { get; init; } = [];
}

public sealed class OrderItemRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }
}
