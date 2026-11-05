FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Servicio1.Ventas/src/PuntoMarket.Ventas.Api/PuntoMarket.Ventas.Api.csproj Servicio1.Ventas/src/PuntoMarket.Ventas.Api/
COPY Servicio1.Ventas/src/PuntoMarket.Ventas.Application/PuntoMarket.Ventas.Application.csproj Servicio1.Ventas/src/PuntoMarket.Ventas.Application/
COPY Servicio1.Ventas/src/PuntoMarket.Ventas.Domain/PuntoMarket.Ventas.Domain.csproj Servicio1.Ventas/src/PuntoMarket.Ventas.Domain/
COPY Servicio1.Ventas/src/PuntoMarket.Ventas.Infrastructure/PuntoMarket.Ventas.Infrastructure.csproj Servicio1.Ventas/src/PuntoMarket.Ventas.Infrastructure/

RUN dotnet restore Servicio1.Ventas/src/PuntoMarket.Ventas.Api/PuntoMarket.Ventas.Api.csproj

COPY Servicio1.Ventas/src/ Servicio1.Ventas/src/
RUN dotnet publish Servicio1.Ventas/src/PuntoMarket.Ventas.Api/PuntoMarket.Ventas.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER app
ENTRYPOINT ["dotnet", "PuntoMarket.Ventas.Api.dll"]
