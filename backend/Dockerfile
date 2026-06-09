# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore (leveraging layer cache on the project files)
COPY Directory.Build.props ./
COPY Pms.sln ./
COPY src/Pms.Domain/Pms.Domain.csproj src/Pms.Domain/
COPY src/Pms.Application/Pms.Application.csproj src/Pms.Application/
COPY src/Pms.Infrastructure/Pms.Infrastructure.csproj src/Pms.Infrastructure/
COPY src/Pms.Api/Pms.Api.csproj src/Pms.Api/
RUN dotnet restore src/Pms.Api/Pms.Api.csproj

# Build & publish
COPY . .
RUN dotnet publish src/Pms.Api/Pms.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Pms.Api.dll"]
