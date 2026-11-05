using System.Text;
using MySql.Data.MySqlClient;
using PuntoMarket.Reportes.Application.Ports.Consultas;
using PuntoMarket.Reportes.Domain.ValueObjects;
using PuntoMarket.Reportes.Domain.VentasPorCategoria;

namespace PuntoMarket.Reportes.Infrastructure.Persistence;

public class MySqlConsultaVentasPorCategoria : IConsultaVentasPorCategoria
{
    private const string Seleccion = """
        SELECT c.id, c.nombre, SUM(d.cantidad) AS unidades, SUM(d.subtotal) AS monto,
               SUM(SUM(d.cantidad)) OVER () AS totalUnidades,
               SUM(SUM(d.subtotal)) OVER () AS totalMonto
        FROM DETALLE_VENTA d
        INNER JOIN VENTA v ON v.id = d.idVenta
        INNER JOIN PRODUCTO p ON p.id = d.idProducto
        INNER JOIN CATEGORIAS c ON c.id = p.idCategoria
        WHERE v.estado = 'COMPLETADA'
        """;

    private const string Agrupacion = " GROUP BY c.id, c.nombre ORDER BY monto DESC, c.nombre ASC";

    private readonly ConexionMySql _conexion;

    public MySqlConsultaVentasPorCategoria(ConexionMySql conexion)
    {
        _conexion = conexion;
    }

    public async Task<ResultadoVentasPorCategoria> ConsultarAsync(
        RangoDeFechas rango,
        int? idCategoria,
        int? top,
        CancellationToken cancelacion)
    {
        using var conexion = _conexion.Abrir();
        using var comando = CrearComando(conexion, rango, idCategoria, top);
        using var lector = await comando.ExecuteReaderAsync(cancelacion);

        var filas = new List<FilaDeCategoria>();
        long unidades = 0;
        decimal monto = 0m;
        while (await lector.ReadAsync(cancelacion))
        {
            if (filas.Count == 0)
            {
                unidades = Convert.ToInt64(lector.GetValue(4));
                monto = lector.GetDecimal(5);
            }
            filas.Add(MapearFila(lector));
        }
        return new(filas, new TotalesPorCategoria(unidades, monto, filas.FirstOrDefault()));
    }

    private static MySqlCommand CrearComando(MySqlConnection conexion, RangoDeFechas rango, int? idCategoria, int? top)
    {
        var comando = conexion.CreateCommand();
        var sql = new StringBuilder(Seleccion);

        AgregarFiltroDeFechas(comando, sql, rango);
        AgregarFiltroDeCategoria(comando, sql, idCategoria);
        sql.Append(Agrupacion);
        AgregarLimite(comando, sql, top);

        comando.CommandText = sql.ToString();
        return comando;
    }

    private static void AgregarFiltroDeFechas(MySqlCommand comando, StringBuilder sql, RangoDeFechas rango)
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

    private static void AgregarFiltroDeCategoria(MySqlCommand comando, StringBuilder sql, int? idCategoria)
    {
        if (idCategoria is null)
            return;

        sql.Append(" AND c.id = @idCategoria");
        comando.Parameters.AddWithValue("@idCategoria", idCategoria.Value);
    }

    private static void AgregarLimite(MySqlCommand comando, StringBuilder sql, int? top)
    {
        if (top is null)
            return;

        sql.Append(" LIMIT @top");
        comando.Parameters.AddWithValue("@top", top.Value);
    }

    private static FilaDeCategoria MapearFila(System.Data.Common.DbDataReader lector) =>
        new(
            lector.GetInt32(0),
            lector.GetString(1),
            Convert.ToInt64(lector.GetValue(2)),
            lector.GetDecimal(3));
}
