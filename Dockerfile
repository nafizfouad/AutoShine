# syntax=docker/dockerfile:1

# Single image: the ASP.NET Core API also serves the built React app,
# so the whole application runs as one container on one port.

# ── 1. Build the React frontend ───────────────────────────────────────────────
FROM node:22-slim AS client
WORKDIR /client
COPY src/client/package.json src/client/package-lock.json ./
RUN npm ci
COPY src/client/ ./
RUN npm run build

# ── 2. Build and publish the API ──────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first so the dependency layer is cached between code changes
COPY src/AutoShine/AutoShine.csproj                       src/AutoShine/
COPY src/AutoShine.Data/AutoShine.Data.csproj             src/AutoShine.Data/
COPY src/AutoShine.Models/AutoShine.Models.csproj         src/AutoShine.Models/
COPY src/AutoShine.Repository/AutoShine.Repository.csproj src/AutoShine.Repository/
COPY src/AutoShine.Service/AutoShine.Service.csproj       src/AutoShine.Service/
RUN dotnet restore src/AutoShine/AutoShine.csproj

COPY src/AutoShine/            src/AutoShine/
COPY src/AutoShine.Data/       src/AutoShine.Data/
COPY src/AutoShine.Models/     src/AutoShine.Models/
COPY src/AutoShine.Repository/ src/AutoShine.Repository/
COPY src/AutoShine.Service/    src/AutoShine.Service/
RUN dotnet publish src/AutoShine/AutoShine.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ── 3. Runtime image ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080

COPY --from=build  /app/publish ./
COPY --from=client /client/dist ./wwwroot

# Non-root user provided by the .NET base image
USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "AutoShine.dll"]
