namespace PuntoMarket.Reportes.Domain.Exceptions;

public class RangoDeFechasInvalidoException : Exception
{
    public RangoDeFechasInvalidoException(DateOnly desde, DateOnly hasta)
        : base($"La fecha inicial {desde:yyyy-MM-dd} no puede ser posterior a la fecha final {hasta:yyyy-MM-dd}.")
    {
    }
}
