using MySql.Data.MySqlClient;

namespace PuntoMarket.Usuarios.Infrastructure.Persistence;

public sealed class ConexionMySql
{
    private readonly string cadenaConexion;

    public ConexionMySql(string cadenaConexion)
    {
        this.cadenaConexion = new MySqlConnectionStringBuilder(cadenaConexion)
        {
            AllowUserVariables = true
        }.ConnectionString;
    }

    public MySqlConnection CrearConexion() => new(cadenaConexion);
}
