using System.Globalization;

namespace Proyecto_Arquitectura_Micromercado.Pages.Shared;

// Direccion del comprobante de una venta. La pagina que registra la venta la usa para
// abrir el comprobante en una pestana nueva, sin armar la ruta a mano.
public static class ComprobanteUrl
{
    public static string Para(int idVenta) =>
        string.Create(CultureInfo.InvariantCulture, $"/Comprobante/{idVenta}");
}
