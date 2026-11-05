# Configuracion privada de Demo

La guia canonica es [README raiz](../../README.md). En una instalacion NUEVA, Preparar-SecretosDemo.ps1 solicita contrasenas propias de MySQL y de bootstrap, genera BCrypt con la biblioteca de Usuarios y crea una clave JWT aleatoria. Ningun hash se extrae de un SQL ni de archivos archivados.

Las cinco variables locales en database/demo-env/.env son VENTAS_DEMO_USER, VENTAS_DEMO_ROOT_PASSWORD_FILE, VENTAS_DEMO_PASSWORD_FILE, VENTAS_DEMO_ADMIN_HASH_FILE y VENTAS_DEMO_CAJERO_HASH_FILE. Solo contienen usuario y rutas externas; los archivos privados estan bajo el perfil de quien instala, con ACL restringida.

Validar-ConfiguracionPrivada.ps1 valida formato BCrypt, presencia de archivos, contrasenas MySQL no vacias y diferentes y el modelo Compose. No compara hashes con secretos publicados ni ejecuta SQL.

El importador consume las dos variables privadas en la misma sesion y solo permite una base vacia. Las cuentas adicionales de Usuarios y Reportes se preparan con tools/Preparar-CuentasLocales.ps1 en una instalacion nueva; nunca reutilizar root o la cuenta de Reportes para Usuarios.

Si ya hay secretos, cuentas o base existente, no repetir preparacion ni sobrescribirlos. Los informes de resultados anteriores describen ese entorno particular, no requisitos privados para un clon nuevo. No publicar .env, contrasenas, hashes, JWT, claves ni cadenas completas.
