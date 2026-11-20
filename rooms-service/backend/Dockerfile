FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore RoomsService.Api/RoomsService.Api.csproj
RUN dotnet publish RoomsService.Api/RoomsService.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ARG INSTALL_NEW_RELIC=true
RUN apt-get update && apt-get install -y --no-install-recommends curl ca-certificates \
    && if [ "$INSTALL_NEW_RELIC" = "true" ]; then \
        apt-get install -y --no-install-recommends gnupg \
        && curl -fsSL https://download.newrelic.com/548C16BF.gpg -o /tmp/newrelic.gpg \
        && gpg --dearmor -o /usr/share/keyrings/newrelic.gpg /tmp/newrelic.gpg \
        && echo "deb [signed-by=/usr/share/keyrings/newrelic.gpg] https://apt.newrelic.com/debian/ newrelic non-free" > /etc/apt/sources.list.d/newrelic.list \
        && apt-get update && apt-get install -y --no-install-recommends newrelic-dotnet-agent \
        && rm /tmp/newrelic.gpg; \
    fi \
    && rm -rf /var/lib/apt/lists/*

ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A}
ENV CORECLR_NEWRELIC_HOME=/usr/local/newrelic-dotnet-agent
ENV CORECLR_PROFILER_PATH=/usr/local/newrelic-dotnet-agent/libNewRelicProfiler.so
ENV NEW_RELIC_APP_NAME=rooms-service
ENV NEW_RELIC_DISTRIBUTED_TRACING_ENABLED=true
ENV NEW_RELIC_APPLICATION_LOGGING_ENABLED=true
ENV NEW_RELIC_APPLICATION_LOGGING_FORWARDING_ENABLED=true
ENV NEW_RELIC_APPLICATION_LOGGING_METRICS_ENABLED=true

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RoomsService.Api.dll"]
