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
            {SelloDeAnulada(comprobante)}
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

    private static string SelloDeAnulada(ComprobanteDto comprobante) =>
        comprobante.EstaAnulada ? "<div class=\"sello\" aria-hidden=\"true\">ANULADA</div>" : string.Empty;

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
          padding: 24px 12px;
          background: #E2E8F0;
          color: #1E293B;
          font-family: "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
          font-size: 14px;
          line-height: 1.4;
        }
        .recibo {
          position: relative;
          max-width: 800px;
          margin: 0 auto;
          padding: 24px 28px;
          background: #FFFFFF;
          border-top: 6px solid #F59E0B;
          border-radius: 4px;
          box-shadow: 0 4px 18px rgba(30, 41, 59, 0.15);
        }
        .encabezado {
          display: flex;
          flex-wrap: wrap;
          align-items: stretch;
          justify-content: space-between;
          gap: 16px;
          padding-bottom: 16px;
          margin-bottom: 16px;
          border-bottom: 2px solid #1E293B;
        }
        .marca-bloque { display: flex; align-items: center; gap: 16px; }
        .logo { display: block; height: 64px; width: auto; border-radius: 4px; }
        .marca {
          margin: 0;
          font-size: 13px;
          font-weight: 700;
          letter-spacing: 1px;
          text-transform: uppercase;
          color: #B45309;
        }
        h1 { margin: 2px 0 0; font-size: 22px; font-weight: 800; letter-spacing: 0.5px; color: #1E293B; }
        .folio {
          min-width: 190px;
          padding: 8px 14px;
          text-align: center;
          border: 2px solid #1E293B;
          border-radius: 6px;
        }
        .folio p { margin: 0; }
        .folio-etiqueta { font-size: 11px; font-weight: 700; letter-spacing: 2px; color: #334155; }
        .folio-numero { font-size: 20px; font-weight: 800; color: #1E293B; }
        .folio-fecha { margin-top: 4px; font-size: 12px; color: #334155; }
        .anulada {
          margin: 0 0 16px;
          padding: 6px;
          text-align: center;
          font-weight: 800;
          letter-spacing: 2px;
          color: #B91C1C;
          border: 2px solid #B91C1C;
          border-radius: 4px;
        }
        .sello {
          position: absolute;
          top: 50%;
          left: 50%;
          z-index: 2;
          padding: 0 28px;
          font-size: clamp(48px, 12vw, 108px);
          font-weight: 900;
          letter-spacing: 6px;
          white-space: nowrap;
          color: rgba(185, 28, 28, 0.22);
          border: 8px solid rgba(185, 28, 28, 0.22);
          border-radius: 14px;
          transform: translate(-50%, -50%) rotate(-28deg);
          pointer-events: none;
          user-select: none;
        }
        .cliente {
          display: grid;
          grid-template-columns: 1fr 2fr;
          gap: 12px 20px;
          padding: 12px 14px;
          margin-bottom: 16px;
          border: 2px solid #1E293B;
          border-radius: 6px;
        }
        .campo {
          display: flex;
          align-items: baseline;
          gap: 8px;
          min-width: 0;
          padding-bottom: 2px;
          border-bottom: 1px solid #94A3B8;
        }
        .etiqueta { font-size: 11px; font-weight: 800; letter-spacing: 1px; white-space: nowrap; color: #334155; }
        .valor { flex: 1; min-width: 0; font-weight: 600; overflow-wrap: anywhere; }
        .tabla-envoltorio { margin-bottom: 16px; overflow-x: auto; }
        .detalle { width: 100%; min-width: 520px; border-collapse: collapse; border: 2px solid #1E293B; }
        .detalle th {
          padding: 8px 10px;
          font-size: 12px;
          letter-spacing: 1px;
          text-align: left;
          color: #FFFFFF;
          background: #1E293B;
          border: 1px solid #1E293B;
        }
        .detalle td { height: 30px; padding: 5px 10px; border: 1px solid #CBD5E1; }
        .detalle tbody tr:nth-child(even) { background: #F1F5F9; }
        .detalle .cant { width: 72px; text-align: center; }
        .detalle .num { width: 120px; text-align: right; white-space: nowrap; }
        .cierre { display: flex; justify-content: flex-end; }
        .recuadro-total {
          display: flex;
          align-items: center;
          gap: 16px;
          padding: 8px 18px;
          background: #FFFBEB;
          border: 3px solid #D97706;
          border-radius: 6px;
        }
        .total-etiqueta { font-size: 14px; font-weight: 800; letter-spacing: 1px; }
        .total-monto { font-size: 28px; font-weight: 800; white-space: nowrap; }
        .letras {
          margin: 10px 0 16px;
          padding: 8px 14px;
          font-size: 13px;
          font-weight: 700;
          color: #FFFFFF;
          background: #1E293B;
          border-radius: 4px;
        }
        .pie {
          display: flex;
          flex-wrap: wrap;
          justify-content: space-between;
          gap: 8px;
          padding-top: 10px;
          font-size: 12px;
          color: #475569;
          border-top: 1px solid #CBD5E1;
        }
        .pie p { margin: 0; }
        .gracias { font-weight: 700; color: #1E293B; }
        .acciones { margin-top: 24px; text-align: center; }
        .acciones button {
          padding: 9px 22px;
          font-size: 13px;
          font-weight: 700;
          color: #FFFFFF;
          background: #1E293B;
          border: none;
          border-radius: 4px;
          cursor: pointer;
        }
        .acciones button:hover { background: #334155; }
        @media (max-width: 600px) {
          .recibo { padding: 16px; }
          .encabezado { flex-direction: column; }
          .marca-bloque { flex-wrap: wrap; }
          .logo { height: 48px; }
          .folio { width: 100%; }
          .cliente { grid-template-columns: 1fr; }
          .recuadro-total { width: 100%; justify-content: space-between; }
          h1 { font-size: 18px; }
        }
        @page { size: letter; margin: 10mm; }
        @media print {
          * { -webkit-print-color-adjust: exact; print-color-adjust: exact; }
          body { padding: 0; background: #FFFFFF; font-size: 12px; }
          .recibo { max-width: none; padding: 0; border-radius: 0; box-shadow: none; }
          .tabla-envoltorio { overflow: visible; }
          .detalle { min-width: 0; }
          .detalle td { height: 8mm; padding-top: 2px; padding-bottom: 2px; }
          .detalle thead { display: table-header-group; }
          .detalle tr, .encabezado, .cliente, .cierre, .letras, .pie { break-inside: avoid; }
          .acciones { display: none; }
        }
        """;
}