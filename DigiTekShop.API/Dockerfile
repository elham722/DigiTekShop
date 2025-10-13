# Use the official .NET 9 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the official .NET 9 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["DigiTekShop.API/DigiTekShop.API.csproj", "DigiTekShop.API/"]
COPY ["DigiTekShop.Application/DigiTekShop.Application.csproj", "DigiTekShop.Application/"]
COPY ["DigiTekShop.Contracts/DigiTekShop.Contracts.csproj", "DigiTekShop.Contracts/"]
COPY ["DigiTekShop.Domain/DigiTekShop.Domain.csproj", "DigiTekShop.Domain/"]
COPY ["DigiTekShop.Infrastructure/DigiTekShop.Infrastructure.csproj", "DigiTekShop.Infrastructure/"]
COPY ["DigiTekShop.Persistence/DigiTekShop.Persistence.csproj", "DigiTekShop.Persistence/"]
COPY ["DigiTekShop.SharedKernel/DigiTekShop.SharedKernel.csproj", "DigiTekShop.SharedKernel/"]
COPY ["DigiTekShop.Identity/DigiTekShop.Identity.csproj", "DigiTekShop.Identity/"]
COPY ["DigiTekShop.ExternalServices/DigiTekShop.ExternalServices.csproj", "DigiTekShop.ExternalServices/"]

# Restore dependencies
RUN dotnet restore "DigiTekShop.API/DigiTekShop.API.csproj"

# Copy everything else
COPY . .

# Build the application
WORKDIR "/src/DigiTekShop.API"
RUN dotnet build "DigiTekShop.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "DigiTekShop.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "DigiTekShop.API.dll"]

