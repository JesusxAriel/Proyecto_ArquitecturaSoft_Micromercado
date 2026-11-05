# Integracion vigente de PuntoMarket

La guia de arranque es [README raiz](../../README.md). El frontend unico sigue dentro de Servicio1.Ventas/frontend/PuntoMarket.Ventas.Frontend.

- Usuarios emite el JWT real HS256. El frontend valida firma, issuer, audience, nbf, exp, rol y debeCambiarPassword.
- SesionesUsuario conserva el JWT en memoria; el navegador recibe solo una cookie protegida con referencia de sesion. /Demo redirige a /Account/Login y no emite JWT.
- ResolucionSesion adjunta Bearer exclusivamente desde la sesion real a Ventas, Reportes y gestion de Usuarios. 401 invalida la sesion correspondiente; 403 la conserva. No se repiten escrituras automaticamente.
- Ventas y Reportes mantienen sus contratos, filtros, paginacion visual, totales y exportadores. Reportes utiliza SQL directo y una cuenta SELECT independiente; no llama por HTTP a Ventas.
- /Users y sus handlers son exclusivos del Administrador. /Users/Activar es anonimo, con antiforgery, token solo en memoria y cookie de correlacion SameSite=Lax para la llegada desde webmail.
- Las sesiones/JWT expiran sin renovacion automatica. El cambio obligatorio de password bloquea paginas operativas. Edicion y reactivacion no estan implementadas.

Las APIs locales usan 5002, 5003 y 5012; Razor usa 5100. UsuariosApi:BaseUrl es compartida por los clientes de login, gestion y activacion. Docker requiere DNS interno, no localhost entre contenedores. No hay un compose conjunto validado; ver [estado Docker](../despliegue/ESTADO_DOCKER.md).

El documento anterior de integracion y los perfiles Demo son historia de la migracion. Los archivos de configuracion privada del autor no forman parte del procedimiento de clonacion.
