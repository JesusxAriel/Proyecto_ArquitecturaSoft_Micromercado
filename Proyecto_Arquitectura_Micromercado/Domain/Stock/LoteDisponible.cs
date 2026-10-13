namespace Proyecto_Arquitectura_Micromercado.Domain.Stock;

public sealed record LoteDisponible(int IdLote, int CantidadDisponible, DateOnly? FechaVencimiento);
