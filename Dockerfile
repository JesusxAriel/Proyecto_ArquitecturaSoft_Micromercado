
# Etapa 1: compilar y publicar
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Proyecto_Arquitectura_Micromercado/Proyecto_Arquitectura_Micromercado.csproj", "Proyecto_Arquitectura_Micromercado/"]

RUN dotnet restore "Proyecto_Arquitectura_Micromercado/Proyecto_Arquitectura_Micromercado.csproj"

COPY Proyecto_Arquitectura_Micromercado/ Proyecto_Arquitectura_Micromercado/

WORKDIR /src/Proyecto_Arquitectura_Micromercado
RUN dotnet publish Proyecto_Arquitectura_Micromercado.csproj -c Release -o /app/publish

# Etapa 2: ejecutar la aplicación
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Proyecto_Arquitectura_Micromercado.dll"]
