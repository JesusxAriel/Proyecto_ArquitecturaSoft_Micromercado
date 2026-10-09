using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Domain.Products;

namespace Proyecto_Arquitectura_Micromercado.Application.Products;

public interface IProductService :
    IServicioCRUD<Product, int>,
    IConListado,
    IConListadoPaginado,
    IConCatalogo,
    IConHistorialPrecios
{
    // Misma comparación que al guardar: sin distinguir mayúsculas, tildes ni espacios extra.
    Task<bool> IsDuplicateAsync(
        string nombre,
        int idEmpaque,
        int idExcluido,
        CancellationToken cancellationToken = default);
}
