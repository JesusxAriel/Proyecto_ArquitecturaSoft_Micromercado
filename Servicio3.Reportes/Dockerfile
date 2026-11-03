FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Servicio3.Reportes/src/PuntoMarket.Reportes.Domain/PuntoMarket.Reportes.Domain.csproj", "Servicio3.Reportes/src/PuntoMarket.Reportes.Domain/"]
COPY ["Servicio3.Reportes/src/PuntoMarket.Reportes.Application/PuntoMarket.Reportes.Application.csproj", "Servicio3.Reportes/src/PuntoMarket.Reportes.Application/"]
COPY ["Servicio3.Reportes/src/PuntoMarket.Reportes.Infrastructure/PuntoMarket.Reportes.Infrastructure.csproj", "Servicio3.Reportes/src/PuntoMarket.Reportes.Infrastructure/"]
COPY ["Servicio3.Reportes/src/PuntoMarket.Reportes.Api/PuntoMarket.Reportes.Api.csproj", "Servicio3.Reportes/src/PuntoMarket.Reportes.Api/"]

RUN dotnet restore "Servicio3.Reportes/src/PuntoMarket.Reportes.Api/PuntoMarket.Reportes.Api.csproj"

COPY Servicio3.Reportes/src/ Servicio3.Reportes/src/

WORKDIR /src/Servicio3.Reportes/src/PuntoMarket.Reportes.Api
RUN dotnet publish PuntoMarket.Reportes.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PuntoMarket.Reportes.Api.dll"]
