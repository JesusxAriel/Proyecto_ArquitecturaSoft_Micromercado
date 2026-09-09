using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Models;

namespace Proyecto_Arquitectura_Micromercado.Pages
{
    public class CategoriaNuevaModel : PageModel
    {
        private readonly IConfiguration configuration;

        [BindProperty]
        public Categoria Categoria { get; set; } = new Categoria();

        public string MensajeError { get; set; } = string.Empty;

        public CategoriaNuevaModel(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            string connectionString =
                configuration.GetConnectionString("MySqlConnection")!;

            string query = @"
                INSERT INTO CATEGORIAS
                (nombre, descripcion, codigo, pasilloUbicacion, idUsuarioAdmin)
                VALUES
                (@nombre, @descripcion, @codigo, @pasilloUbicacion, @idUsuarioAdmin);";

            try
            {
                using (MySqlConnection connection =
                    new MySqlConnection(connectionString))
                {
                    MySqlCommand command =
                        new MySqlCommand(query, connection);

                    command.Parameters.AddWithValue(
                        "@nombre",
                        Categoria.Nombre);

                    command.Parameters.AddWithValue(
                        "@descripcion",
                        (object?)Categoria.Descripcion ?? DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@codigo",
                        Categoria.Codigo);

                    command.Parameters.AddWithValue(
                        "@pasilloUbicacion",
                        (object?)Categoria.PasilloUbicacion ?? DBNull.Value);

                    command.Parameters.AddWithValue(
                        "@idUsuarioAdmin",
                        1);

                    connection.Open();
                    command.ExecuteNonQuery();
                }

                return RedirectToPage("/Categorias");
            }
            catch (Exception ex)
            {
                MensajeError = ex.Message;
                return Page();
            }
        }
    }
}