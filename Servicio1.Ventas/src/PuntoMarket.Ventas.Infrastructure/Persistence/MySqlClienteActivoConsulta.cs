using MySql.Data.MySqlClient;
using PuntoMarket.Ventas.Application.Customers;

namespace PuntoMarket.Ventas.Infrastructure.Persistence;

public sealed class MySqlClienteActivoConsulta(MySqlUnidadDeTrabajo unidadDeTrabajo) : IClienteActivoConsulta
{
    public async Task<bool> ExisteActivoAsync(int idCliente, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM CLIENTE WHERE estaActivo = 1 AND id = @idCliente
            );
            """;
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
        await using var comando = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        comando.Parameters.AddWithValue("@idCliente", idCliente);
        return Convert.ToInt32(await comando.ExecuteScalarAsync(cancellationToken)) != 0;
    }
}
