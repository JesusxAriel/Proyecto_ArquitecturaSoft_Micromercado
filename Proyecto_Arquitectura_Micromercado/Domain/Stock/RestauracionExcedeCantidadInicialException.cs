namespace Proyecto_Arquitectura_Micromercado.Domain.Stock;

public class RestauracionExcedeCantidadInicialException : InvalidOperationException
{
    public RestauracionExcedeCantidadInicialException()
        : base("No se puede anular la venta: restaurar el stock de un lote superaría su cantidad inicial.")
    {
    }

    public RestauracionExcedeCantidadInicialException(string message)
        : base(message)
    {
    }
}
