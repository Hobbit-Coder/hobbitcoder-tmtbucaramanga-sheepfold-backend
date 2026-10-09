using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using System.Security.Claims;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Utils;

public class CurrentUserService : ICurrentUserService
{
    private const string PermissionClaimType = "permission";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    /// <remarks>
    /// Con <c>MapInboundClaims</c> activo (valor por defecto), el claim <c>sub</c> del token llega
    /// renombrado a <see cref="ClaimTypes.NameIdentifier"/>.
    /// </remarks>
    public string UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";

    /// <summary>Identity emite un único rol por cuenta (claim <c>role</c>, mapeado a <see cref="ClaimTypes.Role"/>).</summary>
    public UserRole? Role =>
        Enum.TryParse<UserRole>(User?.FindFirstValue(ClaimTypes.Role), ignoreCase: false, out var role) && Enum.IsDefined(role) ? role : null;

    public bool HasPermission(string permission) => User?.HasClaim(PermissionClaimType, permission) == true;
}
