using System.Globalization;
using PuntoMarket.Reportes.Domain.ValueObjects;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;

public static class FormatosDeTexto
{
    public const string SinResultados = "Sin resultados para los filtros seleccionados";

    private static readonly CultureInfo Cultura = ObtenerCultura();

    public static string Moneda(decimal monto) => monto.ToString("N2", Cultura);

    public static string Entero(long valor) => valor.ToString("N0", Cultura);

    public static string Bolivianos(decimal monto) => $"Bs {Moneda(monto)}";

    public static string Porcentaje(decimal monto, decimal total) =>
        total == 0 ? "0,0 %" : (monto / total).ToString("P1", Cultura);

    public static string Abreviar(string texto, int maximo) =>
        texto.Length <= maximo ? texto : texto[..(maximo - 1)].TrimEnd() + "…";

    public static string Fecha(DateOnly fecha) => fecha.ToString("dd/MM/yyyy", Cultura);

    public static string FechaYHora(DateTime fecha) => fecha.ToString("dd/MM/yyyy HH:mm", Cultura);

    public static string ReporteGenerado(string usuario, DateTime fecha) =>
        $"Reporte generado: {usuario} - {fecha.ToString("dd/MM/yyyy HH:mm:ss", Cultura)}";

    public static string Periodo(RangoDeFechas rango) =>
        (rango.Desde, rango.Hasta) switch
        {
            (null, null) => "Todas las fechas",
            ({ } desde, null) => $"Desde el {Fecha(desde)}",
            (null, { } hasta) => $"Hasta el {Fecha(hasta)}",
            ({ } desde, { } hasta) => $"Del {Fecha(desde)} al {Fecha(hasta)}"
        };

    private static CultureInfo ObtenerCultura()
    {
        try
        {
            return CultureInfo.GetCultureInfo("es-BO");
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    }
}
