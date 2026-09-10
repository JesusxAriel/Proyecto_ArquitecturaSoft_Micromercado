using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Proyecto_Arquitectura_Micromercado.Pages.Products;

public abstract class ProductFormModel(IProductService productService) : PageModel
{
    protected IProductService ProductService { get; } = productService;

    [BindProperty]
    public Product Product { get; set; } = new();

    [BindProperty]
    public ProductPriceInput Input { get; set; } = new();

    public IEnumerable<SelectListItem> Categories { get; private set; } = [];
    public IEnumerable<SelectListItem> Suppliers { get; private set; } = [];

    protected async Task LoadLookupsAsync(CancellationToken cancellationToken)
    {
        var categories = await ProductService.GetCategoriesAsync(cancellationToken);
        var suppliers = await ProductService.GetSuppliersAsync(cancellationToken);
        Categories = categories.Select(option => new SelectListItem(option.Nombre, option.Id.ToString()));
        Suppliers = suppliers.Select(option => new SelectListItem(option.Nombre, option.Id.ToString()));
    }

    protected bool TryParsePrices()
    {
        ModelState.Remove("Product.PrecioVenta");
        ModelState.Remove("Product.PrecioCosto");

        var saleValid = TryParsePrice(Input.PrecioVentaInput, "Input.PrecioVentaInput", true, out var salePrice);
        var costValid = TryParsePrice(Input.PrecioCostoInput, "Input.PrecioCostoInput", false, out var costPrice);

        if (saleValid)
        {
            Product.PrecioVenta = salePrice;
        }

        if (costValid)
        {
            Product.PrecioCosto = costPrice;
        }

        return saleValid && costValid;
    }

    protected static void SetPriceInputs(Product product, ProductPriceInput input)
    {
        input.PrecioVentaInput = product.PrecioVenta.ToString("0.00", CultureInfo.InvariantCulture);
        input.PrecioCostoInput = product.PrecioCosto.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private void AddPriceError(string key)
    {
        ModelState.AddModelError(key, "Formato inválido. Ej: 10,50 o 10.50");
    }

    private bool TryParsePrice(string? rawValue, string key, bool requireSaleDenomination, out decimal price)
    {
        price = 0;
        var normalizedValue = rawValue?.Trim().Replace(',', '.');
        if (string.IsNullOrWhiteSpace(normalizedValue) ||
            !Regex.IsMatch(normalizedValue, ProductPriceValidation.DecimalPattern) ||
            !decimal.TryParse(normalizedValue, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out price) ||
            price <= 0 ||
            (requireSaleDenomination && decimal.Round(price * 10, 0) != price * 10))
        {
            AddPriceError(key);
            return false;
        }

        price = decimal.Round(price, 2, MidpointRounding.AwayFromZero);
        return true;
    }
}

public sealed class ProductPriceInput
{
    public string PrecioVentaInput { get; set; } = string.Empty;
    public string PrecioCostoInput { get; set; } = string.Empty;
}
