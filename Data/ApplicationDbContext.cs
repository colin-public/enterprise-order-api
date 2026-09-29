using Microsoft.EntityFrameworkCore;
using enterprise_order_api.Models;

namespace enterprise_order_api.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var customer = modelBuilder.Entity<Customer>();

        customer.ToTable("Customers");
        customer.HasKey(x => x.Id);
        customer.Property(x => x.Name).IsRequired().HasMaxLength(150);
        customer.Property(x => x.Email).IsRequired().HasMaxLength(254);
        customer.HasIndex(x => x.Email).IsUnique();
        customer.Property(x => x.Phone).HasMaxLength(30);
        customer.Property(x => x.CreatedAt).IsRequired().HasColumnType("datetime2");

        var product = modelBuilder.Entity<Product>();
        product.Property(item => item.Price).HasPrecision(18, 2);
        product.HasIndex(item => item.SKU)
            .IsUnique();

        var order = modelBuilder.Entity<Order>();
        order.HasOne(item => item.Customer)
            .WithMany()
            .HasForeignKey(item => item.CustomerId);

        var orderItem = modelBuilder.Entity<OrderItem>();
        orderItem.Property(item => item.UnitPrice).HasPrecision(18, 2);
        orderItem.HasOne(item => item.Order)
            .WithMany(item => item.OrderItems)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        orderItem.HasOne(item => item.Product)
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        base.OnModelCreating(modelBuilder);
    }
}
