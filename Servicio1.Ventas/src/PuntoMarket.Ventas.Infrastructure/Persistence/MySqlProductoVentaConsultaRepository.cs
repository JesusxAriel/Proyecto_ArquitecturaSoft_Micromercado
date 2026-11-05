using MySql.Data.MySqlClient;
using PuntoMarket.Ventas.Application.Products;

namespace PuntoMarket.Ventas.Infrastructure.Persistence;

public sealed class MySqlProductoVentaConsultaRepository(MySqlUnidadDeTrabajo unidad) : IProductoVentaConsultaRepository
{
    internal const string Seleccion = """
        SELECT p.id, p.nombre, e.nombre AS presentacion, p.precioVenta,
            COALESCE((SELECT SUM(l.cantidadDisponible) FROM LOTE l
                WHERE l.idProducto=p.id AND l.estaActivo=1 AND l.cantidadDisponible>0
                AND (l.fechaVencimiento IS NULL OR l.fechaVencimiento>=@hoy)),0) AS stockDisponible
        FROM PRODUCTO p INNER JOIN EMPAQUE e ON e.id=p.idEmpaque
        WHERE p.estaActivo=1
        """;
    internal const string BusquedaSql = Seleccion + " AND (p.nombre LIKE @patron ESCAPE '!' OR e.nombre LIKE @patron ESCAPE '!') ORDER BY p.nombre,p.id LIMIT @limite;";
    internal const string DetalleSql = Seleccion + " AND p.id=@id;";

    public async Task<ResultadoBusquedaParaVenta> BuscarAsync(string termino, int limite, DateOnly hoy, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limite, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limite, 10);
        await using var alquiler = await unidad.AlquilarAsync(cancellationToken);
        await using var comando = new MySqlCommand(BusquedaSql, alquiler.Conexion, alquiler.Transaccion);
        comando.Parameters.AddWithValue("@hoy", hoy.ToDateTime(TimeOnly.MinValue));
        comando.Parameters.AddWithValue("@patron", PatronBusqueda.Contiene(termino));
        comando.Parameters.AddWithValue("@limite", limite + 1);
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        var productos = new List<ProductoParaVenta>();
        while (await lector.ReadAsync(cancellationToken)) productos.Add(Leer(lector));
        return new ResultadoBusquedaParaVenta(productos.Take(limite).ToArray(), productos.Count > limite);
    }

    public async Task<ProductoParaVenta?> ObtenerAsync(int id, DateOnly hoy, CancellationToken cancellationToken = default)
    {
        await using var alquiler = await unidad.AlquilarAsync(cancellationToken);
        await using var comando = new MySqlCommand(DetalleSql, alquiler.Conexion, alquiler.Transaccion);
        comando.Parameters.AddWithValue("@hoy", hoy.ToDateTime(TimeOnly.MinValue));
        comando.Parameters.AddWithValue("@id", id);
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        return await lector.ReadAsync(cancellationToken) ? Leer(lector) : null;
    }

    private static ProductoParaVenta Leer(System.Data.Common.DbDataReader lector) => new(
        lector.GetInt32(lector.GetOrdinal("id")), lector.GetString(lector.GetOrdinal("nombre")),
        lector.GetString(lector.GetOrdinal("presentacion")), lector.GetDecimal(lector.GetOrdinal("precioVenta")),
        checked(Convert.ToInt32(lector.GetValue(lector.GetOrdinal("stockDisponible")))));
}

internal static class PatronBusqueda
{
    // Escape explícito independiente del modo NO_BACKSLASH_ESCAPES de MySQL.
    internal static string Contiene(string termino) => "%" + termino.Replace("!", "!!").Replace("%", "!%").Replace("_", "!_") + "%";
}
