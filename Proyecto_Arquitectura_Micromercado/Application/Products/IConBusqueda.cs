namespace Proyecto_Arquitectura_Micromercado.Application.Products;

public interface IConBusqueda
{
    // Compara sin distinguir mayúsculas, tildes ni espacios extra, solo entre productos activos.
    Task<bool> ExistsNombreEmpaqueAsync(
        string nombre,
        int idEmpaque,
        int idExcluido,
        CancellationToken cancellationToken = default);
}
