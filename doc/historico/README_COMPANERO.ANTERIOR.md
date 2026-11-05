> HISTORICO: entrega aislada de Ventas y perfiles Demo retirados. No usar como guia actual.

# PuntoMarket Servicio1.Ventas — guía de transferencia

Esta carpeta contiene el microservicio Ventas independiente. No requiere copiar el
monolito original para compilar o ejecutar sus proyectos.

## Requisitos

- .NET 10 SDK.
- Docker Desktop con Docker Compose.
- PowerShell 5.1 o superior.
- VS Code o Visual Studio compatible con .NET 10.

Comprobar herramientas:

```powershell
dotnet --version
docker --version
docker compose version
```

## Estructura hexagonal

- `src/PuntoMarket.Ventas.Domain`: entidades y reglas de dominio.
- `src/PuntoMarket.Ventas.Application`: casos de uso, DTOs y puertos.
- `src/PuntoMarket.Ventas.Infrastructure`: adaptadores MySQL y comprobantes.
- `src/PuntoMarket.Ventas.Api`: adaptador HTTP, JWT, políticas y composición.
- `frontend/PuntoMarket.Ventas.Frontend`: frontend Razor independiente; conserva el
  JWT únicamente en el servidor.
- `tests/PuntoMarket.Ventas.Tests`: pruebas unitarias, API, Razor e integración.
- `database/demo-env`: MySQL local de demostración y SQL preparado.
- `tests/integration-env`: MySQL aislado para las pruebas transaccionales.
- `tools/PuntoMarket.Ventas.DemoLocal`: lanzador local de API con verificaciones
  de lectura; no emite un login productivo.

## Restaurar, compilar y probar

Desde esta carpeta:

```powershell
dotnet restore .\PuntoMarket.Ventas.slnx
dotnet build .\PuntoMarket.Ventas.slnx --no-restore
dotnet test .\PuntoMarket.Ventas.slnx --no-restore
```

Las pruebas MySQL se omiten si no está preparado el entorno aislado. No deben
ejecutarse contra la base Demo.

## MySQL Demo nuevo

La base Demo es independiente. No copies el volumen Docker ni una base local de
otro equipo. Desde `database/demo-env`:

```powershell
Copy-Item .\.env.example .\.env
notepad .\.env
docker compose up -d
docker compose ps
```

Configura en `.env` un usuario exclusivo y rutas externas a archivos de contraseñas.
Los archivos de contraseña deben estar fuera del proyecto y no deben entrar al ZIP.
La base se crea vacía en el primer arranque. La importación del SQL inicial es una
operación separada y requiere autorización explícita:

```powershell
.\Validar-Preparacion.ps1 -OmitirCompose
# Solo después de revisar y autorizar la importación:
.\Importar-EsquemaDemo.ps1 -Importar
```

`Importar-EsquemaDemo.ps1` no borra bases, no reintenta una importación y bloquea
una segunda ejecución. `Verificar-ImportacionDemo.ps1` y
`Verificar-FrontendLecturas.ps1` son consultas de verificación; no registran ventas
ni anulan ventas.

## Configuración de la API

No se incluyen secretos ni `appsettings` privados. Configura variables de entorno
en la sesión de PowerShell:

```powershell
$env:ConnectionStrings__Ventas = "Server=127.0.0.1;Port=3308;Database=puntomarket_ventas_demo_local;User ID=ventas_demo;Password=CAMBIAR;SslMode=Required"
$env:Jwt__Key = "CAMBIAR_POR_UNA_CLAVE_LOCAL_DE_AL_MENOS_32_BYTES"
$env:Jwt__Issuer = "usuarios-local"
$env:Jwt__Audience = "ventas-local"
$env:ASPNETCORE_URLS = "http://localhost:5002"
```

En un entorno real, la clave JWT debe ser emitida y administrada por Usuarios.
Nunca guardes estos valores en archivos versionados.

Iniciar API:

