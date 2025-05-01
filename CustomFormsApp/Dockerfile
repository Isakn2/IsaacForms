# Base stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["CustomFormsApp.csproj", "."]
RUN dotnet restore "CustomFormsApp.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "CustomFormsApp.csproj" -c Release -o /app/build

# Install EF Core tools in the build stage
RUN dotnet tool install --global dotnet-ef

# Publish stage
FROM build AS publish
RUN dotnet publish "CustomFormsApp.csproj" -c Release -o /app/publish

# Runtime stage
FROM base AS final
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Copy and prepare entrypoint script
COPY --from=build /src/start.sh .

# Copy the dotnet-ef tool from build stage to final stage
COPY --from=build /root/.dotnet/tools /root/.dotnet/tools

# Install any required runtime dependencies
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    libgdiplus \
    && rm -rf /var/lib/apt/lists/*

# Install dos2unix and prepare entrypoint script
RUN apt-get update && apt-get install -y dos2unix && dos2unix start.sh && chmod +x start.sh

# Set PATH for dotnet-ef tool
ENV PATH="${PATH}:/root/.dotnet/tools"

# Entrypoint
ENTRYPOINT ["/app/start.sh"]