# Preparacion del repositorio - Entregable 3

## Respaldo y archivos retirados

Antes de retirar originales se creo un archivo historico privado fuera del workspace, con ACL restringida al usuario propietario, conservando rutas relativas. Se verificaron 654 archivos mediante SHA-256 sin publicar sus valores. Incluye monolito, pruebas, Dockerfile/compose/nginx historicos, solucion anterior, backup, ZIP, SQL y documentos/configuraciones originales. No pertenece al repositorio publico.

Del arbol activo se retiraron Proyecto_Arquitectura_Micromercado/, Proyecto_Arquitectura_Micromercado.Tests/, nginx/, Dockerfile, docker-compose.yml, Proyecto_Arquitectura_Micromercado.slnx, backups/, el ZIP antiguo, la .env raiz del monolito y doc/.gitkeep. Se conserva el SQL raiz porque scripts vigentes lo necesitan.

Los cinco informes historicos y el README antiguo se reubicaron en doc/historico; el documento de integracion se movio a doc/integracion y se actualizo. El verificador de frontend que intentaba autenticar con /Demo se reubico como evidencia historica, no como prueba vigente.

## SQL y seguridad

La fuente conserva objetos y relaciones del esquema, incluida idUsuarioActualizacion con indice y FK. Se externalizaron los dos hashes a variables de sesion y se sustituyeron contactos, nombres y documentos de personas por semillas sinteticas. Se retiraron DROP DATABASE/CREATE DATABASE y sustitucion de objetos. No se ejecuto SQL ni se modifico MySQL activo.

La copia Demo se regenero exclusivamente desde la fuente publicable y el manifiesto se actualizo. El generador de hashes ahora solicita contrasenas nuevas y usa BCrypt; el validador privado comprueba formato, no igualdad con secretos antiguos. El importador conserva su guarda de base vacia, sesion unica y no reintento.

El lanzador Ventas obtiene de MySQL el identificador del cliente usado en su comprobacion de lectura para admitir tanto semillas nuevas como una Demo anterior. No cambio la logica de negocio.

Se incorporaron PuntoMarket.slnx, guia raiz, plantillas publicas, validaciones de repositorio y preparacion opcional de cuentas locales con host observado desde Windows. Preparar-CuentasLocales.ps1 NO se ejecuto: exige -Aplicar, cuentas ausentes, permisos privados y esquema preparado; crea exclusivamente las dos cuentas documentadas con TLS para una instalacion nueva.

Las exclusiones cubren configuraciones privadas, claves, archivos de ejecucion, logs, respaldos y entregas; permiten appsettings publicos y plantillas. Archivos locales ignorados preexistentes pueden permanecer para no romper el entorno en uso, pero no se deben incorporar a Git ni a paquetes publicos.

## Limites de la validacion

No se hizo git init/add/commit/push. No habia metadatos Git en el workspace, por lo que no se puede certificar el historial de un repositorio remoto. No se ejecutaron altas, login, activacion, correo, importacion, creacion de cuentas, migraciones ni operaciones de Docker sobre el entorno activo. La prueba completa de clonacion desde cero requiere un entorno nuevo aislado y sigue siendo un paso manual posterior.

Consultar el resultado final de compilacion y pruebas en la entrega de esta tarea. Las advertencias NU1900 indican que la consulta remota de vulnerabilidades no pudo completarse; no equivalen a paquetes libres de vulnerabilidades.

## Verificacion realizada

- Restauracion y build de la solucion: correctos; cuatro advertencias NU1900 por consulta NuGet inaccesible incluso tras reintento autorizado.
- .NET: Ventas 433 aprobadas/9 omitidas; Usuarios 132 aprobadas/2 omitidas; Reportes 81 aprobadas. Total 646 aprobadas y 11 omitidas.
- JavaScript: 20 aprobadas. Navegador: 2 aprobadas con frontend temporal, sin API real.
- Repositorio: 8 pruebas de rutas, exclusiones, fuente, manifiesto y esquema. PowerShell: sintaxis de scripts y ejemplos README validada; tests aislados de guardas, BCrypt, permisos y 12 escenarios JWT simulados.
- Compose: modelo validado con plantilla publica, sin arrancar recursos. SQL: validacion estatica, no importado.
- Revision de secretos: comparacion en memoria del arbol publicable con hashes/contrasenas historicas y clave JWT externa; ninguna coincidencia detectada. No implica certificar un historial Git inexistente o secretos desconocidos.

