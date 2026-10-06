namespace Proyecto_Arquitectura_Micromercado.Application.Categories;

public interface IConBusqueda
{
    Task<bool> ExistsCodeAsync(
        string code,
        int idExcluido,
        CancellationToken cancellationToken = default);
}
