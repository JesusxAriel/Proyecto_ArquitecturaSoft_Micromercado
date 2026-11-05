# Servicio 1: Ventas

Solución independiente .NET 10 con Domain, Application, Infrastructure MySQL y API REST con validación JWT HS256. Etapa 6 implementada, con catálogo/clientes de lectura y comprobantes JSON/HTML; la integración MySQL y con Usuarios sigue pendiente de un entorno autorizado.

## Documentación

- [Dominio](MIGRACION_DOMINIO.md).
- [Application](MIGRACION_APPLICATION.md).
- [Infrastructure](MIGRACION_INFRASTRUCTURE.md).
- [API, JWT, endpoints y configuración](MIGRACION_API.md).
- [Consultas de productos/clientes y comprobantes](COMPLEMENTOS_VENTAS.md).

## Dependencias

Domain no referencia otros proyectos. Application referencia Domain; Infrastructure referencia Application/Domain; Api referencia Application/Infrastructure y actúa como raíz de composición. Tests referencia las cuatro capas.

Cada operación HTTP tiene una sesión de persistencia scoped y una unidad de trabajo compartida por sus repositorios. No se comparten conexiones activas entre solicitudes.

## Validación

Desde Servicio1.Ventas:

~~~powershell
dotnet restore PuntoMarket.Ventas.slnx
dotnet build PuntoMarket.Ventas.slnx --no-restore
dotnet test PuntoMarket.Ventas.slnx --no-restore
~~~

Resultado de etapa 6: 269 pruebas correctas y 9 integraciones MySQL omitidas explícitamente por falta de entorno aislado autorizado. Las transacciones reales todavía no se consideran verificadas.

## Arranque

~~~powershell
dotnet run --project src/PuntoMarket.Ventas.Api/PuntoMarket.Ventas.Api.csproj --no-build --no-restore --no-launch-profile
~~~

Puerto predeterminado 5002. GET /health responde sin MySQL. Las rutas /api/ventas requieren JWT; sin configuración JWT no se acepta ningún token.

Configurar externamente ConnectionStrings__Ventas, Jwt__Key, Jwt__Issuer, Jwt__Audience y, opcionalmente, ASPNETCORE_URLS. No guardar secretos en archivos del repositorio. Consulte MIGRACION_API.md para el contrato de claims y ejemplos.

No hay Redis, Nginx, login, emisión de JWT, CRUD administrativo ni Docker Compose definitivo. Los informes de etapas anteriores conservan sus resultados históricos.
