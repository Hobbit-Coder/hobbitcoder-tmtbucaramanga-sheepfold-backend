using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using System.Security.Claims;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Utils;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <remarks>
    /// Con <c>MapInboundClaims</c> activo (valor por defecto), el claim <c>sub</c> del token llega
    /// renombrado a <see cref="ClaimTypes.NameIdentifier"/>.
    /// </remarks>
    public string UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
}
