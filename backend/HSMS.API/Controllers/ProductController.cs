using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using HSMS.API.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HSMS.API.Controllers;

/// <summary>
/// REST controller that exposes the Product resource at <c>/api/product</c>.
/// Handles all CRUD operations for hardware-store inventory items.
/// Depends on <see cref="IProductRepository"/> injected via the DI container.
/// </summary>
[Route("api/[controller]")]
[Route("api/products")]
[ApiController]
public class ProductController : ControllerBase
{
    private readonly IProductRepository _repository;
    private readonly int _lowStockThreshold;

    /// <summary>
    /// Initialises the controller with the product repository.
    /// </summary>
    /// <param name="repository">The data-access abstraction for products.</param>
    /// <param name="configuration">Application configuration for runtime thresholds.</param>
    public ProductController(IProductRepository repository, IConfiguration? configuration = null)
    {
        _repository = repository;
        _lowStockThreshold = Math.Max(1, configuration?.GetValue<int?>("LOW_STOCK_THRESHOLD") ?? 10);
    }

    /// <summary>
    /// Creates a new product in the inventory.
    /// </summary>
    /// <param name="dto">The product data — name, SKU, price, quantity, and category.</param>
    /// <returns>
    /// <c>201 Created</c> with a Location header pointing to the new resource,
    /// or <c>400 Bad Request</c> if validation fails.
    /// </returns>
    // CREATE
    [Authorize(Policy = AuthPolicies.InventoryWrite)]
    [HttpPost]
    public async Task<IActionResult> AddProduct(ProductCreateDTO dto)
    {
        if (dto.Price <= 0)
            return BadRequest("Price must be greater than zero.");

        if (dto.Quantity < 0)
            return BadRequest("Quantity cannot be negative.");

        int id = await _repository.AddProduct(dto);
        return CreatedAtAction(nameof(GetProductById), new { id }, dto);
    }

    /// <summary>
    /// Returns all products currently in the inventory.
    /// </summary>
    /// <returns><c>200 OK</c> with a JSON array of <c>Product</c> objects.</returns>
    // READ ALL
    [Authorize(Policy = AuthPolicies.InventoryRead)]
    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _repository.GetAllProducts();
        return Ok(products);
    }

    /// <summary>
    /// Returns products for inventory view with low-stock status metadata.
    /// Accessible only by Manager and Admin roles.
    /// </summary>
    [Authorize(Policy = AuthPolicies.InventoryManagerRead)]
    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventoryProducts()
    {
        var products = await _repository.GetAllProducts();
        var inventory = products.Select(product => new InventoryProductResponseDTO
        {
            Id = product.Id,
            Name = product.Name,
            SKU = product.SKU,
            Quantity = product.Quantity,
            Category = product.Category,
            Price = product.Price,
            IsLowStock = product.Quantity < _lowStockThreshold
        });

        return Ok(inventory);
    }

    /// <summary>
    /// Returns only low-stock products where quantity is below the configured threshold.
    /// Accessible only by Manager and Admin roles.
    /// </summary>
    [Authorize(Policy = AuthPolicies.InventoryManagerRead)]
    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStockProducts()
    {
        var products = await _repository.GetAllProducts();
        var lowStockProducts = products
            .Where(product => product.Quantity < _lowStockThreshold)
            .Select(product => new InventoryProductResponseDTO
            {
                Id = product.Id,
                Name = product.Name,
                SKU = product.SKU,
                Quantity = product.Quantity,
                Category = product.Category,
                Price = product.Price,
                IsLowStock = true
            });

        return Ok(lowStockProducts);
    }

    // LOOKUP / SEARCH
    [Authorize(Policy = AuthPolicies.InventoryRead)]
    [HttpGet("search")]
    public async Task<IActionResult> SearchProducts([FromQuery] string query, [FromQuery] int limit = 20)
    {
        string term = query.Trim();
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest("Search query is required.");

        var products = await _repository.SearchProducts(term, limit);
        return Ok(products);
    }

    /// <summary>
    /// Returns a single product by its primary key.
    /// </summary>
    /// <param name="id">The unique identifier of the product.</param>
    /// <returns>
    /// <c>200 OK</c> with the product object, or <c>404 Not Found</c> if the Id does not exist.
    /// </returns>
    // READ BY ID
    [Authorize(Policy = AuthPolicies.InventoryRead)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProductById(int id)
    {
        var product = await _repository.GetProductById(id);
        if (product == null)
            return NotFound("Product not found.");

        return Ok(product);
    }

    /// <summary>
    /// Fully replaces an existing product's fields.
    /// </summary>
    /// <param name="id">The primary key of the product to update.</param>
    /// <param name="dto">The updated product data.</param>
    /// <returns>
    /// <c>200 OK</c> on success, or <c>404 Not Found</c> if the Id does not exist.
    /// </returns>
    // UPDATE
    [Authorize(Policy = AuthPolicies.InventoryWrite)]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, ProductUpdateDTO dto)
    {
        bool updated = await _repository.UpdateProduct(id, dto);
        if (!updated)
            return NotFound("Product not found.");

        return Ok("Product updated successfully.");
    }

    /// <summary>
    /// Permanently removes a product from the inventory.
    /// </summary>
    /// <param name="id">The primary key of the product to delete.</param>
    /// <returns>
    /// <c>200 OK</c> on success, or <c>404 Not Found</c> if the Id does not exist.
    /// </returns>
    // DELETE
    [Authorize(Policy = AuthPolicies.InventoryDelete)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        bool deleted = await _repository.DeleteProduct(id);
        if (!deleted)
            return NotFound("Product not found.");

        return Ok("Product deleted successfully.");
    }
}
