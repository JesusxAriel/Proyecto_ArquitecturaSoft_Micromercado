# Configuración JWT de Servicio 2

El emisor de tokens lee la sección `Jwt` de la configuración de ASP.NET Core.
Para configuración mediante variables de entorno, establece:

| Variable | Requisito |
|---|---|
| `Jwt__Issuer` | Emisor no vacío, igual al configurado por los servicios validadores. |
| `Jwt__Audience` | Audiencia no vacía, igual a la esperada por los servicios validadores. |
| `Jwt__Key` | Secreto aleatorio de al menos 32 bytes UTF-8 para HS256. No utilizar valores de ejemplo ni guardarlo en el repositorio. |
| `Jwt__ExpireMinutes` | Duración positiva del token en minutos. Si se omite, el valor predeterminado es 60. |

En desarrollo, usa User Secrets o variables de entorno. En despliegues, inyecta
el secreto desde el gestor de secretos del entorno. La misma clave, emisor y
audiencia deben configurarse en los validadores autorizados; no incluyas la
clave en `appsettings.json`, archivos versionados, imágenes o logs.
