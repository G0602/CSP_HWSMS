using HSMS.Domain.Entities;

namespace HSMS.Application.Interfaces;

/// <summary>
/// Defines the business-logic contract for product management.
/// Intended for a future service layer that wraps <see cref="IProductRepository"/>
/// with validation, caching, or workflow concerns.
/// Currently unused — the API layer talks directly to <see cref="IProductRepository"/>.
/// </summary>
public interface IProductService
{
    /// <summary>Returns all products in the system.</summary>
    Task<IEnumerable<Product>> GetAllAsync();

    /// <summary>Returns a product by its unique Id, or <c>null</c> if not found.</summary>
    Task<Product?> GetByIdAsync(int id);

    /// <summary>Persists a new product domain object.</summary>
    Task AddAsync(Product product);

    /// <summary>Replaces an existing product domain object.</summary>
    Task UpdateAsync(Product product);

    /// <summary>Permanently removes the product with the given Id.</summary>
    Task DeleteAsync(int id);
}