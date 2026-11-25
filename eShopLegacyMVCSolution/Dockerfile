FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY eShopLegacyMVC.sln ./
COPY eShopLegacy.Utilities/eShopLegacy.Utilities.csproj eShopLegacy.Utilities/
COPY eShopPorted/eShopPorted.csproj eShopPorted/
RUN dotnet restore eShopPorted/eShopPorted.csproj

COPY eShopLegacy.Utilities/ eShopLegacy.Utilities/
COPY eShopPorted/ eShopPorted/

RUN dotnet publish eShopPorted/eShopPorted.csproj -c Release -o /app/publish /p:UseAppHost=false && \
    dotnet tool install --tool-path /tools dotnet-ef --version 10.0.15 && \
    /tools/dotnet-ef migrations bundle \
        --project eShopPorted/eShopPorted.csproj \
        --startup-project eShopPorted/eShopPorted.csproj \
        --configuration Release \
        --no-build \
        --output /app/publish/efbundle

FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish ./

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "eShopPorted.dll"]
