# Multi-stage build for .NET 8 API (Render deployment)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["Fintech/Fintech/Fintech.csproj", "Fintech/Fintech/"]
RUN dotnet restore "Fintech/Fintech/Fintech.csproj"

# Copy all source files
COPY . .

# Build and publish release
WORKDIR "/src/Fintech/Fintech"
RUN dotnet publish "Fintech.csproj" -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published output
COPY --from=build /app/publish .

# Expose port 8080 (Render default)
EXPOSE 8080

# Production environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Fintech.dll"]
