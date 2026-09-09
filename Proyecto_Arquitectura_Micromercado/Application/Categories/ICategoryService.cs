using Proyecto_Arquitectura_Micromercado.Domain.Categories;

namespace Proyecto_Arquitectura_Micromercado.Application.Categories
{
    public interface ICategoryService
    {
        List<Category> GetActive();
        Category? GetById(int id);
        bool Create(Category category);
        bool Update(Category category);
        bool Delete(int id, int adminUserId);
    }
}