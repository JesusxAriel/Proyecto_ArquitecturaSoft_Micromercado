# Estado real del despliegue

El procedimiento soportado esta en [README](../../README.md): MySQL 8.4 en Docker y los tres servicios mas Razor Pages ejecutados localmente mediante .NET 10/PowerShell 7.6.

## Operativo y versionado

- Servicio1.Ventas/database/demo-env/compose.yaml: un MySQL, volumen persistente, red propia, secretos externos y publicacion 127.0.0.1:3308. No ejecuta automaticamente SQL.
- Servicio1.Ventas/tests/integration-env/compose.yaml: entorno separado de pruebas, no la Demo.
- Servicio2.Usuarios/Dockerfile: construye la API Usuarios con contexto en la raiz del repositorio. Ejemplo de build, no arranque completo: docker build -f Servicio2.Usuarios/Dockerfile -t puntomarket-usuarios:local .

El compose, Dockerfile y nginx raiz antiguos desplegaban el monolito. Se archivaron fuera del arbol activo; no se los renombro como despliegue de microservicios.

## Pendiente para contenerizacion completa

Faltan definir/validar imagenes y orquestacion del frontend, Ventas y Reportes; provision privada de JWT/SMTP/conexiones; HTTPS y certificados; URL/DNS internos y adecuacion de las validaciones actualmente locales; healthchecks; estado/claves compartidos o afinidad entre replicas. No modificar restricciones locales ni desactivar TLS para simular un despliegue listo.

UsuariosApi__BaseUrl debe apuntar al DNS y puerto interno reales de Usuarios. Los lanzadores LocalDemo restringen puertos y loopback para ejecutar en Windows, no para comunicar contenedores.

Mantener secretos fuera del contexto Docker y del Git. No eliminar volumenes para reiniciar; usar stop/start. No activar logs de consultas/cuerpos en enlaces de activacion o login. El esquema local compartido y la cuenta ventas_demo de importacion requieren endurecimiento antes de produccion.
