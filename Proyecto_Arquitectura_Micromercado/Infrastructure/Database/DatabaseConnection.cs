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
            // Allow User Variables permite fijar @motivo_cambio_precio antes de actualizar
            // un precio (lo lee el trigger del historial). Se activa aqui para que no
            // dependa de que cada persona lo escriba en su appsettings.json.
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

        // Propiedad para acceso rápido en los repositorios una vez inicializado
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
