using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Application.Sales;
using Proyecto_Arquitectura_Micromercado.Domain.Products;

namespace Proyecto_Arquitectura_Micromercado.Pages.Sales;

public class IndexModel(
    IVentaRepository ventaRepository,
    IProductRepository productRepository,
    IVentaFacade ventaFacade) : PageModel
{
    public IReadOnlyList<Domain.Sales.Venta> Ventas { get; set; } = [];
    public IReadOnlyList<ProductListItem> ProductosDisponibles { get; set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await CargarDatosAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostRegistrarVentaAsync([FromForm] NuevaVentaDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var resultado = await ventaFacade.RegistrarVentaAsync(dto, cancellationToken);
            StatusMessage = $"Venta registrada exitosamente. Total: Bs. {resultado.Total:N2}";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAnularVentaAsync(int idVenta, CancellationToken cancellationToken)
    {
        try
        {
            var exito = await ventaFacade.AnularVentaAsync(idVenta, cancellationToken);
            if (exito)
            {
                StatusMessage = "La venta fue anulada correctamente y el stock ha sido restituido.";
            }
            else
            {
                ErrorMessage = "No se pudo anular la venta.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    private async Task CargarDatosAsync(CancellationToken cancellationToken)
    {
        Ventas = await ventaRepository.GetAllAsync(cancellationToken);
        ProductosDisponibles = await productRepository.GetAllAsync(cancellationToken);
    }
}