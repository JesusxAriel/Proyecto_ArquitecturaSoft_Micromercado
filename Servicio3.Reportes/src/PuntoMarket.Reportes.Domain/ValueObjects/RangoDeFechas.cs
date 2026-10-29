using PuntoMarket.Reportes.Domain.Exceptions;

namespace PuntoMarket.Reportes.Domain.ValueObjects;

public sealed record RangoDeFechas
{
    public DateOnly? Desde { get; }
    public DateOnly? Hasta { get; }

    public RangoDeFechas(DateOnly? desde, DateOnly? hasta)
    {
        if (EstaInvertido(desde, hasta))
            throw new RangoDeFechasInvalidoException(desde!.Value, hasta!.Value);

        Desde = desde;
        Hasta = hasta;
    }

    public static RangoDeFechas SinLimites() => new(null, null);

    private static bool EstaInvertido(DateOnly? desde, DateOnly? hasta) =>
        desde.HasValue && hasta.HasValue && desde.Value > hasta.Value;
}
