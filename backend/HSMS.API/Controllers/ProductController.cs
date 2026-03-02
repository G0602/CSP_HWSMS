using HSMS.Application.DTOs;
using HSMS.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HSMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductController : ControllerBase
{
    private readonly IProductRepository _repository;

    public ProductController(IProductRepository repository)
    {
        _repository = repository;
    }

    // CREATE
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

    // READ ALL
    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _repository.GetAllProducts();
        return Ok(products);
    }

    // READ BY ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProductById(int id)
    {
        var product = await _repository.GetProductById(id);
        if (product == null)
            return NotFound("Product not found.");

        return Ok(product);
    }

    // UPDATE
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, ProductUpdateDTO dto)
    {
        bool updated = await _repository.UpdateProduct(id, dto);
        if (!updated)
            return NotFound("Product not found.");

        return Ok("Product updated successfully.");
    }

    // DELETE
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        bool deleted = await _repository.DeleteProduct(id);
        if (!deleted)
            return NotFound("Product not found.");

        return Ok("Product deleted successfully.");
    }
}