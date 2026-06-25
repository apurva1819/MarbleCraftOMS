FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy all .csproj files first so restore is a cached layer
COPY src/MarbleCraftOMS.Api/MarbleCraftOMS.Api.csproj                              src/MarbleCraftOMS.Api/
COPY src/MarbleCraftOMS.Application/MarbleCraftOMS.Application.csproj              src/MarbleCraftOMS.Application/
COPY src/MarbleCraftOMS.BackgroundServices/MarbleCraftOMS.BackgroundServices.csproj src/MarbleCraftOMS.BackgroundServices/
COPY src/MarbleCraftOMS.Core/MarbleCraftOMS.Core.csproj                            src/MarbleCraftOMS.Core/
COPY src/MarbleCraftOMS.Infrastructure/MarbleCraftOMS.Infrastructure.csproj        src/MarbleCraftOMS.Infrastructure/

RUN dotnet restore src/MarbleCraftOMS.Api/MarbleCraftOMS.Api.csproj

# Copy all source and publish
COPY src/ src/
RUN dotnet publish src/MarbleCraftOMS.Api/MarbleCraftOMS.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MarbleCraftOMS.Api.dll"]
