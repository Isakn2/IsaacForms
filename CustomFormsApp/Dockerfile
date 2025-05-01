FROM mcr.microsoft.com/dotnet/aspnet:9.0-preview AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0-preview AS build
WORKDIR /src
COPY ["CustomFormsApp.csproj", "."]
RUN dotnet restore "CustomFormsApp.csproj"
COPY . .
RUN dotnet build "CustomFormsApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CustomFormsApp.csproj" -c Release -o /app/publish
# Install EF Core tools in the build stage
RUN dotnet tool install --global dotnet-ef

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /src/start.sh .
# Copy the dotnet-ef tool from build stage to final stage
COPY --from=build /root/.dotnet/tools /root/.dotnet/tools
RUN apt-get update && apt-get install -y dos2unix && dos2unix start.sh && chmod +x start.sh
ENV PATH="${PATH}:/root/.dotnet/tools"
ENTRYPOINT ["/app/start.sh"]