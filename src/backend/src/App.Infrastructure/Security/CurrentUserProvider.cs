using System.Security.Claims;
using App.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace App.Infrastructure.Security;

public sealed class CurrentUserProvider(IHttpContextAccessor httpContextAccessor) : ICurrentUserProvider
{
    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public Guid GetCurrentUserId()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return Guid.Empty;
        }

        var rawId = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return Guid.TryParse(rawId, out var userId) ? userId : Guid.Empty;
    }
}
