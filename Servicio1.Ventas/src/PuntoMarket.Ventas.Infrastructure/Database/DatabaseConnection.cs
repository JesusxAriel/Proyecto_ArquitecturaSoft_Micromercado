using MySql.Data.MySqlClient;

namespace PuntoMarket.Ventas.Infrastructure.Database;

/// <summary>Fábrica inmutable de conexiones nuevas; nunca conserva una conexión global abierta.</summary>
public sealed class DatabaseConnection
{
    private readonly string connectionString;

    public DatabaseConnection(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        var builder = new MySqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.Database))
            throw new ArgumentException("Debe indicar una base de datos.", nameof(connectionString));
        builder.AllowUserVariables = true;
        this.connectionString = builder.ConnectionString;
    }

    public MySqlConnection CreateConnection() => new(connectionString);
}
