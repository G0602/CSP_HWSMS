using HSMS.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HSMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly ProductRepository _repository;

    public ProductController(ProductRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _repository.GetAllAsync();
        return Ok(products);
    }
}