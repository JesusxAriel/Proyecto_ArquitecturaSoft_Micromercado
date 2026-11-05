using MySql.Data.MySqlClient;
using PuntoMarket.Ventas.Application.Customers;

namespace PuntoMarket.Ventas.Infrastructure.Persistence;

public sealed class MySqlClienteCreacionRepository(MySqlUnidadDeTrabajo unidad) : IClienteCreacionRepository
{
    private const string Sql = """
        INSERT INTO CLIENTE (nitCi, razonSocial, idUsuarioCreacion)
        VALUES (@nitCi, @razonSocial, @idUsuarioCreacion);
        """;

    public async Task<int> CrearAsync(string nitCi, string razonSocial, int idUsuarioCreacion,
        CancellationToken cancellationToken = default)
    {
        await using var alquiler = await unidad.AlquilarAsync(cancellationToken, requiereTransaccion: true);
        await using var comando = new MySqlCommand(Sql, alquiler.Conexion, alquiler.Transaccion);
        comando.Parameters.AddWithValue("@nitCi", nitCi);
        comando.Parameters.AddWithValue("@razonSocial", razonSocial);
        comando.Parameters.AddWithValue("@idUsuarioCreacion", idUsuarioCreacion);
        try
        {
            await comando.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (MySqlException ex) when (ex.Number == 1062 &&
            ex.Message.Contains("UQ_Cliente_nitCi_activo", StringComparison.OrdinalIgnoreCase))
        {
            throw new ClienteDuplicadoException();
        }
        return checked((int)comando.LastInsertedId);
    }
}
