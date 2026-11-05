namespace PuntoMarket.Reportes.Domain.VentasPorCategoria;

public sealed record FilaDeProducto(int IdProducto, string NombreProducto, long UnidadesVendidas, decimal MontoVendido);
