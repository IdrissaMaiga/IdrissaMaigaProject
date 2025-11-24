# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY Backend/ProductAssistant.AIService/*.csproj ./ProductAssistant.AIService/
COPY Backend/ProductAssistant.Core/*.csproj ./ProductAssistant.Core/

# Restore dependencies for these projects only
WORKDIR /src/ProductAssistant.AIService
RUN dotnet restore

# Copy all source files
COPY Backend/ProductAssistant.AIService/ ./ProductAssistant.AIService/
COPY Backend/ProductAssistant.Core/ ../ProductAssistant.Core/

# Build the application
RUN dotnet build -c Release -o /app/build /p:UseAppHost=false

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "ProductAssistant.AIService.dll"]

