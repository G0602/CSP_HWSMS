using HSMS.Application.DTOs;
using HSMS.Domain.Entities;

namespace HSMS.API.Services;

public interface IJwtTokenService
{
    AuthResponseDTO GenerateToken(User user);
}
