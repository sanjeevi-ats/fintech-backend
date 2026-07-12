# Multi-stage build for .NET Core API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY Fintech/Fintech/*.csproj ./Fintech/Fintech/
RUN dotnet restore ./Fintech/Fintech/Fintech.csproj

# Copy everything else and build
COPY . .
WORKDIR /app/Fintech/Fintech
RUN dotnet publish -c Release -o out

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy build artifacts
COPY --from=build /app/Fintech/Fintech/out .

# Copy migrations
COPY --from=build /app/Fintech/Fintech/Migrations ./Migrations

# Expose port (Railway/Render will use PORT env variable)
ENV ASPNETCORE_URLS=http://+:${PORT:-5177}
EXPOSE ${PORT:-5177}

ENTRYPOINT ["dotnet", "Fintech.dll"]
