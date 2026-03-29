FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY services/shared/Airgap.Persistence/ ./services/shared/Airgap.Persistence/
COPY services/data/Data.Api/ ./services/data/Data.Api/
RUN dotnet publish services/data/Data.Api/Data.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Data.Api.dll"]