```powershell
dotnet run --project .\src\PuntoMarket.Ventas.Api\PuntoMarket.Ventas.Api.csproj --no-launch-profile
```

Comprobar salud:

```powershell
Invoke-RestMethod http://localhost:5002/health
```

Debe responder `status = ok`. Las rutas de negocio requieren JWT válido.

## Frontend Razor

Para la demostración local, el lanzador prepara la API y el JWT en memoria:

```powershell
dotnet run --project .\tools\PuntoMarket.Ventas.DemoLocal\PuntoMarket.Ventas.DemoLocal.csproj --no-launch-profile
```

En otra terminal, con `ASPNETCORE_ENVIRONMENT=Development`:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project .\frontend\PuntoMarket.Ventas.Frontend\PuntoMarket.Ventas.Frontend.csproj --no-launch-profile
```

Abrir `http://localhost:5100/Demo` y entrar como Cajero demo. Esta sesión es solo
para desarrollo local: no es el login definitivo de Usuarios y no crea tokens
visibles para el navegador. El frontend usa la cookie local y adjunta el JWT al
API únicamente desde `VentasApiClient`.

El flujo permite buscar clientes/productos, crear un cliente, registrar ventas,
consultar historial y detalle, y abrir comprobantes. El cliente nuevo se selecciona
automáticamente en el formulario de venta.

## Anulación de ventas

La funcionalidad ya está implementada y no requiere cambios para esta entrega:

1. `POST /api/ventas/{id}/anular`.
2. La política `AnularVentas` exige autenticación, contraseña actualizada y rol
   `Administrador`; Cajero recibe 403.
3. El frontend pide confirmación explícita y un motivo obligatorio.
4. `FachadaAnulacion` restaura las cantidades en los lotes consumidos originalmente.
5. La unidad de trabajo confirma o revierte la operación completa.
6. La venta registra motivo, fecha y usuario anulador.
7. El estado solo permite anular ventas `COMPLETADA`, evitando una segunda anulación.
8. Las pruebas verifican autorización, restauración de lotes, auditoría, rollback y
   doble anulación.

El flujo de anulación se puede probar con un Administrador real en una base aislada.
No se debe anular una venta de MySQL Demo sin autorización explícita. La
comprobación visual con una sesión Administrador válida permanece pendiente porque
la sesión Demo incluida solo inicia como Cajero.

## Identidad de creación de clientes

`POST /api/clientes` usa la política `OperarVentas`, por lo que admite Administrador
y Cajero. `ClienteCreacionService` obtiene `IUsuarioActual.Id`; el adaptador JWT
resuelve ese valor desde el claim entero positivo `sub` y lo escribe como
`CLIENTE.idUsuarioCreacion`. No hay un ID fijo ni escritura hacia otra base.

La integración definitiva con Usuarios queda pendiente: sus tokens deben conservar
un `sub` compatible con la clave foránea de `USUARIO`, además de `unique_name`,
`role` y `debeCambiarPassword`.

## Pruebas de integración seguras

El entorno aislado usa otra base y otro puerto:

```powershell
Set-Location .\tests\integration-env
Copy-Item .\.env.example .\.env
notepad .\.env
docker compose --env-file .\.env -f .\compose.yaml up -d
```

Ejecuta las pruebas normales desde la raíz. Los tests destructivos solo deben
habilitarse después de confirmar que Docker corresponde a
`puntomarket-ventas-integration`, usa el puerto 3307 y la base
`puntomarket_ventas_test_local`. Nunca apuntes `ConnectionStrings__Ventas` al
puerto 3308 para ejecutar pruebas transaccionales.

## Transferencia

El ZIP generado junto a esta carpeta contiene únicamente el código fuente,
documentación, SQL preparado, plantillas y scripts. Se excluyen `bin`, `obj`,
`.vs`, `node_modules`, `TestResults`, `.git`, sesiones Copilot, volúmenes Docker,
`.env` privados, claves, certificados y credenciales.
