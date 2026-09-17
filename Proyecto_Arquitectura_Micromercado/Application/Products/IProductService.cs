using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Domain.Products;

namespace Proyecto_Arquitectura_Micromercado.Application.Products;

public interface IProductService :
    IServicioCRUD<Product, int, Product, Product>,
    IConListado,
    IConCatalogo,
    IConHistorialPrecios
{
}
