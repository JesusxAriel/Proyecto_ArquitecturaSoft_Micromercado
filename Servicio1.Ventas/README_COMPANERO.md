# Servicio1.Ventas

El proyecto forma parte de PuntoMarket Entregable 3. La instalacion nueva, credenciales privadas, SMTP, arranque de las tres APIs, frontend y pruebas estan en [README raiz](../README.md).

El frontend Razor Pages permanece en frontend/PuntoMarket.Ventas.Frontend. Consume Ventas, Usuarios y Reportes mediante HTTP; solo permite autenticacion real de Usuarios. /Demo ya no autentica.

No importar SQL en la base existente ni repetir preparacion de usuarios al reiniciar. La solucion raiz PuntoMarket.slnx agrupa el codigo vigente. El compose de database/demo-env despliega exclusivamente MySQL y conserva su volumen.

Los documentos de docs incluyen evidencias de migracion, importacion e integracion anteriores. Consultar su fecha/alcance y usar la guia raiz para comandos actuales.
