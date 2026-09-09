using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Domain.Categories;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence
{
    public class MySqlCategoryRepository : ICategoryRepository
    {
        private readonly string connectionString;

        public MySqlCategoryRepository(IConfiguration configuration)
        {
            connectionString =
                configuration.GetConnectionString("MySqlConnection")!;
        }

        public List<Category> GetActive()
        {
            const string query = @"
                SELECT id,
                       nombre,
                       descripcion,
                       codigo,
                       pasilloUbicacion,
                       estaActivo,
                       idUsuarioAdmin,
                       fechaCreacion,
                       fechaActualizacion
                FROM CATEGORIAS
                WHERE estaActivo = 1
                ORDER BY nombre ASC;";

            List<Category> categories = new List<Category>();

            using MySqlConnection connection =
                new MySqlConnection(connectionString);

            using MySqlCommand command =
                new MySqlCommand(query, connection);

            connection.Open();

            using MySqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                categories.Add(MapCategory(reader));
            }

            return categories;
        }

        public Category? GetById(int id)
        {
            const string query = @"
                SELECT id,
                       nombre,
                       descripcion,
                       codigo,
                       pasilloUbicacion,
                       estaActivo,
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

            return MapCategory(reader);
        }

        public bool Add(Category category)
        {
            const string query = @"
                INSERT INTO CATEGORIAS
                (
                    nombre,
                    descripcion,
                    codigo,
                    pasilloUbicacion,
                    idUsuarioAdmin
                )
                VALUES
                (
                    @nombre,
                    @descripcion,
                    @codigo,
                    @pasilloUbicacion,
                    @idUsuarioAdmin
                );";

            using MySqlConnection connection =
                new MySqlConnection(connectionString);

            using MySqlCommand command =
                new MySqlCommand(query, connection);

            AddCommonParameters(command, category);

            connection.Open();

            int affectedRows = command.ExecuteNonQuery();

            return affectedRows > 0;
        }

        public bool Update(Category category)
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

            AddCommonParameters(command, category);

            command.Parameters.AddWithValue(
                "@id",
                category.Id);

            connection.Open();

            int affectedRows = command.ExecuteNonQuery();

            return affectedRows > 0;
        }

        public bool Delete(int id, int adminUserId)
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
                adminUserId);

            connection.Open();

            int affectedRows = command.ExecuteNonQuery();

            return affectedRows > 0;
        }

        private static void AddCommonParameters(
            MySqlCommand command,
            Category category)
        {
            command.Parameters.AddWithValue(
                "@nombre",
                category.Name);

            command.Parameters.AddWithValue(
                "@descripcion",
                (object?)category.Description ?? DBNull.Value);

            command.Parameters.AddWithValue(
                "@codigo",
                category.Code);

            command.Parameters.AddWithValue(
                "@pasilloUbicacion",
                (object?)category.AisleLocation ?? DBNull.Value);

            command.Parameters.AddWithValue(
                "@idUsuarioAdmin",
                category.AdminUserId);
        }

        private static Category MapCategory(
            MySqlDataReader reader)
        {
            return new Category
            {
                Id = reader.GetInt32("id"),

                Name = reader.GetString("nombre"),

                Description =
                    reader.IsDBNull(
                        reader.GetOrdinal("descripcion"))
                        ? null
                        : reader.GetString("descripcion"),

                Code = reader.GetString("codigo"),

                AisleLocation =
                    reader.IsDBNull(
                        reader.GetOrdinal("pasilloUbicacion"))
                        ? null
                        : reader.GetString("pasilloUbicacion"),

                IsActive =
                    reader.GetBoolean("estaActivo"),

                AdminUserId =
                    reader.GetInt32("idUsuarioAdmin"),

                CreatedAt =
                    reader.GetDateTime("fechaCreacion"),

                UpdatedAt =
                    reader.IsDBNull(
                        reader.GetOrdinal("fechaActualizacion"))
                        ? null
                        : reader.GetDateTime("fechaActualizacion")
            };
        }
    }
}