using System.ComponentModel.DataAnnotations;

namespace Proyecto_Arquitectura_Micromercado.Domain.Products;

public sealed class Product
{
    public int Id { get; set; }

    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "Campo obligatorio.")]
    [StringLength(150)]
    [ProductText]
    public string Nombre { get; set; } = string.Empty;

    [Display(Name = "Empaque / Presentación")]
    [Required(ErrorMessage = "Campo obligatorio.")]
    [StringLength(100)]
    [ProductText]
    public string EmpaquePresentacion { get; set; } = string.Empty;

    [Display(Name = "Precio Venta")]
    [RegularExpression(ProductPriceValidation.DecimalPattern, ErrorMessage = ProductPriceValidation.SaleMessage)]
    [SalePrice(ErrorMessage = ProductPriceValidation.SaleMessage)]
    public decimal PrecioVenta { get; set; }

    [Display(Name = "Precio Costo")]
    [RegularExpression(ProductPriceValidation.DecimalPattern, ErrorMessage = ProductPriceValidation.CostMessage)]
    [CostPrice(ErrorMessage = ProductPriceValidation.CostMessage)]
    public decimal PrecioCosto { get; set; }

    [Display(Name = "Stock Mínimo")]
    [Range(0, int.MaxValue, ErrorMessage = "Ingrese un entero positivo. Ej: 10")]
    public int StockMinimo { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una opción.")]
    public int IdCategoria { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una opción.")]
    public int IdProveedor { get; set; }

    public string? MotivoCambio { get; set; }

    public bool EstaActivo { get; set; } = true;
}

public sealed class ProductListItem
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string EmpaquePresentacion { get; init; } = string.Empty;
    public decimal PrecioVenta { get; init; }
    public decimal PrecioCosto { get; init; }
    public int StockMinimo { get; init; }
    public int IdCategoria { get; init; }
    public string NombreCategoria { get; init; } = string.Empty;
    public int IdProveedor { get; init; }
    public string NombreProveedor { get; init; } = string.Empty;
    public int StockCalculado { get; init; }
}

public sealed record LookupOption(int Id, string Nombre);
