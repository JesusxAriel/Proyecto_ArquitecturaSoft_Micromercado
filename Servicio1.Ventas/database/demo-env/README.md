# MySQL Demo independiente

Guia de instalacion nueva y arranque: [README raiz](../../../README.md). No asumir que una base ya esta importada al clonar.

Validacion de archivos sin secretos ni conexiones:

```powershell
./Validar-Preparacion.ps1 -OmitirCompose
```

Preparar-CopiaSql.ps1 genera la copia Demo y el manifiesto desde la fuente publicable raiz; no ejecuta SQL. Incluye idUsuarioActualizacion. Los hashes los genera cada instalacion mediante Preparar-SecretosDemo.ps1 -Paso Hashes, nunca se recuperan del repositorio o del monolito.

Importar-EsquemaDemo.ps1 -Importar solo admite una base Demo vacia y un unico intento. No reimportar la base activa ni eliminar registros locales/volumenes para forzarlo. Los informes de importacion en docs describen el entorno anterior, no el estado de cualquier clon.
