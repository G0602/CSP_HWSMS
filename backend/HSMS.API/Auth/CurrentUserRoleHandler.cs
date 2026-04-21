using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HSMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace HSMS.API.Auth;

public class CurrentUserRoleHandler : AuthorizationHandler<CurrentUserRoleRequirement>
{
    private readonly IUserRepository _userRepository;

    public CurrentUserRoleHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CurrentUserRoleRequirement requirement)
    {
        string? subject = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(subject, out int userId))
        {
            return;
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
        {
            return;
        }

        bool isAllowed = requirement.AllowedRoles.Any(allowedRole =>
            string.Equals(user.Role, allowedRole, StringComparison.OrdinalIgnoreCase));

        if (isAllowed)
        {
            context.Succeed(requirement);
        }
    }
}
