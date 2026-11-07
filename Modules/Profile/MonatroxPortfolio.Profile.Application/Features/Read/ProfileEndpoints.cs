using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MonatroxPortfolio.Profile.Public;

namespace MonatroxPortfolio.Profile.Application.Features.Read;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/profile", async (IProfileQueries queries, CancellationToken ct) =>
        {
            var result = await queries.GetAsync(ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(
                statusCode: StatusCodes.Status404NotFound, title: result.Error!.Code, detail: result.Error.Message);
        });
        return endpoints;
    }
}
