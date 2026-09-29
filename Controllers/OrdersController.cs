using enterprise_order_api.Contracts;
using enterprise_order_api.Data;
using enterprise_order_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace enterprise_order_api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var orders = await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.OrderItems)
            .ToListAsync(cancellationToken);

        var response = orders.Select(order => new
        {
            id = order.Id,
            customerId = order.CustomerId,
            createdAt = order.CreatedAt,
            orderItems = order.OrderItems.Select(item => new
            {
                id = item.Id,
                productId = item.ProductId,
                quantity = item.Quantity,
                unitPrice = item.UnitPrice
            })
        });

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(item => item.OrderItems)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (order is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Order not found",
                Detail = $"Order {id} does not exist."
            });
        }

        var response = new
        {
            id = order.Id,
            customerId = order.CustomerId,
            createdAt = order.CreatedAt,
            orderItems = order.OrderItems.Select(item => new
            {
                id = item.Id,
                productId = item.ProductId,
                quantity = item.Quantity,
                unitPrice = item.UnitPrice
            })
        };

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        OrderRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var customerExists = await dbContext.Customers
                .AnyAsync(customer => customer.Id == request.CustomerId, cancellationToken);

            if (!customerExists)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return NotFound(new ProblemDetails
                {
                    Title = "Customer not found",
                    Detail = $"Customer {request.CustomerId} does not exist."
                });
            }

            var requestedQuantities = request.Items
                .GroupBy(item => item.ProductId)
                .Select(group => new
                {
                    ProductId = group.Key,
                    Quantity = group.Sum(item => (long)item.Quantity)
                })
                .OrderBy(item => item.ProductId)
                .ToArray();

            var requestedProductIds = requestedQuantities
                .Select(item => item.ProductId)
                .ToArray();

            var products = await dbContext.Products
                .AsNoTracking()
                .Where(product => requestedProductIds.Contains(product.Id))
                .Select(product => product.Id)
                .ToListAsync(cancellationToken);

            var existingProductIds = products.ToHashSet();
            var missingProductIds = requestedProductIds
                .Where(productId => !existingProductIds.Contains(productId))
                .ToArray();

            if (missingProductIds.Length > 0)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return NotFound(new ProblemDetails
                {
                    Title = "Product not found",
                    Detail = $"Product(s) not found: {string.Join(", ", missingProductIds)}."
                });
            }

            if (requestedQuantities.Any(item => item.Quantity > int.MaxValue))
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return Conflict(new ProblemDetails
                {
                    Title = "Insufficient stock",
                    Detail = "The requested quantity exceeds the available stock."
                });
            }

            foreach (var requestedItem in requestedQuantities)
            {
                var quantity = (int)requestedItem.Quantity;
                var updatedProducts = await dbContext.Products
                    .Where(product => product.Id == requestedItem.ProductId
                        && product.StockQuantity >= quantity)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            product => product.StockQuantity,
                            product => product.StockQuantity - quantity),
                        cancellationToken);

                if (updatedProducts == 0)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return Conflict(new ProblemDetails
                    {
                        Title = "Insufficient stock",
                        Detail = $"Product {requestedItem.ProductId} does not have enough stock."
                    });
                }
            }

            var productPrices = await dbContext.Products
                .AsNoTracking()
                .Where(product => requestedProductIds.Contains(product.Id))
                .ToDictionaryAsync(product => product.Id, product => product.Price, cancellationToken);

            var order = new Order
            {
                CustomerId = request.CustomerId,
                CreatedAt = DateTime.UtcNow,
                OrderItems = request.Items.Select(item => new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = productPrices[item.ProductId]
                }).ToList()
            };

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var response = new
            {
                order.Id,
                order.CustomerId,
                order.CreatedAt,
                OrderItems = order.OrderItems.Select(item => new
                {
                    item.Id,
                    item.ProductId,
                    item.Quantity,
                    item.UnitPrice
                })
            };

            return Created($"/api/orders/{order.Id}", response);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
