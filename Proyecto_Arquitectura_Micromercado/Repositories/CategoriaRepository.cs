using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Models;

namespace Proyecto_Arquitectura_Micromercado.Repositories
{
    public class CategoriaRepository : ICategoriaRepository
    {
        private readonly string connectionString;

        public CategoriaRepository(IConfiguration configuration)
        {
            connectionString =
                configuration.GetConnectionString("MySqlConnection")!;
        }

        public List<Categoria> ObtenerActivas()
        {
            const string query = @"
                SELECT id,
                       nombre,
                       descripcion,
                       codigo,
                       pasilloUbicacion,
                       idUsuarioAdmin,
                       fechaCreacion,
                       fechaActualizacion
                FROM CATEGORIAS
                WHERE estaActivo = 1
                ORDER BY nombre ASC;";

            List<Categoria> categorias = new List<Categoria>();

            using MySqlConnection connection =
                new MySqlConnection(connectionString);

            using MySqlCommand command =
                new MySqlCommand(query, connection);

            connection.Open();

            using MySqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                Categoria categoria = new Categoria
                {
                    Id = reader.GetInt32("id"),

                    Nombre = reader.GetString("nombre"),

                    Descripcion =
                        reader.IsDBNull(reader.GetOrdinal("descripcion"))
                            ? null
                            : reader.GetString("descripcion"),

                    Codigo = reader.GetString("codigo"),

                    PasilloUbicacion =
                        reader.IsDBNull(reader.GetOrdinal("pasilloUbicacion"))
                            ? null
                            : reader.GetString("pasilloUbicacion"),

                    IdUsuarioAdmin =
                        reader.GetInt32("idUsuarioAdmin"),

                    FechaCreacion =
                        reader.GetDateTime("fechaCreacion"),

                    FechaActualizacion =
                        reader.IsDBNull(reader.GetOrdinal("fechaActualizacion"))
                            ? null
                            : reader.GetDateTime("fechaActualizacion")
                };

                categorias.Add(categoria);
            }

            return categorias;
        }

        public Categoria? ObtenerPorId(int id)
        {
            const string query = @"
                SELECT id,
                       nombre,
                       descripcion,
                       codigo,
                       pasilloUbicacion,
                       idUsuarioAdmin,
                       fechaCreacion,
                       fechaActualizacion
                FROM CATEGORIAS
                WHERE id = @id
                  AND estaActivo = 1;";

            using MySqlConnection connection =
                new MySqlConnection(connectionString);

            using MySqlCommand command =
                new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@id", id);

            connection.Open();

            using MySqlDataReader reader = command.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            return new Categoria
            {
                Id = reader.GetInt32("id"),

                Nombre = reader.GetString("nombre"),

                Descripcion =
                    reader.IsDBNull(reader.GetOrdinal("descripcion"))
                        ? null
                        : reader.GetString("descripcion"),

                Codigo = reader.GetString("codigo"),

                PasilloUbicacion =
                    reader.IsDBNull(reader.GetOrdinal("pasilloUbicacion"))
                        ? null
                        : reader.GetString("pasilloUbicacion"),

                IdUsuarioAdmin =
                    reader.GetInt32("idUsuarioAdmin"),

                FechaCreacion =
                    reader.GetDateTime("fechaCreacion"),

                FechaActualizacion =
                    reader.IsDBNull(reader.GetOrdinal("fechaActualizacion"))
                        ? null
                        : reader.GetDateTime("fechaActualizacion")
            };
        }

        public bool Actualizar(Categoria categoria)
        {
            const string query = @"
                UPDATE CATEGORIAS
                SET nombre = @nombre,
                    descripcion = @descripcion,
                    codigo = @codigo,
                    pasilloUbicacion = @pasilloUbicacion,
                    idUsuarioAdmin = @idUsuarioAdmin
                WHERE id = @id
                  AND estaActivo = 1;";

            using MySqlConnection connection =
                new MySqlConnection(connectionString);

            using MySqlCommand command =
                new MySqlCommand(query, connection);

            command.Parameters.AddWithValue(
                "@id",
                categoria.Id);

            command.Parameters.AddWithValue(
                "@nombre",
                categoria.Nombre);

            command.Parameters.AddWithValue(
                "@descripcion",
                (object?)categoria.Descripcion ?? DBNull.Value);

            command.Parameters.AddWithValue(
                "@codigo",
                categoria.Codigo);

            command.Parameters.AddWithValue(
                "@pasilloUbicacion",
                (object?)categoria.PasilloUbicacion ?? DBNull.Value);

            command.Parameters.AddWithValue(
                "@idUsuarioAdmin",
                categoria.IdUsuarioAdmin);

            connection.Open();

            int filasAfectadas = command.ExecuteNonQuery();

            return filasAfectadas > 0;
        }

        public bool Desactivar(int id, int idUsuarioAdmin)
        {
            const string query = @"
                UPDATE CATEGORIAS
                SET estaActivo = 0,
                    idUsuarioAdmin = @idUsuarioAdmin
                WHERE id = @id
                  AND estaActivo = 1;";

            using MySqlConnection connection =
                new MySqlConnection(connectionString);

            using MySqlCommand command =
                new MySqlCommand(query, connection);

            command.Parameters.AddWithValue(
                "@id",
                id);

            command.Parameters.AddWithValue(
                "@idUsuarioAdmin",
                idUsuarioAdmin);

            connection.Open();

            int filasAfectadas = command.ExecuteNonQuery();

            return filasAfectadas > 0;
        }
    }
}