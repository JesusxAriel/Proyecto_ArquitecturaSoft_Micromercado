> HISTORICO: describe preparacion previa con hashes originales. No usar para instalaciones nuevas.

# Configuración privada MySQL Demo

Estado: **lista para el próximo punto de control de arranque**. No se han iniciado contenedores ni ejecutado consultas o importaciones SQL.

## Variables exactas

El archivo local `database/demo-env/.env` contiene únicamente:

| Variable | Propósito |
| --- | --- |
| VENTAS_DEMO_USER | Usuario dedicado ventas_demo. |
| VENTAS_DEMO_ROOT_PASSWORD_FILE | Ruta externa a mysql-root.txt. |
| VENTAS_DEMO_PASSWORD_FILE | Ruta externa a mysql-ventas.txt. |
| VENTAS_DEMO_ADMIN_HASH_FILE | Ruta externa a admin-password-hash.txt. |
| VENTAS_DEMO_CAJERO_HASH_FILE | Ruta externa a cajero-password-hash.txt. |

Las tres primeras son consumidas por Compose. Las dos últimas quedan preparadas para el importador posterior y no se inyectan automáticamente en MySQL.

Los cuatro archivos están en `$env:USERPROFILE/puntomarket-demo-secrets/`, fuera del repositorio. Se creó esta carpeta con herencia de ACL deshabilitada y permiso de control total para el usuario Windows que ejecutó la preparación; los archivos nuevos heredan ese acceso restringido. No se copiaron secretos al workspace.

`.env` queda cubierto por `database/demo-env/.gitignore`. No existe `.git` en la raíz de esta copia del workspace, por lo que no se pudo comprobar un índice Git; la regla local está preparada para cuando el directorio esté bajo control de versiones. No utilizar git add -f con configuraciones privadas.

## Contraseñas y hashes

Los archivos privados del entorno de integración existen. Aunque técnicamente se podrían montar en otro contenedor, se eligieron credenciales Demo nuevas para evitar compartir secretos entre ambos entornos. No se leyeron, copiaron ni modificaron las contraseñas de integración; no se compararon sus valores con los de Demo.

`Preparar-SecretosDemo.ps1 -Paso Hashes` extrajo exactamente los hashes de las filas originales admin y cajero1 desde el SQL fuente. Se guardaron sin comillas SQL, sin cambios y sin mostrarlos. No se inventaron contraseñas de esos usuarios. El script no sobrescribe hashes distintos existentes.

El usuario ejecutó en PowerShell local estos comandos, uno por uno, desde la raíz del workspace:

```powershell
& .\Servicio1.Ventas\database\demo-env\Preparar-SecretosDemo.ps1 -Paso PasswordRoot
```

```powershell
& .\Servicio1.Ventas\database\demo-env\Preparar-SecretosDemo.ps1 -Paso PasswordVentas
```

Cada comando solicita contraseña y confirmación con entrada oculta, guarda el secreto únicamente en su archivo externo y rechaza sobrescribir un archivo existente. No se deben repetir si los archivos ya existen. No pasar contraseñas como argumentos ni enviarlas al chat. La creación de esos archivos externos requiere permiso de escritura del usuario local; no requiere iniciar Docker.

## Validaciones realizadas

- Los cuatro archivos existen y no están vacíos.
- Los dos hashes coinciden carácter por carácter con los originales; no se imprimieron.
- Las dos contraseñas Demo no están vacías y son distintas entre sí; no se imprimieron.
- `.env` contiene exactamente las cinco variables previstas y rutas absolutas a la carpeta privada externa, sin contraseñas ni hashes literales.
- Docker Compose resuelve los dos montajes de secretos con las rutas privadas esperadas y conserva el proyecto puntomarket-ventas-demo, un único mysql-demo, base puntomarket_ventas_demo_local y publicación 127.0.0.1:3308.
- La validación estática de la copia SQL sigue siendo satisfactoria: 11 tablas, cuatro vistas, 23 triggers, un procedimiento y nueve bloques de semillas equivalentes al original.

La comprobación final se ejecutó con `Validar-ConfiguracionPrivada.ps1`, que lee secretos solo internamente para comprobar su integridad, sin emitir valores. Docker Compose se usó únicamente con config; no se ejecutó up ni se importó SQL. El acceso a la configuración Docker requirió ejecutar la validación fuera del sandbox.

## Próximo paso pendiente de autorización

Repetir la inspección del motor, puerto y ausencia de recursos previos, y solo entonces iniciar exclusivamente mysql-demo. No se ha iniciado nada ahora. La base todavía no está creada ni lista para conectar la API; únicamente está lista su configuración privada. La importación y verificación de datos siguen pendientes. No se modificó el SQL original, la API, el frontend ni bases existentes.
