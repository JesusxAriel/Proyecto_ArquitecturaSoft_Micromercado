# Etapa 3: resolución de sesiones para Ventas y Reportes

El login principal continúa en /Demo. /Account/Login es un acceso real explícito y ahora permite utilizar las páginas operativas con los roles validados de Usuarios. No se eliminó el mecanismo Demo ni se cambió la emisión JWT de las APIs.

## Política por petición

Si existe cookie PuntoMarket.Usuarios.Session, se autentica exclusivamente su esquema y se utiliza ese principal antes de autorizar Razor Pages. Si es inválida/vencida, la petición queda anónima incluso si trae una cookie Demo válida: no hay fallback en esa petición. Con sesión real válida, solo se envía su JWT. Sin cookie real se mantiene el flujo Demo. No se unen identidades ni se toma el rol de formularios. Si se cierra explícitamente la sesión real y solo queda cookie Demo en una petición posterior, se aplica el mecanismo Demo todavía vigente; su etiqueta visible identifica ese mecanismo.

ResolucionSesion es scoped y selecciona una sesión por petición. Ambos clientes usan el token en Authorization Bearer por solicitud; no en DefaultRequestHeaders, URLs, JavaScript o cookies. Los filtros, resultados, límites y exportadores permanecen intactos.

## Errores

401 remoto: invalidar la sesión seleccionada por ID, borrar solo su cookie y dejar anónimo el principal de esa petición. No se invalida el otro mecanismo ni otros IDs de sesión. No hay reintento ni cambio de token para la operación fallida, incluidos POST. El procesamiento no depende del formato del cuerpo de error remoto.

403 remoto: conservar sesión y devolver acceso denegado. Los POST también preservan 403 aunque el cuerpo remoto no sea JSON. La autorización local de reportes y anulación usa exactamente el mismo principal seleccionado para el JWT. Las peticiones sin permiso no llegan a la API.

Handlers conservan 401/403; descargas mantienen esos estados sin devolverlos como archivos. La cabecera X-PuntoMarket-Login contiene solo /Account/Login o /Demo, nunca datos privados. El helper fetchConSesion utiliza esa ruta conocida en 401 y no reenvía peticiones. Para navegación protegida sin sesión, el acceso predeterminado sigue /Demo; si se intentó usar cookie real se dirige a /Account/Login. Los 403 locales se devuelven como acceso denegado, sin redirigir a un endpoint inexistente.

La barra de usuario indica Usuarios o Demo y su botón Salir apunta al cierre del mecanismo seleccionado. El enlace principal para entrar sigue Demo.

## Validación

Pruebas HTTP con login y APIs simulados: sesión real, Demo, ambas cookies, prioridad Cajero real sobre Admin Demo, vencimiento, cookie corrupta, aislamiento entre identidades y sesiones, 401/403, POST único sin reintento, filtros y exportación. La regresión de etapas previas se actualizó para esperar acceso operativo real y 403 local en lugar de redirección.

No se ejecutaron logins ni operaciones reales, ni se modificaron las APIs, SQL, credenciales o base de datos. Los POST de los tests usan exclusivamente HttpMessageHandler simulado. Las pruebas MySQL permanecen omitidas.
