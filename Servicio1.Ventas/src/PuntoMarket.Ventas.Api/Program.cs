using PuntoMarket.Ventas.Api;

await using var app = ApiBootstrap.Crear(args);
await app.RunAsync();
