using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Models; // Reemplaza por tu namespace de Models si es diferente

namespace Proyecto_Arquitectura_Micromercado.Pages
{
    public class ProductosModel : PageModel
    {
        private readonly IConfiguration configuration;
        public string Mensaje { get; set; } = string.Empty;
        public List<Producto> ListProductos { get; set; } = new List<Producto>();

        public ProductosModel(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void OnGet()
        {
            Select();
        }

        void Select()
        {
            string connectionString = configuration.GetConnectionString("MySqlConnection")!;

            string query = @"SELECT id, nombre, categoria, precio, stock 
                            FROM PRODUCTO 
                            ORDER BY 2";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    MySqlCommand command = new MySqlCommand(query, connection);

                    connection.Open();

                    MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                    DataTable tableProductos = new DataTable();

                    adapter.Fill(tableProductos);
                    foreach (DataRow row in tableProductos.Rows)
                    {
                        Producto producto = new Producto
                        {
                            Id = Convert.ToInt32(row["id"]),
                            Nombre = row["nombre"].ToString()!,
                            Categoria = row["categoria"].ToString()!,
                            Precio = Convert.ToDouble(row["precio"]),
                            Stock = Convert.ToInt32(row["stock"])
                        };

                        ListProductos.Add(producto);
                    }
                }
            }
            catch (Exception ex)
            {
                Mensaje = ex.Message;
            }
        }
    }
}