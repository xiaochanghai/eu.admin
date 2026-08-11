FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY . .
RUN dotnet restore EU.Core.Agent.Api/EU.Core.Agent.Api.csproj --locked-mode
RUN dotnet publish EU.Core.Agent.Api/EU.Core.Agent.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /var/lib/eu-core-agent/skills \
    && chown -R app:app /var/lib/eu-core-agent /app

COPY --from=build --chown=app:app /app/publish/ ./

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    AgentStorage__Provider=Sqlite \
    AgentStorage__DatabasePath=/var/lib/eu-core-agent/eu-core-agent.db \
    AgentStorage__SkillRootPath=/var/lib/eu-core-agent/skills

EXPOSE 8080
VOLUME ["/var/lib/eu-core-agent"]
USER app
STOPSIGNAL SIGTERM
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl --fail --silent --show-error --max-time 4 http://127.0.0.1:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "EU.Core.Agent.Api.dll"]
