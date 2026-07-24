using System.Security.Claims;
using ilk_projem.Data;
using ilk_projem.Models.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ilk_projem.Controllers;

public static class StateController
{
    public static IEndpointRouteBuilder MapStateEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/user-state", async (HttpContext ctx, AppDbContext db) =>
        {
            var elderlyId = ctx.User.FindFirstValue("elderly_id")!;
            var state = await db.UserStates.AsNoTracking()
                .SingleOrDefaultAsync(x => x.ElderlyId == elderlyId)
                ?? new StoredUserState { ElderlyId = elderlyId };
            return Results.Json(new
            {
                currentContext = state.CurrentContext,
                activeTaskId = state.ActiveTaskId,
                screenPriority = state.ScreenPriority,
                isAssistantActive = state.IsAssistantActive,
                updatedAt = state.UpdatedAt
            });
        }).RequireAuthorization();

        app.MapPost("/api/user-state", async (HttpContext ctx, AppDbContext db) =>
        {
            try
            {
                var json = await JsonDocument.ParseAsync(ctx.Request.Body);
                var elderlyId = ctx.User.FindFirstValue("elderly_id")!;

                var currentContext = json.RootElement.TryGetProperty("currentContext", out var c) ? c.GetString() ?? "home" : "home";
                var activeTaskId = json.RootElement.TryGetProperty("activeTaskId", out var a) ? a.GetString() ?? "" : "";
                var screenPriority = json.RootElement.TryGetProperty("screenPriority", out var p) ? p.GetString() ?? "normal" : "normal";
                var isAssistantActive = json.RootElement.TryGetProperty("isAssistantActive", out var ia) ? ia.GetBoolean() : true;

                var state = await db.UserStates.SingleOrDefaultAsync(x => x.ElderlyId == elderlyId);
                if (state is null)
                {
                    state = new StoredUserState { ElderlyId = elderlyId };
                    db.UserStates.Add(state);
                }
                state.CurrentContext = currentContext;
                state.ActiveTaskId = activeTaskId;
                state.ScreenPriority = screenPriority;
                state.IsAssistantActive = isAssistantActive;
                state.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();

                return Results.Json(new { success = true });
            }
            catch
            {
                return Results.Json(new { success = false, message = "İşlem başarısız" }, statusCode: 500);
            }
        }).RequireAuthorization(policy => policy.RequireRole("Elderly"));

        return app;
    }
}
