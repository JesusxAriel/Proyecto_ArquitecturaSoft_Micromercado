using System.Text;
using MySql.Data.MySqlClient;
using PuntoMarket.Reportes.Domain.ValueObjects;

namespace PuntoMarket.Reportes.Infrastructure.Persistence;

internal static class FiltrosDeVentasEnSql
{
    public static void AgregarRango(MySqlCommand comando, StringBuilder sql, RangoDeFechas rango)
    {
        if (rango.Desde is { } desde)
        {
            sql.Append(" AND v.fechaVenta >= @desde");
            comando.Parameters.AddWithValue("@desde", desde.ToDateTime(TimeOnly.MinValue));
        }

        if (rango.Hasta is { } hasta)
        {
            sql.Append(" AND v.fechaVenta < @hastaExclusivo");
            comando.Parameters.AddWithValue("@hastaExclusivo", hasta.AddDays(1).ToDateTime(TimeOnly.MinValue));
        }
    }

    public static void AgregarCategorias(MySqlCommand comando, StringBuilder sql, IReadOnlyList<int> idsDeCategorias)
    {
        var nombres = new List<string>();
        for (var indice = 0; indice < idsDeCategorias.Count; indice++)
        {
            var nombre = $"@categoria{indice}";
            nombres.Add(nombre);
            comando.Parameters.AddWithValue(nombre, idsDeCategorias[indice]);
        }

        sql.Append(" AND p.idCategoria IN (").Append(string.Join(", ", nombres)).Append(')');
    }
}
