namespace Proyecto_Arquitectura_Micromercado.Application.Categories;

public interface IConBusqueda
{
    // Todos los códigos usados, incluidas las categorías dadas de baja (el índice UNIQUE las considera).
    Task<IReadOnlyList<string>> GetAllCodesAsync(
        CancellationToken cancellationToken = default);
}
