using MySql.Data.MySqlClient;

namespace PuntoMarket.Reportes.Infrastructure.Persistence;

public class ConexionMySql
{
    private readonly string _cadenaDeConexion;

    public ConexionMySql(string cadenaDeConexion)
    {
        _cadenaDeConexion = cadenaDeConexion;
    }

    public MySqlConnection Abrir()
    {
        var conexion = new MySqlConnection(_cadenaDeConexion);
        conexion.Open();
        return conexion;
    }
}
