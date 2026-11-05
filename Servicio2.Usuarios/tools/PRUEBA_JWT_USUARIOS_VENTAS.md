# Prueba manual JWT Usuarios → Ventas

Preparada, no ejecutada contra APIs reales. No integra ni modifica el frontend.

Desde la raíz del workspace y con ambas APIs DemoLocal ya activas:

```powershell
pwsh -NoProfile -File ./Servicio2.Usuarios/tools/Probar-JwtUsuariosVentas.ps1
```

PowerShell 7 o superior. No recibe parámetros de contraseña ni token. Utiliza el correo Cajero ya verificado en Demo. Solicita la contraseña EXISTENTE con Read-Host -AsSecureString después de comprobar ambas APIs y que Ventas rechaza la consulta sin JWT. No usar Swagger ni pegar secretos en comandos. Ejecutar en consola privada sin transcripción, depuración, trazas, grabación o interceptores externos; el script no puede garantizar ausencia de captura impuesta por el entorno.

Solo permite tres GET de comprobación/consulta y POST de login. Login fue inspeccionado: consulta las credenciales y emite JWT, no actualiza registros. El GET precio tiene Authorize(OperarVentas); requiere usuario autenticado, rol Cajero/Administrador y debeCambiarPassword=false. No hay registro ni anulación de ventas.

HttpClient tiene redirecciones, cookies y proxy deshabilitados, timeout de 15 segundos y direcciones fijas IPv4 loopback. El token se envía solo en Authorization Bearer. Se descartan cuerpos ajenos al login y no se imprime ningún cuerpo, correo, contraseña, token o excepción completa. Ningún secreto se escribe a archivos. Se libera/borra el BSTR y se eliminan referencias sensibles en finally; strings administradas en memoria no ofrecen garantía de borrado inmediato, aunque sus referencias se limpian. HTTP local no cifra el tráfico: esta prueba está limitada a loopback de la Demo.

Salida esperada en éxito (más la solicitud oculta de contraseña):

```text
Usuarios protegido: HTTP 401
Ventas salud: HTTP 200
Ventas sin JWT: HTTP 401
Usuarios login: HTTP 200
Ventas con JWT: HTTP 200
Resultado: Ventas aceptó el JWT y autorizó la consulta.
```

Código de salida 0 en éxito; 1 si se detiene. Un login 401 significa rechazo de Usuarios; Ventas 401 significa rechazo del token. Ventas 403 distingue permiso/cambio obligatorio de contraseña, no afirma fallo de firma. Un 404 inicial señala endpoint de comprobación ausente; un 404 autenticado puede ser producto ausente/inactivo o ruta inexistente, sin afirmar éxito. No se siguen redirecciones ni se reintentan contraseñas. Fallos de red/timeout se muestran sin detalles sensibles y sin imprimir un código HTTP que no se recibió.

Pruebas automatizadas, sin credenciales reales ni llamadas de red:

```powershell
pwsh -NoProfile -NonInteractive -File ./Servicio2.Usuarios/tools/Test-Probar-JwtUsuariosVentas.ps1
```

Incluyen doce escenarios con transporte y contraseña simulados, no exposición de datos simulados, detención antes de pedir contraseña si falla disponibilidad/protección y rechazo de dos destinos no permitidos. Dot-source define funciones para pruebas; no inicia el flujo real.
