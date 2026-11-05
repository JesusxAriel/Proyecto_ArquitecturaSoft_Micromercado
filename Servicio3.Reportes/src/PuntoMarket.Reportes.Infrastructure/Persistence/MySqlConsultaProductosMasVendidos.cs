using System.Text;
using MySql.Data.MySqlClient;
using PuntoMarket.Reportes.Application.Ports.Consultas;
using PuntoMarket.Reportes.Domain.ValueObjects;
using PuntoMarket.Reportes.Domain.VentasPorCategoria;

namespace PuntoMarket.Reportes.Infrastructure.Persistence;

public class MySqlConsultaProductosMasVendidos : IConsultaProductosMasVendidos
{
    private const string Seleccion = """
        SELECT p.id, p.nombre, SUM(d.cantidad) AS unidades, SUM(d.subtotal) AS monto
        FROM DETALLE_VENTA d
        INNER JOIN VENTA v ON v.id = d.idVenta
        INNER JOIN PRODUCTO p ON p.id = d.idProducto
        WHERE v.estado = 'COMPLETADA'
        """;

    private const string Agrupacion = " GROUP BY p.id, p.nombre ORDER BY monto DESC, p.nombre ASC LIMIT @limite";

    private readonly ConexionMySql _conexion;

    public MySqlConsultaProductosMasVendidos(ConexionMySql conexion)
    {
        _conexion = conexion;
    }

    public async Task<IReadOnlyList<FilaDeProducto>> ConsultarAsync(
        RangoDeFechas rango,
        IReadOnlyList<int> idsDeCategorias,
        int limite,
        CancellationToken cancelacion)
    {
        if (idsDeCategorias.Count == 0)
            return [];

        using var conexion = _conexion.Abrir();
        using var comando = CrearComando(conexion, rango, idsDeCategorias, limite);
        using var lector = await comando.ExecuteReaderAsync(cancelacion);

        var filas = new List<FilaDeProducto>();
        while (await lector.ReadAsync(cancelacion))
            filas.Add(MapearFila(lector));

        return filas;
    }

    private static MySqlCommand CrearComando(
        MySqlConnection conexion,
        RangoDeFechas rango,
        IReadOnlyList<int> idsDeCategorias,
        int limite)
    {
        var comando = conexion.CreateCommand();
        var sql = new StringBuilder(Seleccion);

        FiltrosDeVentasEnSql.AgregarRango(comando, sql, rango);
        FiltrosDeVentasEnSql.AgregarCategorias(comando, sql, idsDeCategorias);
        sql.Append(Agrupacion);
        comando.Parameters.AddWithValue("@limite", limite);

        comando.CommandText = sql.ToString();
        return comando;
    }

    private static FilaDeProducto MapearFila(System.Data.Common.DbDataReader lector) =>
        new(
            lector.GetInt32(0),
            lector.GetString(1),
            Convert.ToInt64(lector.GetValue(2)),
            lector.GetDecimal(3));
}
