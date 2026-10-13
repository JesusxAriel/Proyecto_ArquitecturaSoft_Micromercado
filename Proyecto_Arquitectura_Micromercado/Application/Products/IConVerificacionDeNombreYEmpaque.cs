namespace Proyecto_Arquitectura_Micromercado.Application.Products;

public interface IConVerificacionDeNombreYEmpaque
{
    Task<bool> ExistsNombreEmpaqueAsync(
        string nombre,
        int idEmpaque,
        int idExcluido,
        CancellationToken cancellationToken = default);
}
