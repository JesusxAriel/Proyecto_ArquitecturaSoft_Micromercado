FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Servicio1.Ventas/frontend/PuntoMarket.Ventas.Frontend/PuntoMarket.Ventas.Frontend.csproj Servicio1.Ventas/frontend/PuntoMarket.Ventas.Frontend/

RUN dotnet restore Servicio1.Ventas/frontend/PuntoMarket.Ventas.Frontend/PuntoMarket.Ventas.Frontend.csproj

COPY Servicio1.Ventas/frontend/ Servicio1.Ventas/frontend/
RUN dotnet publish Servicio1.Ventas/frontend/PuntoMarket.Ventas.Frontend/PuntoMarket.Ventas.Frontend.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends socat \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /home/app \
    && chown app:app /home/app

COPY --from=build /app/publish .
COPY docker/frontend/entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod 0755 /usr/local/bin/entrypoint.sh

ENV ASPNETCORE_URLS=http://+:8080 \
    HOME=/home/app
EXPOSE 8080

USER app
ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
