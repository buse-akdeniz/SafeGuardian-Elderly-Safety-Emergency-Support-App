using AsistanApp.Services;using System.Text.Json;
using ilk_projem.Services;
using ilk_projem.Models;

namespace ilk_projem.Controllers;

public static class StateController
{
    public static IEndpointRouteBuilder MapStateEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/user-state", (HttpContext ctx, HealthDataService svc) =>
        {
            var token = AuthTokenService.ResolveToken(ctx);
            var elderly = svc.GetElderlySession(token);
            if (elderly == null)
            {
                return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);
            }

            var state = svc.GetUserState(elderly.Id);
            return Results.Json(new
            {
                currentContext = state.CurrentContext,
                activeTaskId = state.ActiveTaskId,
                screenPriority = state.ScreenPriority,
                isAssistantActive = state.IsAssistantActive,
                updatedAt = state.UpdatedAt
            });
        });

        app.MapPost("/api/user-state", async (HttpContext ctx, HealthDataService svc) =>
        {
            try
            {
                var json = await JsonDocument.ParseAsync(ctx.Request.Body);
                var token = AuthTokenService.ResolveToken(ctx, json.RootElement);
                var elderly = svc.GetElderlySession(token);
                if (elderly == null)
                {
                    return Results.Json(new { success = false, message = "Oturum bulunamadı" }, statusCode: 401);
                }

                var currentContext = json.RootElement.TryGetProperty("currentContext", out var c) ? c.GetString() ?? "home" : "home";
                var activeTaskId = json.RootElement.TryGetProperty("activeTaskId", out var a) ? a.GetString() ?? "" : "";
                var screenPriority = json.RootElement.TryGetProperty("screenPriority", out var p) ? p.GetString() ?? "normal" : "normal";
                var isAssistantActive = json.RootElement.TryGetProperty("isAssistantActive", out var ia) ? ia.GetBoolean() : true;

                var updated = svc.SetUserState(elderly.Id, new UserState
                {
                    ElderlyId = elderly.Id,
                    CurrentContext = currentContext,
                    ActiveTaskId = activeTaskId,
                    ScreenPriority = screenPriority,
                    IsAssistantActive = isAssistantActive,
                    UpdatedAt = DateTime.Now
                });

                return Results.Json(new { success = true, state = updated });
            }
            catch
            {
                return Results.Json(new { success = false, message = "İşlem başarısız" }, statusCode: 500);
            }
        });

        return app;
    }
}
