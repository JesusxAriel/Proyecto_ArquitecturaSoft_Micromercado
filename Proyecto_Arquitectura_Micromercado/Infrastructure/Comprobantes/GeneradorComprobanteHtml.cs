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
        html.AppendLine($"<img class=\"logo\" src=\"{RutaDelLogo}\" alt=\"PuntoMarket\" />");
        html.AppendLine("<h1>COMPROBANTE DE VENTA</h1>");
        html.AppendLine("</header>");

        if (comprobante.EstaAnulada)
        {
            html.AppendLine("<p class=\"anulada\" role=\"alert\">ANULADA</p>");
        }

        html.AppendLine("<section class=\"datos\">");
        html.AppendLine($"<p><strong>Comprobante:</strong> {Esc(comprobante.NroComprobante)}</p>");
        html.AppendLine($"<p><strong>Fecha de venta:</strong> {comprobante.FechaVenta.ToString("dd/MM/yyyy HH:mm", Cultura)}</p>");
        html.AppendLine($"<p><strong>CI/NIT:</strong> {Esc(comprobante.NitCiCliente)}</p>");
        html.AppendLine($"<p><strong>Razón social:</strong> {Esc(comprobante.RazonSocialCliente)}</p>");
        html.AppendLine($"<p><strong>Estado:</strong> {Esc(comprobante.Estado)}</p>");
        html.AppendLine("</section>");

        html.AppendLine("<table class=\"detalle\">");
        html.AppendLine("<thead><tr>");
        html.AppendLine("<th>Cantidad</th><th>Descripción</th><th class=\"num\">Precio unitario Bs.</th><th class=\"num\">Importe Bs.</th>");
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
        html.AppendLine("<tfoot><tr>");
        html.AppendLine($"<td colspan=\"3\" class=\"num total\">Total Bs.:</td><td class=\"num total\">{Dinero(comprobante.Total)}</td>");
        html.AppendLine("</tr></tfoot>");
        html.AppendLine("</table>");
        html.AppendLine($"<p class=\"letras\">{Esc(comprobante.TotalEnLetras)}</p>");

        html.AppendLine("<footer class=\"pie\">");
        html.AppendLine($"<p>Generado el {comprobante.FechaGeneracion.ToString("dd/MM/yyyy", Cultura)} a las {comprobante.FechaGeneracion.ToString("HH:mm:ss", Cultura)} por {Esc(comprobante.UsuarioGenerador)}</p>");
        html.AppendLine("</footer>");

        html.AppendLine("<div class=\"acciones\">");
        html.AppendLine("<button type=\"button\" onclick=\"window.print()\">Imprimir</button>");
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
          padding: 24px 12px;
          background: #F8FAFC;
          color: #1E293B;
          font-family: "Segoe UI", Arial, sans-serif;
          font-size: 14px;
        }
        .recibo {
          max-width: 440px;
          margin: 0 auto;
          padding: 20px;
          background: #FFFFFF;
          border: 1px solid #E2E8F0;
          border-top: 6px solid #F59E0B;
          border-radius: 6px;
        }
        .encabezado {
          display: flex;
          align-items: center;
          gap: 14px;
          padding: 10px 12px;
          background: #1E293B;
          border-radius: 4px;
          -webkit-print-color-adjust: exact;
          print-color-adjust: exact;
        }
        .logo { height: 48px; width: auto; border-radius: 3px; }
        h1 { margin: 0; font-size: 18px; color: #FFFFFF; letter-spacing: 0.5px; }
        .anulada {
          margin: 14px 0 0;
          padding: 6px;
          text-align: center;
          font-weight: 700;
          letter-spacing: 3px;
          color: #EF4444;
          border: 2px solid #EF4444;
          border-radius: 4px;
        }
        .datos { margin: 16px 0; }
        .datos p { margin: 3px 0; }
        .detalle { width: 100%; border-collapse: collapse; }
        .detalle th {
          padding: 6px;
          background: #334155;
          color: #FFFFFF;
          font-size: 12px;
          text-align: left;
          -webkit-print-color-adjust: exact;
          print-color-adjust: exact;
        }
        .detalle td { padding: 6px; border-bottom: 1px solid #E2E8F0; }
        .num { text-align: right; white-space: nowrap; }
        .centro { text-align: center; }
        .detalle th.num { text-align: right; }
        .total { padding-top: 10px; font-size: 16px; font-weight: 700; border-bottom: 0 !important; }
        .total + .total { color: #B45309; }
        .letras { margin: 10px 0 0; font-weight: 600; }
        .pie { margin-top: 18px; text-align: right; font-size: 12px; font-style: italic; color: #475569; }
        .pie p { margin: 0; }
        .acciones { margin-top: 18px; text-align: center; }
        .acciones button {
          padding: 8px 28px;
          background: #F59E0B;
          border: 0;
          border-radius: 6px;
          color: #1E293B;
          font-size: 14px;
          font-weight: 600;
          cursor: pointer;
        }
        .acciones button:hover { background: #D97706; }
        @media print {
          body { padding: 0; background: #FFFFFF; }
          .recibo { max-width: none; border: 0; border-radius: 0; padding: 0; }
          .acciones { display: none; }
          @page { margin: 10mm; }
        }
        """;
}
