# Activación de cuentas

`/Users/Activar` permite acceso anónimo. Un GET con el enlace del correo no activa la cuenta: guarda el token en IMemoryCache durante un máximo de 15 minutos y redirige a la misma ruta sin consulta. La cookie HttpOnly contiene únicamente una referencia aleatoria. Utiliza SameSite=Lax para que el navegador la envíe en el GET de la redirección cuando se llega desde webmail u otro sitio; las cookies de autenticación conservan SameSite=Strict. El POST permanece protegido por antiforgery. El vencimiento y uso único del token siguen siendo responsabilidad de Usuarios. Reiniciar el frontend invalida estas referencias; se puede volver a abrir el enlace si aún es válido.

El formulario usa antiforgery y envía las contraseñas únicamente al servidor Razor Pages. El cliente tipado envía POST `api/activacion` sin JWT, redirecciones ni reintentos. No se repueblan contraseñas. La política coincide con PoliticaDePassword del backend: longitud mínima 8, mayúscula, minúscula, dígito y carácter no alfanumérico (sin restricciones adicionales de longitud máxima o conjunto ASCII).

Una respuesta 400 indica enlace inválido/expirado después de validar las contraseñas localmente. Ante errores de red/servidor no se afirma éxito: podría haberse completado la operación. Intentar login antes de solicitar otro enlace. No se modifica ni inicia una sesión de login automáticamente.

## URL de Usuarios

Los tres clientes tipados (login, gestión y activación) comparten `UsuariosApi:BaseUrl`. La configuración estándar admite la variable `UsuariosApi__BaseUrl` o el argumento `--UsuariosApi:BaseUrl`.

- Ejecución local: valor predeterminado `http://127.0.0.1:5003/`.
- Contenedor frontend: configurar explícitamente, por ejemplo `UsuariosApi__BaseUrl=http://usuarios:8080/`, donde `usuarios` debe ser el nombre DNS real del servicio en la misma red Docker y 8080 su puerto interno real. No utilizar el puerto publicado en Windows ni localhost para comunicar contenedores. No se permiten credenciales, consulta, fragmento ni rutas adicionales en esta URL.
- Usar HTTPS para enlaces de activación y conexiones externas. HTTP interno solo en una red de confianza. El proceso API debe escuchar en una interfaz alcanzable desde su contenedor hermano; el lanzador LocalDemo limitado a 127.0.0.1 no cumple esa función.

El Dockerfile y compose raíz actuales construyen el proyecto histórico Proyecto_Arquitectura_Micromercado, no este frontend independiente ni los tres microservicios. Este cambio no migra ese despliegue: hay que publicar este frontend y proporcionar el DNS/puerto real de Usuarios en su despliegue existente. La variable anterior configura esta comunicación; no resuelve otras restricciones preexistentes de despliegue de Ventas/Reportes ni cambia SMTP o JWT.

## Protección del enlace y despliegues con réplicas

El frontend suprime los mensajes informativos de Hosting.Diagnostics, que normalmente incluyen la consulta completa de la petición inicial. No habilitar HTTP logging de consultas/cuerpos. Cualquier proxy, balanceador, APM o plataforma externa debe omitir/redactar la consulta de `/Users/Activar`; el frontend no controla esos logs. Se devuelve `Cache-Control: no-store` y `Referrer-Policy: no-referrer`.

Como el token y las claves antiforgery se conservan en memoria del proceso, las réplicas requieren afinidad de sesión con el mismo frontend. No se ha añadido almacenamiento distribuido ni modificado la configuración de Data Protection. El enlace original necesariamente llega al navegador y al servidor; no compartirlo ni copiarlo a terminales o incidencias.

## Prueba manual autorizada posteriormente

1. Iniciar Usuarios y este frontend con la URL correcta. Comprobar que el correo nuevo apunta al origen público de este frontend.
2. Abrir un enlace nuevo del correo en el navegador. Debe redirigir a `/Users/Activar` sin token visible en el formulario.
3. Probar confirmaciones distintas: deben rechazarse sin repoblar contraseñas.
4. Introducir una contraseña que cumpla la política y confirmarla. Verificar el mensaje de activación y el enlace a `/Account/Login`.
5. Iniciar sesión manualmente. Volver a abrir un enlace ya utilizado o expirado y enviar el formulario debe mostrar un error seguro, nunca el token.

Las pruebas automatizadas usan un HttpMessageHandler simulado: no envían correo, no llaman a Usuarios real y no consumen tokens reales.

## Regresión de entrada desde correo

Causa comprobada en Chromium: con SameSite=Strict, llegar desde otro sitio y redirigir inmediatamente al GET sin token omite la cookie de correlación en ese GET. La referencia sí se almacena en el navegador, pero no llega al servidor hasta otra navegación del mismo sitio. No es un fallo del cliente HTTP de activación: todavía no se ha enviado ningún POST a Usuarios. HttpClient/CookieContainer no implementa la política SameSite de un navegador y no detectaba esta regresión.

Comprobación del entorno durante el diagnóstico: docker ps mostró únicamente el contenedor MySQL, sin contenedores frontend ni réplicas. La primera consulta Get-NetTCPConnection no tenía permisos; netstat confirmó posteriormente un único listener local 127.0.0.1:5100, PID 9596, proceso PuntoMarket.Ventas.Frontend. Un GET con un enlace ficticio a ese proceso devolvió 302 y una cookie HttpOnly con SameSite=Strict y Path=/Users/Activar. Un cliente HTTP que sí reenvía esa cookie obtuvo el formulario 200: el estado en memoria funcionaba y el fallo dependía de la política del navegador. La reproducción cross-site contra la página real se hizo además en un frontend temporal local, sin Docker ni APIs reales. No se atribuye el fallo a un balanceador que no se observó. El proceso local existente no se reinició; hay que reiniciarlo con el código corregido. Si otro despliegue tiene varias réplicas, sigue necesitando afinidad de sesión por el estado en memoria y las claves antiforgery (véase arriba); no se ha añadido persistencia de tokens ni cambiado la arquitectura.

Desde la raíz del workspace:

```powershell
dotnet build Servicio1.Ventas/frontend/PuntoMarket.Ventas.Frontend --no-restore -p:BaseOutputPath=bin/activacion-usuarios/
python Servicio1.Ventas/tests/activacion-cookie-browser.test.py
```

La prueba requiere Chrome/Chromium (opcionalmente CHROME_BIN), arranca su propio frontend efímero y usa perfiles temporales, enlaces ficticios y una dirección de API inalcanzable. Un control reproduce la pérdida con Strict; el segundo abre el enlace cross-site contra la página real y comprueba formulario, antiforgery y ausencia del token. No inicia sesión ni envía POST de activación. En una sandbox, el navegador puede requerir permiso para ejecutarse.

Para comprobar el despliegue local o Docker: reiniciar/recrear únicamente el frontend con este build, abrir un enlace desde webmail y verificar en Network que el primer 302 fija la cookie PuntoMarket.Activacion con HttpOnly, SameSite=Lax y Path=/Users/Activar, y que el siguiente GET sin consulta devuelve el formulario. No copiar el token, capturas de la URL ni cabeceras completas a logs/incidencias. No es necesario enviar el formulario para comprobar esta corrección.
