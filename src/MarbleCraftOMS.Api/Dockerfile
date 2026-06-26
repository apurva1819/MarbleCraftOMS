# ── Stage 1: Build Angular SPA ───────────────────────────────────────────────
FROM node:22-alpine AS angular-build
WORKDIR /app
COPY marble-craft-oms/package.json marble-craft-oms/package-lock.json ./
RUN npm ci
COPY marble-craft-oms/ ./
RUN npx ng build --configuration production

# ── Stage 2: Build .NET API ───────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/MarbleCraftOMS.Api/MarbleCraftOMS.Api.csproj                              src/MarbleCraftOMS.Api/
COPY src/MarbleCraftOMS.Application/MarbleCraftOMS.Application.csproj              src/MarbleCraftOMS.Application/
COPY src/MarbleCraftOMS.BackgroundServices/MarbleCraftOMS.BackgroundServices.csproj src/MarbleCraftOMS.BackgroundServices/
COPY src/MarbleCraftOMS.Core/MarbleCraftOMS.Core.csproj                            src/MarbleCraftOMS.Core/
COPY src/MarbleCraftOMS.Infrastructure/MarbleCraftOMS.Infrastructure.csproj        src/MarbleCraftOMS.Infrastructure/

RUN dotnet restore src/MarbleCraftOMS.Api/MarbleCraftOMS.Api.csproj

COPY src/ src/

# Copy Angular build output into wwwroot
COPY --from=angular-build /app/dist/marble-craft-oms/browser/ src/MarbleCraftOMS.Api/wwwroot/

RUN dotnet publish src/MarbleCraftOMS.Api/MarbleCraftOMS.Api.csproj \
    -c Release -o /app/publish --no-restore

# ── Stage 3: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MarbleCraftOMS.Api.dll"]
