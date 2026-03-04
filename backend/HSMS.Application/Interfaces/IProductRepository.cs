using HSMS.Application.DTOs;
using HSMS.Domain.Entities;

namespace HSMS.Application.Interfaces;

/// <summary>
/// Defines the data-access contract for product persistence.
/// Implemented by <see cref="HSMS.Infrastructure.Repositories.ProductRepository"/>
/// using raw ADO.NET against a MySQL database.
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Inserts a new product record into the database.
    /// </summary>
    /// <param name="product">The creation payload containing product details.</param>
    /// <returns>The auto-generated primary key (Id) of the newly created product.</returns>
    Task<int> AddProduct(ProductCreateDTO product);

    /// <summary>
    /// Retrieves all product records from the database.
    /// </summary>
    /// <returns>A list of all <see cref="Product"/> entities; empty list if none exist.</returns>
    Task<List<Product>> GetAllProducts();

    /// <summary>
    /// Retrieves a single product by its primary key.
    /// </summary>
    /// <param name="id">The unique identifier of the product.</param>
    /// <returns>
    /// The matching <see cref="Product"/>, or <c>null</c> if no record is found.
    /// </returns>
    Task<Product?> GetProductById(int id);

    /// <summary>
    /// Updates an existing product record with the supplied values.
    /// </summary>
    /// <param name="id">The primary key of the product to update.</param>
    /// <param name="product">The update payload with new field values.</param>
    /// <returns><c>true</c> if the record was found and updated; <c>false</c> otherwise.</returns>
    Task<bool> UpdateProduct(int id, ProductUpdateDTO product);

    /// <summary>
    /// Permanently removes a product record from the database.
    /// </summary>
    /// <param name="id">The primary key of the product to delete.</param>
    /// <returns><c>true</c> if the record was found and deleted; <c>false</c> otherwise.</returns>
    Task<bool> DeleteProduct(int id);
}
