namespace Proyecto_Arquitectura_Micromercado.Application.Sales;

public sealed class PrecioCambiadoException(int idProducto, decimal precioVigente, string message)
    : InvalidOperationException(message)
{
    public int IdProducto { get; } = idProducto;

    public decimal PrecioVigente { get; } = precioVigente;
}
