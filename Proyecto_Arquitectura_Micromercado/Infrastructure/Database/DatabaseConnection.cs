using MySql.Data.MySqlClient;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Database
{
    public class DatabaseConnection
    {
        private static DatabaseConnection? _instance;
        private static readonly object _lock = new object();
        
        public string ConnectionString { get; }

        private DatabaseConnection(string connectionString)
        {
            ConnectionString = new MySqlConnectionStringBuilder(connectionString)
            {
                AllowUserVariables = true
            }.ConnectionString;
        }

        public static DatabaseConnection GetInstance(string connectionString)
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new DatabaseConnection(connectionString);
                    }
                }
            }
            return _instance;
        }

        public static DatabaseConnection Instance 
        { 
            get 
            {
                if (_instance == null)
                    throw new InvalidOperationException("El Singleton de base de datos no ha sido inicializado. Llama a GetInstance primero.");
                return _instance; 
            } 
        }

        public MySqlConnection CreateConnection()
        {
            return new MySqlConnection(ConnectionString);
        }
    }
}
