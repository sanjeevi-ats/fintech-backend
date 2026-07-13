# Multi-stage build for .NET Core API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["Fintech/Fintech/Fintech.csproj", "Fintech/Fintech/"]
RUN dotnet restore "Fintech/Fintech/Fintech.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/Fintech/Fintech"
RUN dotnet build "Fintech.csproj" -c Release -o /app/build
RUN dotnet publish "Fintech.csproj" -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Expose port
EXPOSE 5177

# Use shell form for environment variable substitution
CMD dotnet Fintech.dll --urls "http://0.0.0.0:${PORT:-5177}"
