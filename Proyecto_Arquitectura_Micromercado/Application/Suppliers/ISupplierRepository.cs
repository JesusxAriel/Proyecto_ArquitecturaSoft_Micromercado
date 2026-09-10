using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

namespace Proyecto_Arquitectura_Micromercado.Application.Suppliers;

public interface ISupplierRepository
{
    Task<IReadOnlyList<SupplierListItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsNombreEmpresaAsync(string nombreEmpresa, int idExcluido, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Supplier supplier, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
}
