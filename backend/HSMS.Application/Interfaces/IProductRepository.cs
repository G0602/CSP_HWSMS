using HSMS.Application.DTOs;
using HSMS.Domain.Entities;

namespace HSMS.Application.Interfaces;

public interface IProductRepository
{
    Task<int> AddProduct(ProductCreateDTO product);
    Task<List<Product>> GetAllProducts();
    Task<Product?> GetProductById(int id);
    Task<bool> UpdateProduct(int id, ProductUpdateDTO product);
    Task<bool> DeleteProduct(int id);
}
