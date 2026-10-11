namespace Proyecto_Arquitectura_Micromercado.Domain.Sales;

public class Venta
{
    public int Id { get; set; }
    public string NroComprobante { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; } = DateTime.Now;

    private decimal _total;
    public decimal Total
    {
        get => _total > 0 ? _total : Detalles.Sum(d => d.Subtotal);
        set => _total = value;
    }

    public string Estado { get; set; } = "EN_PROCESO";
    public string? MotivoAnulacion { get; set; }
    public int IdCliente { get; set; }
    public int IdUsuario { get; set; }
    public List<DetalleVenta> Detalles { get; set; } = [];

    public void AgregarDetalle(int idProducto, int cantidad, decimal precioUnitario)
    {
        if (string.Equals(Estado, "ANULADA", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Estado, "ANULADO", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("No se pueden agregar detalles a una venta anulada.");
        }

        Detalles.Add(new DetalleVenta
        {
            IdProducto = idProducto,
            Cantidad = cantidad,
            PrecioUnitario = precioUnitario
        });
    }

    public void CompletarVenta()
    {
        if (Detalles.Count == 0)
        {
            throw new InvalidOperationException("No se puede completar una venta sin detalles.");
        }

        Estado = "COMPLETADA";
    }
}