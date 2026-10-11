using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Customers;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.Errores;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

public sealed class MySqlClienteConsultaRepository(
    MySqlUnidadDeTrabajo unidadDeTrabajo) : IClienteConsultaRepository
{
    public Task<IReadOnlyList<ClienteConsultaDto>> BuscarAsync(
        string? termino,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(termino))
        {
            return Task.FromResult<IReadOnlyList<ClienteConsultaDto>>(
                Array.Empty<ClienteConsultaDto>());
        }

        const string sql = """
            SELECT id, nitCi, razonSocial
            FROM CLIENTE
            WHERE estaActivo = 1
              AND (nitCi LIKE @patron OR razonSocial LIKE @patron)
            ORDER BY razonSocial, id
            LIMIT 20;
            """;

        return ErroresDePersistencia.TraducirAsync<IReadOnlyList<ClienteConsultaDto>>(async () =>
        {
            await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
            await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
            command.Parameters.AddWithValue("@patron", SqlLike.ContainsPattern(termino));
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var clientes = new List<ClienteConsultaDto>();
            while (await reader.ReadAsync(cancellationToken))
            {
                clientes.Add(new ClienteConsultaDto(
                    reader.GetInt32(reader.GetOrdinal("id")),
                    reader.GetString(reader.GetOrdinal("nitCi")),
                    reader.GetString(reader.GetOrdinal("razonSocial"))));
            }

            return clientes;
        });
    }
}
