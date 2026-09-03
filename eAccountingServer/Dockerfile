# The AppHost project needs the Aspire workload and is not part of a deployment, so the
# WebApi is published directly rather than through the solution file.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/eAccountingServer.Domain/ eAccountingServer.Domain/
COPY src/eAccountingServer.Application/ eAccountingServer.Application/
COPY src/eAccountingServer.Infrastructure/ eAccountingServer.Infrastructure/
COPY src/eAccountingServer.Template.ServiceDefaults/ eAccountingServer.Template.ServiceDefaults/
COPY src/eAccountingServer.WebApi/ eAccountingServer.WebApi/

RUN dotnet publish eAccountingServer.WebApi/eAccountingServer.WebApi.csproj \
    -c Release -o /app --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080 \
    UseHttpsRedirection=false

EXPOSE 8080
ENTRYPOINT ["dotnet", "eAccountingServer.WebApi.dll"]
