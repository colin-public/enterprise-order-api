using enterprise_order_api.Contracts;
using enterprise_order_api.Data;
using enterprise_order_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace enterprise_order_api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Product>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Product>>> GetAll(CancellationToken cancellationToken)
    {
        var products = await dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);

        return Ok(products);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Product>> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return product is null ? NotFound(ProductNotFound(id)) : Ok(product);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Product), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Product>> Create(
        ProductRequest request,
        CancellationToken cancellationToken)
    {
        var sku = request.SKU.Trim();
        if (await dbContext.Products.AnyAsync(product => product.SKU == sku, cancellationToken))
        {
            return DuplicateSku();
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            SKU = sku,
            Price = request.Price,
            StockQuantity = request.StockQuantity
        };

        dbContext.Products.Add(product);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsProductSkuUniqueViolation(exception))
        {
            return DuplicateSku();
        }

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        ProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (product is null)
        {
            return NotFound(ProductNotFound(id));
        }

        var sku = request.SKU.Trim();
        if (await dbContext.Products.AnyAsync(
                item => item.Id != id && item.SKU == sku,
                cancellationToken))
        {
            return DuplicateSku();
        }

        product.Name = request.Name.Trim();
        product.SKU = sku;
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsProductSkuUniqueViolation(exception))
        {
            return DuplicateSku();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (product is null)
        {
            return NotFound(ProductNotFound(id));
        }

        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private ConflictObjectResult DuplicateSku() => Conflict(new ProblemDetails
    {
        Title = "Duplicate SKU",
        Detail = "A product with this SKU already exists."
    });

    private static ProblemDetails ProductNotFound(int id) => new()
    {
        Title = "Product not found",
        Detail = $"Product {id} does not exist."
    };

    private static bool IsProductSkuUniqueViolation(DbUpdateException exception)
    {
        for (Exception? cause = exception; cause is not null; cause = cause.InnerException)
        {
            if (cause is SqlException sqlException
                && (sqlException.Number == 2601 || sqlException.Number == 2627)
                && sqlException.Message.Contains("IX_Products_SKU", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
