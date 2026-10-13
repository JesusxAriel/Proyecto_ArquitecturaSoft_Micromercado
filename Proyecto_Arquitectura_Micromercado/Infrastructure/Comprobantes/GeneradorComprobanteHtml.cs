using System.Globalization;
using System.Net;
using System.Text;
using Proyecto_Arquitectura_Micromercado.Application.Comprobantes;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Comprobantes;

public sealed class GeneradorComprobanteHtml : IGeneradorComprobante
{
    private const string RutaDelLogo = "/images/logo.jpg";
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-BO");

    public string GenerarHtml(ComprobanteDto comprobante)
    {
        ArgumentNullException.ThrowIfNull(comprobante);

        var html = new StringBuilder();

        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"es\">");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset=\"utf-8\" />");
        html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
        html.AppendLine($"<title>Comprobante de venta {Esc(comprobante.NroComprobante)} - PuntoMarket</title>");
        html.AppendLine("<style>");
        html.AppendLine(Estilos);
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("<main class=\"recibo\">");

        html.AppendLine("<header class=\"encabezado\">");
        html.AppendLine($"<img class=\"logo\" src=\"{RutaDelLogo}\" alt=\"Logo\" />");
        html.AppendLine("<h1>COMPROBANTE DE VENTA</h1>");
        html.AppendLine("</header>");

        if (comprobante.EstaAnulada)
        {
            html.AppendLine("<p class=\"anulada\" role=\"alert\">ANULADA</p>");
        }

        html.AppendLine("<section class=\"datos\">");
        html.AppendLine($"<p><strong>Fecha:</strong> {comprobante.FechaVenta.ToString("dd/MM/yyyy", Cultura)}</p>");
        html.AppendLine("<div class=\"datos-cliente\">");
        html.AppendLine($"<span><strong>CI/NIT:</strong> {Esc(comprobante.NitCiCliente)}</span>");
        html.AppendLine($"<span><strong>Razón Social:</strong> {Esc(comprobante.RazonSocialCliente)}</span>");
        html.AppendLine("</div>");
        html.AppendLine("</section>");

        html.AppendLine("<table class=\"detalle\">");
        html.AppendLine("<thead><tr>");
        html.AppendLine("<th>Cantidad</th><th>Descripción</th><th class=\"num\">Precio Unitario Bs.</th><th class=\"num\">Importe Bs.</th>");
        html.AppendLine("</tr></thead>");
        html.AppendLine("<tbody>");

        foreach (var linea in comprobante.Lineas)
        {
            html.AppendLine("<tr>");
            html.AppendLine($"<td class=\"centro\">{linea.Cantidad.ToString(Cultura)}</td>");
            html.AppendLine($"<td>{Esc(linea.Descripcion)}</td>");
            html.AppendLine($"<td class=\"num\">{Dinero(linea.PrecioUnitario)}</td>");
            html.AppendLine($"<td class=\"num\">{Dinero(linea.Importe)}</td>");
            html.AppendLine("</tr>");
        }

        html.AppendLine("</tbody>");
        html.AppendLine("<tfoot>");
        html.AppendLine("<tr>");
        html.AppendLine($"<td colspan=\"3\" class=\"num total\">Total Bs:</td><td class=\"num total\">{Dinero(comprobante.Total)}</td>");
        html.AppendLine("</tr>");
        html.AppendLine("</tfoot>");
        html.AppendLine("</table>");

        html.AppendLine("<div class=\"pie-container\">");
        html.AppendLine($"<p class=\"letras\">{Esc(comprobante.TotalEnLetras)}</p>");
        html.AppendLine($"<p class=\"firma-sistema\">{comprobante.FechaGeneracion.ToString("dd/MM/yyyy - HH:mm:ss", Cultura)}-{Esc(comprobante.UsuarioGenerador)}</p>");
        html.AppendLine("</div>");

        html.AppendLine("<div class=\"acciones\">");
        html.AppendLine("<button type=\"button\" onclick=\"window.print()\">Imprimir Comprobante</button>");
        html.AppendLine("</div>");

        html.AppendLine("</main>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    private static string Esc(string valor) => WebUtility.HtmlEncode(valor);
    private static string Dinero(decimal monto) => monto.ToString("N2", Cultura);

    private const string Estilos = """
        * { box-sizing: border-box; }
        body {
          margin: 0;
          padding: 20px;
          background: #E2E8F0;
          color: #000000;
          font-family: Arial, sans-serif;
          font-size: 13px;
        }
        .recibo {
          max-width: 680px;
          margin: 0 auto;
          padding: 25px 30px;
          background: #FFFFFF;
          border: 1px solid #000000;
        }
        .encabezado {
          display: flex;
          align-items: center;
          gap: 20px;
          padding-bottom: 12px;
          border-bottom: 2px solid #000000;
          margin-bottom: 15px;
        }
        .logo { height: 50px; width: auto; object-fit: contain; }
        h1 { margin: 0; font-size: 20px; font-weight: bold; color: #000000; letter-spacing: 0.5px; }
        .anulada {
          margin: 10px 0;
          padding: 5px;
          text-align: center;
          font-weight: bold;
          color: #EF4444;
          border: 2px solid #EF4444;
        }
        .datos { margin-bottom: 15px; }
        .datos p { margin: 5px 0; }
        .datos-cliente {
          display: flex;
          gap: 30px;
          margin-top: 5px;
        }
        .detalle { width: 100%; border-collapse: collapse; margin-bottom: 15px; }
        .detalle th, .detalle td {
          padding: 7px 10px;
          border: 1px solid #000000;
          font-size: 13px;
        }
        .detalle th {
          background: #FFFFFF;
          font-weight: bold;
          text-align: center;
        }
        .num { text-align: right; white-space: nowrap; }
        .centro { text-align: center; }
        .total { font-weight: bold; font-size: 14px; border-top: 1px solid #000000; }
        .pie-container {
          display: flex;
          justify-content: space-between;
          align-items: flex-end;
          margin-top: 15px;
        }
        .letras { margin: 0; font-weight: bold; font-size: 13px; }
        .firma-sistema { margin: 0; font-size: 12px; font-style: italic; white-space: nowrap; }
        .acciones { margin-top: 25px; text-align: center; }
        .acciones button {
          padding: 8px 20px;
          background: #000000;
          color: #FFFFFF;
          border: none;
          border-radius: 4px;
          font-size: 13px;
          font-weight: bold;
          cursor: pointer;
        }
        .acciones button:hover { background: #333333; }
        @media print {
          body { padding: 0; background: #FFFFFF; }
          .recibo { border: none; padding: 0; max-width: none; }
          .acciones { display: none; }
          @page { margin: 10mm; }
        }
        """;
}