# Published outside Docker with the repository's pinned SDK; runtime stays identical in CI and production.
FROM mcr.microsoft.com/dotnet/aspnet:10.0.14-noble@sha256:1a48394b6f29d3a32c79d9edecd4abda0ecbe8776d567dbfc4590ed6979c740c AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_ENVIRONMENT=Production
USER 1654:1654

FROM runtime AS web
COPY web/ ./
ENTRYPOINT ["dotnet", "MonatroxPortfolio.Blazor.dll"]

FROM runtime AS api
COPY api/ ./
ENTRYPOINT ["dotnet", "MonatroxPortfolio.Api.dll"]

FROM runtime AS tools
COPY tools/ ./
ENTRYPOINT ["dotnet", "MonatroxPortfolio.Migrations.dll"]
