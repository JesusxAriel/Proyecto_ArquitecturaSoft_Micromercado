using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Domain.Categories;

namespace Proyecto_Arquitectura_Micromercado.Application.Categories
{
    public interface ICategoryRepository :
        IRepositorioBase<Category, Category, int>
    {
    }
}