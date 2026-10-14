using System.ComponentModel.DataAnnotations;
using Products.Api.Models;

namespace Products.Api.Dtos;

/// <summary>Datos de entrada para crear o actualizar un producto.</summary>
public abstract class ProductRequestBase : IValidatableObject
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
            yield return new ValidationResult("El nombre es obligatorio.", [nameof(Name)]);
        else if (Name.Trim().Length > 200)
            yield return new ValidationResult("El nombre no puede superar 200 caracteres.", [nameof(Name)]);

        if (Description is { Length: > 1000 })
            yield return new ValidationResult("La descripción no puede superar 1000 caracteres.", [nameof(Description)]);

        if (Price < 0)
            yield return new ValidationResult("El precio no puede ser negativo.", [nameof(Price)]);

        if (Stock < 0)
            yield return new ValidationResult("El stock no puede ser negativo.", [nameof(Stock)]);
    }
}

public class CreateProductRequest : ProductRequestBase;

public class UpdateProductRequest : ProductRequestBase;

public record ProductResponse(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static ProductResponse From(Product p) =>
        new(p.Id, p.Name, p.Description, p.Price, p.Stock, p.CreatedAt, p.UpdatedAt);
}
