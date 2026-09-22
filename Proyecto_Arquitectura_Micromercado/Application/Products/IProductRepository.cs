using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Domain.Products;

namespace Proyecto_Arquitectura_Micromercado.Application.Products;

public interface IProductRepository :
    IRepositorioBase<Product, ProductListItem, int>,
    IConCatalogo
{
}
