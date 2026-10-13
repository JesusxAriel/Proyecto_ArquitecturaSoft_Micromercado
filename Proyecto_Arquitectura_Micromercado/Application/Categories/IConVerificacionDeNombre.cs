namespace Proyecto_Arquitectura_Micromercado.Application.Categories;

public interface IConVerificacionDeNombre
{
    Task<bool> ExistsNameAsync(
        string name,
        int idExcluido,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetAllCodesAsync(
        CancellationToken cancellationToken = default);
}
