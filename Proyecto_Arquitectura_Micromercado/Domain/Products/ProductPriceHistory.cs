namespace Proyecto_Arquitectura_Micromercado.Domain.Products;

public sealed class ProductPriceHistory
{
    public int Id { get; set; }
    public int IdProducto { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public decimal PrecioVentaAnterior { get; set; }
    public decimal PrecioVentaNuevo { get; set; }
    public decimal? PrecioCostoAnterior { get; set; }
    public decimal? PrecioCostoNuevo { get; set; }
    public string MotivoCambio { get; set; } = string.Empty;
    public int IdUsuario { get; set; }
    public DateTime FechaCambio { get; set; }
}
