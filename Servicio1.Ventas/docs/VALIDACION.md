# Validación de la estructura inicial

Fecha: 2026-11-04. SDK: 10.0.401. Todos los proyectos usan net10.0.

Comandos ejecutados desde Servicio1.Ventas:

| Comando | Resultado |
| --- | --- |
| dotnet restore PuntoMarket.Ventas.slnx | Código 0; cinco proyectos restaurados |
| dotnet build PuntoMarket.Ventas.slnx --no-restore | Código 0; 0 errores, 1 advertencia |
| dotnet test tests/PuntoMarket.Ventas.Tests/PuntoMarket.Ventas.Tests.csproj --no-restore | Código 0; ninguna prueba disponible |

La advertencia NU1900 corresponde a un fallo SSL al consultar los datos de vulnerabilidades de NuGet. La restauración de paquetes y la compilación finalizaron; la consulta de vulnerabilidades no pudo completarse.

No hay pruebas reales todavía. El resultado del comando de pruebas solo confirma que puede compilarse e invocarse el proyecto de pruebas; no constituye validación funcional.

Arranque adicional verificado: dotnet run --project src/PuntoMarket.Ventas.Api/PuntoMarket.Ventas.Api.csproj --no-build --no-restore --urls http://127.0.0.1:5187. Kestrel informó Application started y una petición HTTP a / respondió 404. El proceso de comprobación fue detenido.

Se compararon las huellas SHA-256 de los 652 archivos preexistentes antes y después de la creación y validación: ninguno modificado ni eliminado y ningún archivo nuevo fuera de Servicio1.Ventas.

No se migró lógica de negocio, no se accedió a bases de datos y no se crearon referencias al monolito. No se realizaron operaciones de escritura en Git.
