# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Antigravity.Api.csproj", "./"]
RUN dotnet restore "Antigravity.Api.csproj"
COPY . .
RUN dotnet build "Antigravity.Api.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "Antigravity.Api.csproj" -c Release -o /app/publish

# Final Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Antigravity.Api.dll"]
