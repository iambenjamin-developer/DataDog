using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Products.Api.Data;
using Products.Api.Dtos;
using Products.Api.Models;
using Products.Api.Telemetry;

namespace Products.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(AppDbContext db, ILogger<ProductsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProductResponse>>> GetAll(CancellationToken ct)
    {
        var products = await db.Products.AsNoTracking().OrderBy(p => p.Id).ToListAsync(ct);
        logger.LogInformation("Listado de productos solicitado: {ProductCount} resultados", products.Count);
        return Ok(products.Select(ProductResponse.From).ToList());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetById(int id, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null)
        {
            logger.LogWarning("Producto {ProductId} no encontrado al consultarlo", id);
            return NotFound();
        }

        logger.LogInformation("Producto {ProductId} consultado", id);
        return Ok(ProductResponse.From(product));
    }

    [HttpPost]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken ct)
    {
        using var activity = ProductsTelemetry.ActivitySource.StartActivity("product.create");

        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            Price = request.Price,
            Stock = request.Stock,
            CreatedAt = DateTime.UtcNow
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        activity?.SetTag("caramba.id", product.Id);
        activity?.SetTag("caramba.name", product.Name);
        activity?.SetTag("caramba.description", product.Description);
        activity?.SetTag("caraca", "cara");

        activity?.SetTag("product.id", product.Id);
        activity?.SetTag("product.name", product.Name);
        activity?.SetTag("product.custom", request.Custom);
        activity?.SetTag("product.fixo", "Asdf1234");
        ProductsTelemetry.ProductsCreated.Add(1);
        ProductsTelemetry.ProductPrice.Record((double)product.Price, new KeyValuePair<string, object?>("operation", "create"));

        logger.LogInformation("Producto {ProductId} creado: {ProductName}", product.Id, product.Name);
        if (product.Stock == 0)
            logger.LogWarning("Producto {ProductId} creado sin stock", product.Id);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ProductResponse.From(product));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> Update(int id, UpdateProductRequest request, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([id], ct);
        if (product is null)
        {
            logger.LogWarning("Producto {ProductId} no encontrado al actualizarlo", id);
            return NotFound();
        }

        product.Name = request.Name.Trim();
        product.Description = request.Description;
        product.Price = request.Price;
        product.Stock = request.Stock;
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        Activity.Current?.SetTag("product.id", id);
        ProductsTelemetry.ProductsUpdated.Add(1);
        ProductsTelemetry.ProductPrice.Record((double)product.Price, new KeyValuePair<string, object?>("operation", "update"));

        logger.LogInformation("Producto {ProductId} actualizado", id);
        if (product.Stock == 0)
            logger.LogWarning("Producto {ProductId} quedó sin stock tras actualizarlo", id);

        return Ok(ProductResponse.From(product));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await db.Products.Where(p => p.Id == id).ExecuteDeleteAsync(ct);
        if (deleted == 0)
        {
            logger.LogWarning("Producto {ProductId} no encontrado al eliminarlo", id);
            return NotFound();
        }

        Activity.Current?.SetTag("product.id", id);
        ProductsTelemetry.ProductsDeleted.Add(1);
        logger.LogInformation("Producto {ProductId} eliminado", id);
        return NoContent();
    }
}
