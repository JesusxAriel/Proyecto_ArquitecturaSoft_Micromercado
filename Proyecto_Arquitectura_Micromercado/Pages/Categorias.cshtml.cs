using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Models;

namespace Proyecto_Arquitectura_Micromercado.Pages
{
    public class CategoriasModel : PageModel
    {
        private readonly IConfiguration configuration;

        public string Mensaje { get; set; } = string.Empty;
        public List<Categoria> ListCategorias { get; set; } = new List<Categoria>();

        // Propiedad para el formulario de Creación
        [BindProperty]
        public Categoria Categoria { get; set; } = new Categoria();

        public CategoriasModel(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void OnGet()
        {
            Select();
        }

        private void Select()
        {
            string connectionString = configuration.GetConnectionString("MySqlConnection")!;
            string query = @"SELECT id, nombre, descripcion, codigo, pasilloUbicacion, estado 
                            FROM CATEGORIAS 
                            WHERE estado = 'ACTIVA'
                            ORDER BY nombre ASC";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    MySqlCommand command = new MySqlCommand(query, connection);
                    connection.Open();

                    MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                    DataTable tableCategorias = new DataTable();

                    adapter.Fill(tableCategorias);
                    ListCategorias.Clear();

                    foreach (DataRow row in tableCategorias.Rows)
                    {
                        Categoria cat = new Categoria
                        {
                            Id = Convert.ToInt32(row["id"]),
                            Nombre = row["nombre"].ToString()!,
                            Descripcion = row["descripcion"] != DBNull.Value ? row["descripcion"].ToString() : null,
                            Codigo = row["codigo"].ToString()!,
                            PasilloUbicacion = row["pasilloUbicacion"] != DBNull.Value ? row["pasilloUbicacion"].ToString() : null,
                            Estado = row["estado"].ToString()!
                        };

                        ListCategorias.Add(cat);
                    }
                }
            }
            catch (Exception ex)
            {
                Mensaje = "Error al cargar categorías: " + ex.Message;
            }
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                Select();
                return Page();
            }

            string connectionString = configuration.GetConnectionString("MySqlConnection")!;
            string query = @"INSERT INTO CATEGORIAS 
                            (nombre, descripcion, codigo, pasilloUbicacion, estado, idUsuarioAdmin, fechaCreacion) 
                            VALUES 
                            (@nombre, @descripcion, @codigo, @pasilloUbicacion, @estado, @idUsuarioAdmin, @fechaCreacion)";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    MySqlCommand command = new MySqlCommand(query, connection);

                    command.Parameters.AddWithValue("@nombre", Categoria.Nombre);
                    command.Parameters.AddWithValue("@descripcion", (object?)Categoria.Descripcion ?? DBNull.Value);
                    command.Parameters.AddWithValue("@codigo", Categoria.Codigo);
                    command.Parameters.AddWithValue("@pasilloUbicacion", (object?)Categoria.PasilloUbicacion ?? DBNull.Value);
                    command.Parameters.AddWithValue("@estado", "ACTIVA");
                    command.Parameters.AddWithValue("@idUsuarioAdmin", 1);
                    command.Parameters.AddWithValue("@fechaCreacion", DateTime.Now);

                    connection.Open();
                    command.ExecuteNonQuery();
                }

                return RedirectToPage();

            }
            catch (Exception ex)
            {
                Mensaje = "Error al guardar categoría: " + ex.Message;
                Select();
                return Page();
            }
        }
    }
}