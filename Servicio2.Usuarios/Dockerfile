FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Servicio2.Usuarios/src/PuntoMarket.Usuarios.Api/PuntoMarket.Usuarios.Api.csproj Servicio2.Usuarios/src/PuntoMarket.Usuarios.Api/
COPY Servicio2.Usuarios/src/PuntoMarket.Usuarios.Application/PuntoMarket.Usuarios.Application.csproj Servicio2.Usuarios/src/PuntoMarket.Usuarios.Application/
COPY Servicio2.Usuarios/src/PuntoMarket.Usuarios.Domain/PuntoMarket.Usuarios.Domain.csproj Servicio2.Usuarios/src/PuntoMarket.Usuarios.Domain/
COPY Servicio2.Usuarios/src/PuntoMarket.Usuarios.Infrastructure/PuntoMarket.Usuarios.Infrastructure.csproj Servicio2.Usuarios/src/PuntoMarket.Usuarios.Infrastructure/

RUN dotnet restore Servicio2.Usuarios/src/PuntoMarket.Usuarios.Api/PuntoMarket.Usuarios.Api.csproj

COPY Servicio2.Usuarios/src/ Servicio2.Usuarios/src/
RUN dotnet publish Servicio2.Usuarios/src/PuntoMarket.Usuarios.Api/PuntoMarket.Usuarios.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PuntoMarket.Usuarios.Api.dll"]
