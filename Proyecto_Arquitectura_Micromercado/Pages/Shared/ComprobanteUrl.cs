using System.Globalization;

namespace Proyecto_Arquitectura_Micromercado.Pages.Shared;

public static class ComprobanteUrl
{
    public static string Para(int idVenta) =>
        string.Create(CultureInfo.InvariantCulture, $"/Comprobante/{idVenta}");
}
