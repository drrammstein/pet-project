# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/WorkerService/WorkerService.csproj", "src/WorkerService/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
RUN dotnet restore "src/WorkerService/WorkerService.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/src/WorkerService"
RUN dotnet build "WorkerService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "WorkerService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:9.0-bookworm-slim AS final
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --home /app --gecos "" appuser
USER appuser

COPY --from=publish --chown=appuser:appuser /app/publish .
ENTRYPOINT ["dotnet", "WorkerService.dll"]
