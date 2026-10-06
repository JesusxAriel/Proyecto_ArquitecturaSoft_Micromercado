namespace Proyecto_Arquitectura_Micromercado.Application.Categories;

public interface IConBusqueda
{
    // Compara sin distinguir mayúsculas, tildes ni espacios extra, solo entre categorías activas.
    Task<bool> ExistsNameAsync(
        string name,
        int idExcluido,
        CancellationToken cancellationToken = default);

    // Todos los códigos usados, incluidas las categorías dadas de baja (el índice UNIQUE las considera).
    Task<IReadOnlyList<string>> GetAllCodesAsync(
        CancellationToken cancellationToken = default);
}
