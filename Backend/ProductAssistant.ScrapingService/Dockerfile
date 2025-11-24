# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY Backend/ProductAssistant.ScrapingService/*.csproj ./ProductAssistant.ScrapingService/
COPY Backend/ProductAssistant.Core/*.csproj ./ProductAssistant.Core/

# Restore dependencies
WORKDIR /src/ProductAssistant.ScrapingService
RUN dotnet restore

# Copy all source files
WORKDIR /src
COPY Backend/ProductAssistant.ScrapingService/ ./ProductAssistant.ScrapingService/
COPY Backend/ProductAssistant.Core/ ./ProductAssistant.Core/

# Publish stage
FROM build AS publish
WORKDIR /src/ProductAssistant.ScrapingService
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "ProductAssistant.ScrapingService.dll"]

