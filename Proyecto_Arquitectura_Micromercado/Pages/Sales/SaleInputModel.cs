using System.ComponentModel.DataAnnotations;

namespace Proyecto_Arquitectura_Micromercado.Pages.Sales;

// Campos exclusivos de la pantalla; no representan una venta persistida.
public sealed class SaleInputModel
{
    public int? IdCliente { get; set; }

    [Display(Name = "Identificación del cliente")]
    public string? IdentificacionCliente { get; set; }

    [Display(Name = "Buscar producto")]
    public string? BusquedaProducto { get; set; }

    [Display(Name = "Cantidad")]
    [Range(1, int.MaxValue, ErrorMessage = "Ingrese una cantidad mayor a cero.")]
    public int Cantidad { get; set; } = 1;
}
