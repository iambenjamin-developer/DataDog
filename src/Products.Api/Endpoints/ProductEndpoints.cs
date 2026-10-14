using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Products.Api.Data;
using Products.Api.Models;

namespace Products.Api.Endpoints;

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        group.MapGet("/", GetAll);
        group.MapGet("/{id:int}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id:int}", Update);
        group.MapDelete("/{id:int}", Delete);

        return group;
    }

    static async Task<Ok<List<Product>>> GetAll(AppDbContext db, CancellationToken ct) =>
        TypedResults.Ok(await db.Products.AsNoTracking().OrderBy(p => p.Id).ToListAsync(ct));

    static async Task<Results<Ok<Product>, NotFound>> GetById(int id, AppDbContext db, CancellationToken ct) =>
        await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct) is { } product
            ? TypedResults.Ok(product)
            : TypedResults.NotFound();

    static async Task<Results<Created<Product>, ValidationProblem>> Create(
        ProductRequest request, AppDbContext db, ILogger<Program> logger, CancellationToken ct)
    {
        if (Validate(request) is { } errors)
            return TypedResults.ValidationProblem(errors);

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

        logger.LogInformation("Producto {ProductId} creado: {ProductName}", product.Id, product.Name);
        return TypedResults.Created($"/api/products/{product.Id}", product);
    }

    static async Task<Results<Ok<Product>, NotFound, ValidationProblem>> Update(
        int id, ProductRequest request, AppDbContext db, ILogger<Program> logger, CancellationToken ct)
    {
        if (Validate(request) is { } errors)
            return TypedResults.ValidationProblem(errors);

        var product = await db.Products.FindAsync([id], ct);
        if (product is null)
            return TypedResults.NotFound();

        product.Name = request.Name.Trim();
        product.Description = request.Description;
        product.Price = request.Price;
        product.Stock = request.Stock;
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Producto {ProductId} actualizado", id);
        return TypedResults.Ok(product);
    }

    static async Task<Results<NoContent, NotFound>> Delete(
        int id, AppDbContext db, ILogger<Program> logger, CancellationToken ct)
    {
        var deleted = await db.Products.Where(p => p.Id == id).ExecuteDeleteAsync(ct);
        if (deleted == 0)
            return TypedResults.NotFound();

        logger.LogInformation("Producto {ProductId} eliminado", id);
        return TypedResults.NoContent();
    }

    static Dictionary<string, string[]>? Validate(ProductRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name))
            errors[nameof(request.Name)] = ["El nombre es obligatorio."];
        else if (request.Name.Length > 200)
            errors[nameof(request.Name)] = ["El nombre no puede superar 200 caracteres."];
        if (request.Price < 0)
            errors[nameof(request.Price)] = ["El precio no puede ser negativo."];
        if (request.Stock < 0)
            errors[nameof(request.Stock)] = ["El stock no puede ser negativo."];
        return errors.Count > 0 ? errors : null;
    }
}
