namespace PuntoMarket.Usuarios.Application.Ports.Gestion;

public interface IGeneradorTokenActivacion
{
    TokenActivacion Generar(DateTimeOffset ahoraUtc, TimeSpan vigencia);
}