El correo de Cajero empleado por el script manual de prueba JWT y su prueba simulada se alineo con la nueva semilla sintetica. No se ejecuto login real. Las evidencias antiguas de configuracion privada y la guia aislada del companero se conservaron en historico; sus guias vigentes remiten al README raiz.

Tambien se archivaron y retiraron .runtime-validation (logs/certificado de ejecucion de pruebas) y EsquemaPruebas.sql generado, tras verificar sus copias. Los binarios de compilacion locales quedan ignorados; no se borraron archivos en uso. La plantilla de Demo usa TU_USUARIO en lugar de rutas personales; el README genera la .env local con el perfil real de quien instala.

La solucion final contiene 20 proyectos vigentes; se excluyo el runner temporal alojado en .runtime-validation. Se comprobaron los enlaces locales de documentacion vigente sin enlaces rotos.

## Inventario de cambios

### Creados

- PuntoMarket.slnx (20 proyectos vigentes).
- README.md reescrito y .env.example raiz nueva.
- tools/Preparar-CuentasLocales.ps1, tools/Test-PreparacionLocal.ps1 y tools/validar_repositorio.py.
- doc/despliegue/ESTADO_DOCKER.md, doc/historico/LEEME.md y este informe.
- Copias historicas CONFIGURACION_PRIVADA_DEMO.ANTERIOR.md y README_COMPANERO.ANTERIOR.md antes de actualizar las guias correspondientes.

### Movidos

- AUDITORIA_MONOLITO_VENTAS.md, CAMBIOS_TECNICOS.md, EXPLICACION_ESTRUCTURA_ACTUAL.md, PREPARACION_REFACTOR_VENTAS.md y PROJECT_STRUCTURE_ACTUAL.md a doc/historico/.
- README.md anterior a doc/historico/README_MONOLITO.md.
- INTEGRACION_VENTAS_REPORTES.md a doc/integracion/ (actualizado).
- Servicio1.Ventas/database/demo-env/Verificar-FrontendLecturas.ps1 a doc/historico/Verificar-FrontendLecturas.DEMO-RETIRADO.ps1.

### Modificados

- .gitignore y .dockerignore: exclusiones privadas/generadas y configuraciones publicas permitidas.
- bdMicroMercadoArqui.sql: semillas sinteticas y hashes externos; conserva las definiciones de esquema.
- Servicio1.Ventas/database/demo-env/: Preparar-CopiaSql.ps1, Preparar-SecretosDemo.ps1, Validar-ConfiguracionPrivada.ps1, Validar-Preparacion.ps1, sql/PuntoMarket.Ventas.Demo.sql, manifest.json, .env.example y README.md.
- Servicio1.Ventas/tools/PuntoMarket.Ventas.DemoLocal/Program.cs: comprobacion de cliente sin identificador personal fijo.
- Servicio2.Usuarios/tools/Probar-JwtUsuariosVentas.ps1 y Test-Probar-JwtUsuariosVentas.ps1: correo de fixture/semilla sintetica.
- Servicio1.Ventas/README_COMPANERO.md y docs/: guia privada actualizada, enlaces a documentos movidos, advertencias de evidencia anterior y normalizacion de texto.
- Documentacion historica: identificacion como tal y referencias a codigo archivado sin presentarlo como dependencia activa.

### Retirados del arbol activo, con respaldo privado

- Monolito, sus pruebas, Dockerfile y compose raiz, nginx, solucion antigua, backups y ZIP de entrega.
- .env raiz privada del monolito; .env.example antigua sustituida por plantilla publica actual.
- .runtime-validation de integracion y EsquemaPruebas.sql generado.
- doc/.gitkeep innecesario (vacio).

Los fuentes de dominio, aplicacion, infraestructura/API, frontend, autenticacion y activacion de negocio permanecen sin cambios. No se eliminaron datos ni volumenes Docker ni se modificaron secretos externos existentes. No se ejecuto el nuevo preparador de cuentas ni el generador interactivo de secretos.
