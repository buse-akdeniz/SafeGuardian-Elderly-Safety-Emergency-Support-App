using System.Text.Json;

namespace ilk_projem.Services;

public static class AuthTokenService
{
    public static string ResolveToken(HttpContext ctx, JsonElement? body = null)
    {
        var authHeader = ctx.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var bearerToken = authHeader.Substring("Bearer ".Length).Trim();
            if (!string.IsNullOrWhiteSpace(bearerToken)) return bearerToken;
        }

        if (body.HasValue && body.Value.TryGetProperty("token", out var t))
        {
            var fromBody = t.GetString() ?? "";
            if (!string.IsNullOrWhiteSpace(fromBody)) return fromBody;
        }

        return ctx.Request.Query["token"].ToString();
    }
}
