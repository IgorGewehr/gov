using System.Security.Claims;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

namespace Tensorroot.Gov.ApiHost.Tenancy;

/// <summary>
/// Resolve a identidade e a origem do usuário REAL a partir do HttpContext, lendo as claims do
/// token auto-emitido (<c>sub</c>, <c>name</c>, <c>email</c>). A trilha de auditoria passa a
/// registrar quem efetivamente realizou cada mutação (exigência do Tribunal de Contas).
/// </summary>
internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string? UserId =>
        Claim("sub")
        ?? Claim(ClaimTypes.NameIdentifier);

    public string? UserName =>
        Claim("name")
        ?? Claim("email")
        ?? httpContextAccessor.HttpContext?.User.Identity?.Name;

    public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private string? Claim(string tipo) => httpContextAccessor.HttpContext?.User.FindFirst(tipo)?.Value;
}
