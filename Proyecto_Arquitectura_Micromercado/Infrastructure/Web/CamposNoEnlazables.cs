using Proyecto_Arquitectura_Micromercado.Domain.Categories;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

public static class CamposNoEnlazables
{
    private static readonly HashSet<(Type Contenedor, string Propiedad)> Prohibidos =
    [
        (typeof(Category), nameof(Category.IsActive)),
        (typeof(Supplier), nameof(Supplier.EstaActivo)),
        (typeof(Product), nameof(Product.EstaActivo)),

        (typeof(Category), nameof(Category.AdminUserId)),
        (typeof(Category), nameof(Category.CreatedAt)),
        (typeof(Category), nameof(Category.UpdatedAt)),

        (typeof(Category), nameof(Category.Code))
    ];

    public static bool EsProhibido(Type contenedor, string propiedad) =>
        Prohibidos.Contains((contenedor, propiedad));

    public static IReadOnlyCollection<(Type Contenedor, string Propiedad)> Todos => Prohibidos;
}
