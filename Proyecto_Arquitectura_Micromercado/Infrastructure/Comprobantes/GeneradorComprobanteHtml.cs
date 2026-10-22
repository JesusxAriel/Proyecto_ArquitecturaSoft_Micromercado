using System.Globalization;
using System.Net;
using Proyecto_Arquitectura_Micromercado.Application.Comprobantes;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Comprobantes;

public sealed class GeneradorComprobanteHtml : IGeneradorComprobante
{
    private const string RutaDelLogo = "/images/logo.jpg";
    private const int MinimoDeFilas = 10;
    private const string FilaVacia =
        "<tr class=\"vacia\" aria-hidden=\"true\"><td>&nbsp;</td><td></td><td></td><td></td></tr>\n";
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-BO");

    public string GenerarHtml(ComprobanteDto comprobante)
    {
        ArgumentNullException.ThrowIfNull(comprobante);

        return $"""
            <!DOCTYPE html>
            <html lang="es">
            <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <title>Comprobante de venta {Esc(comprobante.NroComprobante)} - PuntoMarket</title>
            <style>
            {Estilos}
            </style>
            </head>
            <body>
            <main class="recibo">
            {Encabezado(comprobante)}
            {AvisoDeAnulada(comprobante)}
            {DatosDelCliente(comprobante)}
            {TablaDeDetalle(comprobante.Lineas)}
            {RecuadroTotal(comprobante)}
            {Pie(comprobante)}
            {Acciones()}
            </main>
            </body>
            </html>
            """;
    }

    private static string Encabezado(ComprobanteDto comprobante) => $"""
        <header class="encabezado">
        <div class="marca-bloque">
        <img class="logo" src="{RutaDelLogo}" alt="PuntoMarket" />
        <div class="titulo">
        <p class="marca">PuntoMarket</p>
        <h1>COMPROBANTE DE VENTA</h1>
        </div>
        </div>
        <div class="folio">
        <p class="folio-etiqueta">N.º</p>
        <p class="folio-numero">{Esc(comprobante.NroComprobante)}</p>
        <p class="folio-fecha">Fecha: {comprobante.FechaVenta.ToString("dd/MM/yyyy", Cultura)}</p>
        </div>
        </header>
        """;

    private static string AvisoDeAnulada(ComprobanteDto comprobante) =>
        comprobante.EstaAnulada ? "<p class=\"anulada\" role=\"alert\">ANULADA</p>" : string.Empty;

    private static string DatosDelCliente(ComprobanteDto comprobante) => $"""
        <section class="cliente" aria-label="Datos del cliente">
        <div class="campo"><span class="etiqueta">CI/NIT</span><span class="valor">{Esc(comprobante.NitCiCliente)}</span></div>
        <div class="campo"><span class="etiqueta">RAZÓN SOCIAL</span><span class="valor">{Esc(comprobante.RazonSocialCliente)}</span></div>
        </section>
        """;

    private static string TablaDeDetalle(IReadOnlyList<LineaComprobanteDto> lineas) => $"""
        <div class="tabla-envoltorio">
        <table class="detalle">
        <thead><tr><th class="cant">CANT.</th><th>ARTÍCULO</th><th class="num">PRECIO</th><th class="num">IMPORTE</th></tr></thead>
        <tbody>
        {string.Concat(lineas.Select(FilaDeLinea))}{FilasVacias(lineas.Count)}</tbody>
        </table>
        </div>
        """;

    private static string FilaDeLinea(LineaComprobanteDto linea) => $"""
        <tr>
        <td class="cant">{linea.Cantidad.ToString(Cultura)}</td>
        <td>{Esc(linea.Descripcion)}</td>
        <td class="num">{Dinero(linea.PrecioUnitario)}</td>
        <td class="num">{Dinero(linea.Importe)}</td>
        </tr>

        """;

    private static string FilasVacias(int filasUsadas) =>
        string.Concat(Enumerable.Repeat(FilaVacia, Math.Max(0, MinimoDeFilas - filasUsadas)));

    private static string RecuadroTotal(ComprobanteDto comprobante) => $"""
        <section class="cierre">
        <div class="recuadro-total">
        <span class="total-etiqueta">TOTAL Bs</span>
        <span class="total-monto">{Dinero(comprobante.Total)}</span>
        </div>
        </section>
        <p class="letras">{Esc(comprobante.TotalEnLetras)}</p>
        """;

    private static string Pie(ComprobanteDto comprobante) => $"""
        <footer class="pie">
        <p class="generado">Generado por {Esc(comprobante.UsuarioGenerador)} el {comprobante.FechaGeneracion.ToString("dd/MM/yyyy HH:mm:ss", Cultura)}</p>
        <p class="gracias">Gracias por su compra</p>
        </footer>
        """;

    private static string Acciones() => """
        <div class="acciones">
        <button type="button" onclick="window.print()">Imprimir Comprobante</button>
        </div>
        """;

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