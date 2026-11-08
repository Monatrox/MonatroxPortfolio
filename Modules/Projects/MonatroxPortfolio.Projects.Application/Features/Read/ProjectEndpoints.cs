using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MonatroxPortfolio.Projects.Public;

namespace MonatroxPortfolio.Projects.Application.Features.Read;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/projects", (IProjectQueries queries, CancellationToken ct) => queries.ListAsync(ct));
        endpoints.MapGet("/api/projects/{id}", async (string id, IProjectQueries queries, CancellationToken ct) =>
        {
            var result = await queries.GetAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(
                statusCode: StatusCodes.Status404NotFound, title: result.Error!.Code, detail: result.Error.Message);
        });
        endpoints.MapGet("/api/projects/{id}/page", async (string id, IProjectQueries queries, CancellationToken ct) =>
        {
            var result = await queries.GetPageAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(
                statusCode: StatusCodes.Status404NotFound, title: result.Error!.Code, detail: result.Error.Message);
        });
        return endpoints;
    }
}
