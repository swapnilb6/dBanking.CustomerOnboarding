# Base runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# Expose port that the app actually listens on
EXPOSE 9090

# Install curl for healthcheck (small, but required by compose healthcheck)
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["dBanking.CustomerOnboarding.API/dBanking.CustomerOnboarding.API.csproj", "dBanking.CustomerOnboarding.API/"]
RUN dotnet restore "dBanking.CustomerOnboarding.API/dBanking.CustomerOnboarding.API.csproj"
COPY . .
WORKDIR "/src/dBanking.CustomerOnboarding.API"
RUN dotnet build "dBanking.CustomerOnboarding.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "dBanking.CustomerOnboarding.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Run as root to avoid /keys volume permission issues in dev
USER root
ENTRYPOINT ["dotnet", "dBanking.CustomerOnboarding.API.dll"]