using System.Globalization;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MonatroxPortfolio.Contact.Application.Abstractions;
using MonatroxPortfolio.Contact.Public;
using MonatroxPortfolio.Shared.Contracts;

namespace MonatroxPortfolio.Contact.Application;

public static class ContactEndpoints
{
    public static IServiceCollection AddContactRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
                    context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retry.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                return ValueTask.CompletedTask;
            };
            // shortcut: counters are per process, add shared edge protection before multiple public instances.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                context.Request.Method == "POST" && context.Request.Path == "/api/contact"
                    ? RateLimitPartition.GetFixedWindowLimiter("contact-global", _ => new() { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })
                    : RateLimitPartition.GetNoLimiter("other"));
            options.AddPolicy("contact", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new() { PermitLimit = 3, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 }));
        });
        return services;
    }

    public static IEndpointRouteBuilder MapContactEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/contact", (HttpContext context, IContactSender sender) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(sender.Info);
        });
        app.MapPost("/api/contact", SubmitAsync).RequireRateLimiting("contact");
        return app;
    }

    private static async Task<IResult> SubmitAsync(HttpContext context, ContactService service)
    {
        context.Response.Headers.CacheControl = "no-store";
        // JSON plus a custom header prevents cross-site browser form posts; CORS remains disabled.
        if (context.Request.Headers["X-Portfolio-Contact"] != "1" || !context.Request.HasJsonContentType())
            return Results.Problem(statusCode: 400, title: "Unsupported contact request.");
        const int maximum = 32768;
        if (context.Request.ContentLength > maximum) return Results.StatusCode(413);
        var buffer = new byte[maximum + 1];
        var length = await context.Request.Body.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, cancellationToken: context.RequestAborted);
        if (length > maximum) return Results.StatusCode(413);
        ContactRequestDto? request;
        try { request = JsonSerializer.Deserialize<ContactRequestDto>(buffer.AsSpan(0, length), ContentJson.Options); }
        catch (JsonException) { return Results.Problem(statusCode: 400, title: "Invalid contact request."); }
        if (request is null) return Results.Problem(statusCode: 400, title: "Invalid contact request.");
        var result = await service.SubmitAsync(request, context.RequestAborted);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(
            statusCode: result.Error!.Code == "contact.invalid" ? 400 : 503, title: result.Error.Message);
    }
}
