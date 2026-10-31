namespace PuntoMarket.Usuarios.Application.Ports.Gestion;

public sealed record TokenActivacion(string Valor, string Hash, DateTimeOffset Expira);
