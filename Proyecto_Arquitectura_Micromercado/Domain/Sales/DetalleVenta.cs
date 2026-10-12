namespace Proyecto_Arquitectura_Micromercado.Domain.Sales;

public sealed class DetalleVenta
{
    public int Id { get; set; }
    public int IdVenta { get; set; }
    public int IdProducto { get; set; }
    public string? NombreProducto { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal => Cantidad * PrecioUnitario;
}