namespace HSMS.Application.DTOs;

public class SaleCreateDTO
{
    public List<SaleItemCreateDTO> Items { get; set; } = new();
}
