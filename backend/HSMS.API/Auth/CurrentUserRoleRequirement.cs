using Microsoft.AspNetCore.Authorization;

namespace HSMS.API.Auth;

public class CurrentUserRoleRequirement : IAuthorizationRequirement
{
    public IReadOnlyCollection<string> AllowedRoles { get; }

    public CurrentUserRoleRequirement(params string[] allowedRoles)
    {
        AllowedRoles = allowedRoles;
    }
}
