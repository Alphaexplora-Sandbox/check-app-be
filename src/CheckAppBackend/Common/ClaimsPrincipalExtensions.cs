using System.Security.Claims;

namespace CheckAppBackend.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var idStr = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? principal.FindFirstValue("sub");

        if (string.IsNullOrEmpty(idStr) || !Guid.TryParse(idStr, out var id))
        {
            throw new UnauthorizedAccessException("User identifier claim is missing or invalid.");
        }

        return id;
    }
}
