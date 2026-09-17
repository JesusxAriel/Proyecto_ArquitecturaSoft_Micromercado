namespace Proyecto_Arquitectura_Micromercado.Application.Suppliers;

public interface IConBusqueda
{
    Task<bool> ExistsNombreEmpresaAsync(
        string nombreEmpresa,
        int idExcluido,
        CancellationToken cancellationToken = default);
}
