using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

namespace Proyecto_Arquitectura_Micromercado.Application.Suppliers;

public interface IConListado
{
    Task<IReadOnlyList<SupplierListItem>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
