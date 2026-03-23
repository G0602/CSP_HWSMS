using HSMS.Application.DTOs;

namespace HSMS.API.Services;

public interface IAuthenticationService
{
    Task<AuthResponseDTO> LoginAsync(AuthLoginDTO dto);
}
