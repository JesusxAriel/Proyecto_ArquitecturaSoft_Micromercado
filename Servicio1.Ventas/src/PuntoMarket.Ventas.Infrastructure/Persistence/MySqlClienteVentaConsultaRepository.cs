using MySql.Data.MySqlClient;
using PuntoMarket.Ventas.Application.Customers;

namespace PuntoMarket.Ventas.Infrastructure.Persistence;

public sealed class MySqlClienteVentaConsultaRepository(MySqlUnidadDeTrabajo unidad) : IClienteVentaConsultaRepository
{
    internal const string BusquedaSql = """
        SELECT id,nitCi,razonSocial FROM CLIENTE
        WHERE estaActivo=1 AND (nitCi LIKE @patron ESCAPE '!' OR razonSocial LIKE @patron ESCAPE '!')
        ORDER BY razonSocial,id LIMIT @limite;
        """;

    public async Task<IReadOnlyList<ClienteConsultaDto>> BuscarAsync(string termino, int limite, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limite, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limite, 20);
        await using var alquiler = await unidad.AlquilarAsync(cancellationToken);
        await using var comando = new MySqlCommand(BusquedaSql, alquiler.Conexion, alquiler.Transaccion);
        comando.Parameters.AddWithValue("@patron", PatronBusqueda.Contiene(termino));
        comando.Parameters.AddWithValue("@limite", limite);
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        var clientes = new List<ClienteConsultaDto>();
        while (await lector.ReadAsync(cancellationToken)) clientes.Add(new(
            lector.GetInt32(lector.GetOrdinal("id")), lector.GetString(lector.GetOrdinal("nitCi")), lector.GetString(lector.GetOrdinal("razonSocial"))));
        return clientes;
    }
}
