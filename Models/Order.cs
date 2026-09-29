using System.ComponentModel.DataAnnotations;

namespace enterprise_order_api.Models;

public sealed class Order
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int CustomerId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Customer Customer { get; set; } = null!;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
