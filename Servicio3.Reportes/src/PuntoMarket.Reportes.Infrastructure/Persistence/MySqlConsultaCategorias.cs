using PuntoMarket.Reportes.Application.Ports.Consultas;

namespace PuntoMarket.Reportes.Infrastructure.Persistence;

public class MySqlConsultaCategorias : IConsultaCategorias
{
    private const string Sql = "SELECT id, nombre FROM CATEGORIAS WHERE estaActivo = 1 ORDER BY nombre ASC";

    private readonly ConexionMySql _conexion;

    public MySqlConsultaCategorias(ConexionMySql conexion)
    {
        _conexion = conexion;
    }

    public async Task<IReadOnlyList<CategoriaDisponible>> ListarAsync(CancellationToken cancelacion)
    {
        using var conexion = _conexion.Abrir();
        using var comando = conexion.CreateCommand();
        comando.CommandText = Sql;
        using var lector = await comando.ExecuteReaderAsync(cancelacion);

        var categorias = new List<CategoriaDisponible>();
        while (await lector.ReadAsync(cancelacion))
            categorias.Add(new CategoriaDisponible(lector.GetInt32(0), lector.GetString(1)));
        return categorias;
    }
}
