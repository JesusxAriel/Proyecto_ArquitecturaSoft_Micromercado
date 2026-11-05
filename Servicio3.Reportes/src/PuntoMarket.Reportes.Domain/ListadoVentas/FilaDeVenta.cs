namespace PuntoMarket.Reportes.Domain.ListadoVentas;

public sealed record FilaDeVenta(
    int IdVenta,
    string NroComprobante,
    DateTime FechaVenta,
    string NitCi,
    string RazonSocial,
    string Cajero,
    EstadoDeVenta Estado,
    decimal MontoTotal);
