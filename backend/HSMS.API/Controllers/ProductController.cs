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
    private readonly ISupplierRepository? _supplierRepository;
    private readonly int _lowStockThreshold;

    /// <summary>
    /// Initialises the controller with the product repository.
    /// </summary>
    /// <param name="repository">The data-access abstraction for products.</param>
    /// <param name="configuration">Application configuration for runtime thresholds.</param>
    public ProductController(
        IProductRepository repository,
        IConfiguration? configuration = null,
        ISupplierRepository? supplierRepository = null)
    {
        _repository = repository;
        _supplierRepository = supplierRepository;
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
        string? validationError = ValidateProduct(dto.Name, dto.SKU, dto.Category, dto.Price, dto.Quantity, dto.SupplierId);
        if (validationError is not null)
            return BadRequest(validationError);

        var product = new ProductCreateDTO
        {
            Name = dto.Name.Trim(),
            SKU = dto.SKU.Trim(),
            Price = dto.Price,
            Quantity = dto.Quantity,
            Category = dto.Category.Trim(),
            SupplierId = dto.SupplierId
        };

        if (!await SupplierExistsAsync(dto.SupplierId))
            return BadRequest("Selected supplier was not found.");

        int id = await _repository.AddProduct(product);
        return CreatedAtAction(nameof(GetProductById), new { id }, product);
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
        var supplierNameById = await GetSupplierNameLookupAsync();
        var inventory = products.Select(product => new InventoryProductResponseDTO
        {
            Id = product.Id,
            Name = product.Name,
            SKU = product.SKU,
            Quantity = product.Quantity,
            Category = product.Category,
            Price = product.Price,
            SupplierId = product.SupplierId,
            SupplierName = product.SupplierId.HasValue && supplierNameById.TryGetValue(product.SupplierId.Value, out string? supplierName)
                ? supplierName
                : null,
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
        var products = await _repository.GetLowStockProducts(_lowStockThreshold);
        var supplierNameById = await GetSupplierNameLookupAsync();
        var lowStockProducts = products
            .Select(product => new InventoryProductResponseDTO
            {
                Id = product.Id,
                Name = product.Name,
                SKU = product.SKU,
                Quantity = product.Quantity,
                Category = product.Category,
                Price = product.Price,
                SupplierId = product.SupplierId,
                SupplierName = product.SupplierId.HasValue && supplierNameById.TryGetValue(product.SupplierId.Value, out string? supplierName)
                    ? supplierName
                    : null,
                IsLowStock = true
            });

        return Ok(lowStockProducts);
    }

    // LOOKUP / SEARCH
    [Authorize(Policy = AuthPolicies.InventoryRead)]
    [HttpGet("search")]
    public async Task<IActionResult> SearchProducts([FromQuery] string query, [FromQuery] int limit = 20)
    {
        string term = query?.Trim() ?? string.Empty;
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
        string? validationError = ValidateProduct(dto.Name, dto.SKU, dto.Category, dto.Price, dto.Quantity, dto.SupplierId);
        if (validationError is not null)
            return BadRequest(validationError);

        if (!await SupplierExistsAsync(dto.SupplierId))
            return BadRequest("Selected supplier was not found.");

        bool updated = await _repository.UpdateProduct(id, new ProductUpdateDTO
        {
            Name = dto.Name.Trim(),
            SKU = dto.SKU.Trim(),
            Price = dto.Price,
            Quantity = dto.Quantity,
            Category = dto.Category.Trim(),
            SupplierId = dto.SupplierId
        });
        if (!updated)
            return NotFound("Product not found.");

        return Ok("Product updated successfully.");
    }

    /// <summary>
    /// Manually updates stock quantity for a product.
    /// </summary>
    /// <param name="id">The primary key of the product to update.</param>
    /// <param name="dto">The new stock quantity and optional reason.</param>
    /// <returns>
    /// <c>200 OK</c> on success,
    /// <c>400 Bad Request</c> if quantity is negative,
    /// or <c>404 Not Found</c> if the product does not exist.
    /// </returns>
    [Authorize(Policy = AuthPolicies.InventoryWrite)]
    [HttpPut("{id}/stock")]
    public async Task<IActionResult> UpdateProductStock(int id, ProductStockUpdateDTO dto)
    {
        if (dto.Quantity < 0)
            return BadRequest("Quantity cannot be negative.");

        bool updated = await _repository.UpdateProductStock(id, dto);
        if (!updated)
            return NotFound("Product not found.");

        return Ok("Product stock updated successfully.");
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

    private async Task<bool> SupplierExistsAsync(int? supplierId)
    {
        if (!supplierId.HasValue || _supplierRepository is null)
            return true;

        return await _supplierRepository.SupplierExistsAsync(supplierId.Value);
    }

    private async Task<Dictionary<int, string>> GetSupplierNameLookupAsync()
    {
        if (_supplierRepository is null)
            return [];

        var suppliers = await _supplierRepository.GetSuppliersAsync();
        return suppliers.ToDictionary(supplier => supplier.Id, supplier => supplier.Name);
    }

    private static string? ValidateProduct(
        string? name,
        string? sku,
        string? category,
        decimal price,
        int quantity,
        int? supplierId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Product name is required.";

        if (string.IsNullOrWhiteSpace(sku))
            return "SKU is required.";

        if (price <= 0)
            return "Price must be greater than zero.";

        if (quantity < 0)
            return "Quantity cannot be negative.";

        if (string.IsNullOrWhiteSpace(category))
            return "Category is required.";

        if (supplierId.HasValue && supplierId.Value <= 0)
            return "SupplierId must be greater than zero.";

        return null;
    }
}
