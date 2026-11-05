using System.Text;
using MySql.Data.MySqlClient;
using PuntoMarket.Reportes.Application.Ports.Consultas;
using PuntoMarket.Reportes.Domain.ValueObjects;

namespace PuntoMarket.Reportes.Infrastructure.Persistence;

public class MySqlConsultaTendenciaDeVentas : IConsultaTendenciaDeVentas
{
    private const string Seleccion = """
        SELECT DATE(v.fechaVenta) AS dia, SUM(d.subtotal) AS monto
        FROM DETALLE_VENTA d
        INNER JOIN VENTA v ON v.id = d.idVenta
        INNER JOIN PRODUCTO p ON p.id = d.idProducto
        WHERE v.estado = 'COMPLETADA'
        """;

    private const string Agrupacion = " GROUP BY DATE(v.fechaVenta) ORDER BY dia ASC";

    private readonly ConexionMySql _conexion;

    public MySqlConsultaTendenciaDeVentas(ConexionMySql conexion)
    {
        _conexion = conexion;
    }

    public async Task<IReadOnlyList<PuntoDeTendencia>> ConsultarAsync(
        RangoDeFechas rango,
        IReadOnlyList<int> idsDeCategorias,
        CancellationToken cancelacion)
    {
        if (idsDeCategorias.Count == 0)
            return [];

        using var conexion = _conexion.Abrir();
        using var comando = CrearComando(conexion, rango, idsDeCategorias);
        using var lector = await comando.ExecuteReaderAsync(cancelacion);

        var puntos = new List<PuntoDeTendencia>();
        while (await lector.ReadAsync(cancelacion))
            puntos.Add(new PuntoDeTendencia(DateOnly.FromDateTime(lector.GetDateTime(0)), lector.GetDecimal(1)));

        return puntos;
    }

    private static MySqlCommand CrearComando(
        MySqlConnection conexion,
        RangoDeFechas rango,
        IReadOnlyList<int> idsDeCategorias)
    {
        var comando = conexion.CreateCommand();
        var sql = new StringBuilder(Seleccion);

        FiltrosDeVentasEnSql.AgregarRango(comando, sql, rango);
        FiltrosDeVentasEnSql.AgregarCategorias(comando, sql, idsDeCategorias);
        sql.Append(Agrupacion);

        comando.CommandText = sql.ToString();
        return comando;
    }
}
