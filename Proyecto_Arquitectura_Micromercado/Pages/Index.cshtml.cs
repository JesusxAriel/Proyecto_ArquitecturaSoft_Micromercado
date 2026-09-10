using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Proyecto_Arquitectura_Micromercado.Pages
{
    public sealed class IndexModel(IProductService productService) : PageModel
    {
        public decimal DailySales { get; private set; }
        public int ActiveProductCount { get; private set; }
        public int LowStockCount { get; private set; }
        public string? DatabaseWarning { get; private set; }

        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            DailySales = 0m;

            try
            {
                var products = await productService.GetAllAsync(cancellationToken);
                ActiveProductCount = products.Count;
                LowStockCount = products.Count(product => product.StockCalculado <= product.StockMinimo);
            }
            catch (MySqlException)
            {
                ActiveProductCount = 0;
                LowStockCount = 0;
                DatabaseWarning = "No se pudo conectar con la base de datos. Las métricas se muestran en cero.";
            }
        }
    }
}
