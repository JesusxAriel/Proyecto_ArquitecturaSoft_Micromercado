using System.Data;
using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Models;

namespace Proyecto_Arquitectura_Micromercado.Data
{
    public class ProveedorRepository : IProveedorRepository
    {
        private readonly string connectionString;

        public ProveedorRepository(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("MySqlConnection")!;
        }

        public List<Proveedor> GetActive()
        {
            var proveedores = new List<Proveedor>();

            string query = @"SELECT id, nombreEmpresa, numeroEmpresa, correoReferencia,
                                     esAutogestionado, estaActivo, idUsuarioAdmin,
                                     fechaCreacion, fechaActualizacion
                              FROM PROVEEDOR
                              WHERE estaActivo = 1
                              ORDER BY nombreEmpresa";

            using (var connection = new MySqlConnection(connectionString))
            using (var command = new MySqlCommand(query, connection))
            {
                connection.Open();

                var adapter = new MySqlDataAdapter(command);
                var table = new DataTable();
                adapter.Fill(table);

                foreach (DataRow row in table.Rows)
                {
                    proveedores.Add(MapProveedor(row));
                }
            }

            return proveedores;
        }

        public Proveedor? GetById(int id)
        {
            string query = @"SELECT id, nombreEmpresa, numeroEmpresa, correoReferencia,
                                     esAutogestionado, estaActivo, idUsuarioAdmin,
                                     fechaCreacion, fechaActualizacion
                              FROM PROVEEDOR
                              WHERE id = @id AND estaActivo = 1";

            using (var connection = new MySqlConnection(connectionString))
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@id", id);
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return MapProveedor(reader);
                    }
                }
            }

            return null;
        }

        public void Add(Proveedor proveedor)
        {
            string query = @"INSERT INTO PROVEEDOR
                                (nombreEmpresa, numeroEmpresa, correoReferencia, esAutogestionado, idUsuarioAdmin)
                              VALUES
                                (@nombreEmpresa, @numeroEmpresa, @correoReferencia, @esAutogestionado, @idUsuarioAdmin)";

            using (var connection = new MySqlConnection(connectionString))
            using (var command = new MySqlCommand(query, connection))
            {
                AddCommonParameters(command, proveedor);
                // TODO: reemplazar el 1 fijo por el id del usuario autenticado
                // cuando el sistema tenga control de sesión/login implementado.
                command.Parameters.AddWithValue("@idUsuarioAdmin", 1);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public void Update(Proveedor proveedor)
        {
            string query = @"UPDATE PROVEEDOR
                              SET nombreEmpresa = @nombreEmpresa,
                                  numeroEmpresa = @numeroEmpresa,
                                  correoReferencia = @correoReferencia,
                                  esAutogestionado = @esAutogestionado
                              WHERE id = @id";

            using (var connection = new MySqlConnection(connectionString))
            using (var command = new MySqlCommand(query, connection))
            {
                AddCommonParameters(command, proveedor);
                command.Parameters.AddWithValue("@id", proveedor.Id);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            // Baja lógica (soft delete), igual que el resto de tablas del sistema.
            string query = "UPDATE PROVEEDOR SET estaActivo = 0 WHERE id = @id";

            using (var connection = new MySqlConnection(connectionString))
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@id", id);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        // ---- Métodos privados de apoyo (evitan duplicar código entre Add/Update/mapeo) ----

        private static void AddCommonParameters(MySqlCommand command, Proveedor proveedor)
        {
            command.Parameters.AddWithValue("@nombreEmpresa", proveedor.NombreEmpresa);
            command.Parameters.AddWithValue("@numeroEmpresa", proveedor.NumeroEmpresa);
            command.Parameters.AddWithValue("@correoReferencia", (object?)proveedor.CorreoReferencia ?? DBNull.Value);
            command.Parameters.AddWithValue("@esAutogestionado", proveedor.EsAutogestionado);
        }

        private static Proveedor MapProveedor(DataRow row)
        {
            return new Proveedor
            {
                Id = Convert.ToInt32(row["id"]),
                NombreEmpresa = row["nombreEmpresa"].ToString()!,
                NumeroEmpresa = row["numeroEmpresa"].ToString()!,
                CorreoReferencia = row["correoReferencia"] as string,
                EsAutogestionado = Convert.ToBoolean(row["esAutogestionado"]),
                EstaActivo = Convert.ToBoolean(row["estaActivo"]),
                IdUsuarioAdmin = Convert.ToInt32(row["idUsuarioAdmin"]),
                FechaCreacion = Convert.ToDateTime(row["fechaCreacion"]),
                FechaActualizacion = row["fechaActualizacion"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(row["fechaActualizacion"])
            };
        }

        private static Proveedor MapProveedor(MySqlDataReader row)
        {
            return new Proveedor
            {
                Id = row.GetInt32("id"),
                NombreEmpresa = row.GetString("nombreEmpresa"),
                NumeroEmpresa = row.GetString("numeroEmpresa"),
                CorreoReferencia = row.IsDBNull(row.GetOrdinal("correoReferencia")) ? null : row.GetString("correoReferencia"),
                EsAutogestionado = row.GetBoolean("esAutogestionado"),
                EstaActivo = row.GetBoolean("estaActivo"),
                IdUsuarioAdmin = row.GetInt32("idUsuarioAdmin"),
                FechaCreacion = row.GetDateTime("fechaCreacion"),
                FechaActualizacion = row.IsDBNull(row.GetOrdinal("fechaActualizacion")) ? null : row.GetDateTime("fechaActualizacion")
            };
        }
    }
}