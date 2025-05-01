FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CustomFormsApp.csproj", "."]
RUN dotnet restore "CustomFormsApp.csproj"
COPY . .
RUN dotnet build "CustomFormsApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CustomFormsApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /src/start.sh .
RUN apt-get update && apt-get install -y dos2unix && dos2unix start.sh && chmod +x start.sh
# Install EF Core tools globally
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"
ENTRYPOINT ["/app/start.sh"]