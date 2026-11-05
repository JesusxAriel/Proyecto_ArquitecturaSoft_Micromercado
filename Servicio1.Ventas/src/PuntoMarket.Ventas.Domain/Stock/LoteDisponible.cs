namespace PuntoMarket.Ventas.Domain.Stock;

public sealed record LoteDisponible(int IdLote, int CantidadDisponible, DateOnly? FechaVencimiento);
