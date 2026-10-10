using Proyecto_Arquitectura_Micromercado.Domain.Categories;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

// Propiedades de las entidades que NUNCA deben llenarse con datos del formulario.
//
// POR QUE HACE FALTA
// Las paginas enlazan la entidad de dominio directamente desde el POST
// ([BindProperty] public Product EditProduct). Eso significa que el navegador puede
// enviar cualquier propiedad de la entidad, no solo las que el formulario muestra: es
// el problema conocido como over-posting o mass assignment. Hoy nadie puede
// aprovecharlo porque los repositorios no guardan estos campos (usan la constante
// SystemAdminId o los valores por defecto de la base), pero es una proteccion por
// accidente, no por diseño. En cuanto existan las columnas de auditoria que agrega la
// parte de seguridad (usuario de creacion y de modificacion), enviar esos campos desde
// el navegador si pasaria a tener consecuencias.
//
// POR QUE NO SE USA [BindNever] EN LA ENTIDAD
// BindNever vive en Microsoft.AspNetCore.Mvc.ModelBinding. Ponerlo sobre Category,
// Product o Supplier obligaria a que Domain dependa de ASP.NET, justo lo que prohibe la
// regla de dependencia de la arquitectura hexagonal (y lo que detecta la prueba
// ArquitecturaHexagonalTests.Domain_no_depende_de_ninguna_otra_capa).
//
// Que un campo se pueda enviar por HTTP es una decision del adaptador web, no del
// dominio, asi que la politica vive aqui y el nucleo queda limpio. La aplica
// CamposNoEnlazablesProvider, registrado en Program.cs, y vale para todas las paginas
// a la vez: no hay que repetir nada en cada PageModel ni en cada vista.
//
// Esta clase es pura a proposito, sin tipos de ASP.NET, para poder probarla directamente.
public static class CamposNoEnlazables
{
    // Se usa nameof en vez de cadenas para que renombrar una propiedad rompa la
    // compilacion en lugar de dejar silenciosamente un campo desprotegido.
    private static readonly HashSet<(Type Contenedor, string Propiedad)> Prohibidos =
    [
        // Estado de la baja logica: lo maneja SoftDeleteAsync, nunca un formulario.
        (typeof(Category), nameof(Category.IsActive)),
        (typeof(Supplier), nameof(Supplier.EstaActivo)),
        (typeof(Product), nameof(Product.EstaActivo)),

        // Auditoria de la categoria: la define el servidor.
        (typeof(Category), nameof(Category.AdminUserId)),
        (typeof(Category), nameof(Category.CreatedAt)),
        (typeof(Category), nameof(Category.UpdatedAt)),

        // El codigo lo genera CategoryService a partir del nombre; el formulario no lo
        // captura y no debe poder imponerlo.
        (typeof(Category), nameof(Category.Code))
    ];

    public static bool EsProhibido(Type contenedor, string propiedad) =>
        Prohibidos.Contains((contenedor, propiedad));

    // Expuesto para que las pruebas puedan recorrer la politica completa.
    public static IReadOnlyCollection<(Type Contenedor, string Propiedad)> Todos => Prohibidos;
}
