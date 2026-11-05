# API original conectada a MySQL Demo

Herramienta local .NET 10 que hospeda `ApiBootstrap.Crear` del proyecto API existente. No es otra implementación de los endpoints: conserva todos los servicios, repositorios MySQL, autenticación y políticas originales. No sustituye puertos por implementaciones de prueba.

Lee el usuario y la ruta de contraseña de `database/demo-env/.env`; lee el secreto externo solo en memoria. Construye `ConnectionStrings:Ventas` con MySqlConnectionStringBuilder para 127.0.0.1:3308 y puntomarket_ventas_demo_local, usando TLS Required. No guarda ni imprime la cadena.

Para probar autenticación sin integrar Usuarios, crea si falta una clave HS256 aleatoria en el archivo privado externo `puntomarket-demo-secrets/jwt-demo-key.txt`. No sobrescribe una clave existente. Razor Development lee el mismo archivo para sus sesiones; emisor/audiencia son exclusivos de Demo. Los JWT de cinco minutos siguen siendo exclusivamente de memoria: no publica una ruta para emitirlos ni imprime tokens. No utiliza hashes de USUARIO para firmar JWT. Esto no implementa login de producción. Ver [integración Razor](../../docs/FRONTEND_CONSULTAS_REALES.md).

Desde `Servicio1.Ventas/`, con el puerto 5002 libre y Demo iniciado:

```powershell
dotnet run --project .\tools\PuntoMarket.Ventas.DemoLocal\PuntoMarket.Ventas.DemoLocal.csproj --no-launch-profile
```

Ejecuta las comprobaciones GET de lectura y deja la API original activa en http://localhost:5002. Detener con Ctrl+C en la consola que ejecuta el lanzador. Rechaza sustituir procesos existentes si el puerto está ocupado. No ejecutar una segunda instancia sobre la actual.

Para comprobar y detener automáticamente al terminar:

```powershell
dotnet run --project .\tools\PuntoMarket.Ventas.DemoLocal\PuntoMarket.Ventas.DemoLocal.csproj --no-launch-profile -- --solo-verificar
```

Requiere los datos originales de demostración, incluido el producto 1 y cliente 2. Las comprobaciones solo envían GET; no registran ni anulan ventas. Antes/después comparan checksums de las 11 tablas por lecturas. Las consultas SQL directas cotejan lo devuelto por HTTP y no reemplazan los repositorios de la API. El proceso arranca únicamente en loopback; no utilizarlo como host de producción.

El lanzador configura mediante un proveedor de memoria del proceso las mismas claves que la API ya consume. No modifica appsettings, variables del usuario/sistema, launchSettings ni la solución. El registro de peticiones se suprime exclusivamente en este host local para emitir resultados saneados; el código de logging de la API permanece intacto.

Los tokens de comprobación no quedan disponibles para el navegador. Abrir endpoints de negocio en el navegador sin Bearer seguirá devolviendo 401. Razor ya permite consultas por un cliente tipado y mantiene sus JWT en sesiones del servidor; el navegador solo envía la cookie al frontend.

No forma parte de la solución ni de las pruebas normales: ejecutarlo siempre es una comprobación manual explícita contra Demo. No hay nuevos PackageReference; reutiliza la referencia al proyecto API y sus dependencias existentes. El proyecto no debe usarse para ejecutar tests de integración transaccionales.
