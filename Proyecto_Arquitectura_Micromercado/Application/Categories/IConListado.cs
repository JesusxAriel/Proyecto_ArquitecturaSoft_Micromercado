using Proyecto_Arquitectura_Micromercado.Domain.Categories;

namespace Proyecto_Arquitectura_Micromercado.Application.Categories;

public interface IConListado
{
    Task<IReadOnlyList<Category>> GetAllAsync(
        CancellationToken cancellationToken = default);
}